using System.IO;
using OptiClick.Wpf.Install.Planning;
using OptiClick.Wpf.Install.Precheck;
using OptiClick.Wpf.Install.Uninstall;
using OptiClick.Wpf.Localization;
using OptiClick.Wpf.Logging;
using OptiClick.Wpf.Models;
using OptiClick.Wpf.Shell.Dialogs;
using OptiClick.Wpf.Shell.Games;
using OptiClick.Wpf.Shell.Selection;
using OptiClick.Wpf.ViewModels;
using InfrastructureUninstall = OptiClick.Infrastructure.Install.Uninstall;

namespace OptiClick.Wpf.Install.Flow;

public sealed class UninstallFlowCoordinator
{
    private readonly IOptiClickUninstallPlanBuilder _planBuilder;
    private readonly IOptiClickUninstallExecutor _executor;
    private readonly DialogPresenter _dialogPresenter;
    private readonly IAppLogger _appLogger;

    public UninstallFlowCoordinator(
        IOptiClickUninstallPlanBuilder planBuilder,
        IOptiClickUninstallExecutor executor,
        DialogPresenter dialogPresenter,
        IAppLogger appLogger)
    {
        _planBuilder = planBuilder ?? throw new ArgumentNullException(nameof(planBuilder));
        _executor = executor ?? throw new ArgumentNullException(nameof(executor));
        _dialogPresenter = dialogPresenter ?? throw new ArgumentNullException(nameof(dialogPresenter));
        _appLogger = appLogger ?? throw new ArgumentNullException(nameof(appLogger));
    }

    public async Task RunAsync(
        UninstallFlowCoordinatorRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.SelectedGame);
        ArgumentNullException.ThrowIfNull(request.Strings);

        var targetPath = InstallTargetPathNormalizer.NormalizeTargetDirectory(request.TargetPath);
        LogInfo(
            MainViewModelLogCategories.UninstallFlow,
            $"uninstall plan build start game_id={NormalizeStatusCode(request.SelectedGameId, "none")} target={NormalizeStatusCode(targetPath, "none")}");
        var plan = _planBuilder.BuildPlan(new OptiClickUninstallPlanBuildRequest
        {
            TargetPath = targetPath,
            SelectedGame = ShellGameCardMapper.Map(request.SelectedGame),
            FinalProxyDllName = ResolveFinalProxyDllName(request.SelectionStateBeforeExecution),
            UalDetectedNames = ResolveUalDetectedNames(request.SelectionStateBeforeExecution.PrecheckSnapshot)
        });
        LogInfo(
            MainViewModelLogCategories.UninstallFlow,
            $"uninstall plan build result status={plan.Status} candidates={plan.Candidates.Count} engine_ini_cleanup={plan.EngineIniCleanupTargets.Count} skipped={plan.SkippedFiles.Count} error={NormalizeStatusCode(plan.ErrorCode, "none")}");

        switch (plan.Status)
        {
            case InfrastructureUninstall.UninstallPlanStatus.Ready:
                if (plan.Candidates.Count == 0 && plan.EngineIniCleanupTargets.Count == 0)
                {
                    await ShowUninstallNoRemovableItemsDialogAsync(request.Strings, cancellationToken);
                    return;
                }

                var confirmationDialog = BuildUninstallConfirmationDialogRequest(plan, request.Strings);
                var confirmationResult = await _dialogPresenter.ShowSafelyAsync(confirmationDialog, cancellationToken);
                var confirmed = confirmationResult == AppDialogResult.Continue;
                LogInfo(
                    MainViewModelLogCategories.UninstallFlow,
                    $"uninstall confirmation result confirmed={confirmed} dialog_result={confirmationResult}");
                if (!confirmed)
                {
                    return;
                }

                await ExecuteUninstallAsync(request, plan, cancellationToken);
                return;
            case InfrastructureUninstall.UninstallPlanStatus.NothingToRemove:
                await ShowUninstallNoRemovableItemsDialogAsync(request.Strings, cancellationToken);
                return;
            case InfrastructureUninstall.UninstallPlanStatus.InvalidTarget:
            case InfrastructureUninstall.UninstallPlanStatus.ValidationFailed:
            default:
                LogWarning(
                    MainViewModelLogCategories.UninstallFlow,
                    $"uninstall plan rejected status={plan.Status} error={NormalizeStatusCode(plan.ErrorCode, "none")}");
                await ShowUninstallValidationFailedDialogAsync(request.Strings, cancellationToken);
                return;
        }
    }

    private async Task ExecuteUninstallAsync(
        UninstallFlowCoordinatorRequest request,
        InfrastructureUninstall.UninstallPlan plan,
        CancellationToken cancellationToken)
    {
        request.ApplyInstallBusyState(true, null, request.Strings.OperationOverlayUninstalling);
        request.ApplySettingsStatusText(request.Strings.UninstallInProgressStatus);
        InfrastructureUninstall.UninstallExecutionResult executionResult;
        try
        {
            executionResult = await _executor.ExecuteAsync(plan, cancellationToken);
        }
        finally
        {
            request.ApplyInstallBusyState(false, request.SelectionStateBeforeExecution, "");
        }

        LogInfo(
            MainViewModelLogCategories.UninstallFlow,
            $"uninstall execute result status={executionResult.Status} deleted={executionResult.DeletedFiles.Count} failed={executionResult.FailedFiles.Count} skipped={executionResult.SkippedFiles.Count} engine_ini_cleaned={executionResult.CleanedEngineIniEntries.Count} engine_ini_failed={executionResult.FailedEngineIniEntries.Count} engine_ini_skipped={executionResult.SkippedEngineIniEntries.Count} error={NormalizeStatusCode(executionResult.ErrorCode, "none")}");

        var completionDialog = BuildUninstallCompletionDialogRequest(executionResult, request.Strings);
        await _dialogPresenter.ShowSafelyAsync(completionDialog, cancellationToken);
        await request.RefreshSelectionAfterUninstallAsync(request.SelectedGame, cancellationToken);
    }

    private Task ShowUninstallNoRemovableItemsDialogAsync(
        AppStrings strings,
        CancellationToken cancellationToken)
    {
        return _dialogPresenter.ShowSafelyAsync(
            new AppDialogRequest
            {
                Kind = AppDialogKind.Info,
                Severity = DialogSeverity.Info,
                Title = strings.InstallManagementUninstallButton,
                Summary = strings.UninstallNoRemovableItemsSummary,
                PrimaryButtonText = strings.DialogButtonOk,
                PrimaryResult = AppDialogResult.Ok
            },
            cancellationToken);
    }

    private Task ShowUninstallValidationFailedDialogAsync(
        AppStrings strings,
        CancellationToken cancellationToken)
    {
        return _dialogPresenter.ShowSafelyAsync(
            new AppDialogRequest
            {
                Kind = AppDialogKind.Warning,
                Severity = DialogSeverity.Warning,
                Title = strings.UninstallValidationFailedTitle,
                Summary = strings.UninstallValidationFailedSummary,
                PrimaryButtonText = strings.DialogButtonOk,
                PrimaryResult = AppDialogResult.Ok
            },
            cancellationToken);
    }

    private static AppDialogRequest BuildUninstallConfirmationDialogRequest(
        InfrastructureUninstall.UninstallPlan plan,
        AppStrings strings)
    {
        var lines = BuildUninstallCandidateLines(plan);
        var details = string.Join("\n", lines);
        var summary = string.IsNullOrWhiteSpace(details)
            ? strings.UninstallConfirmationSummary
            : $"{strings.UninstallConfirmationSummary}\n\n{details}";

        return new AppDialogRequest
        {
            Kind = AppDialogKind.Warning,
            Severity = DialogSeverity.Warning,
            Title = strings.UninstallConfirmationTitle,
            Summary = summary,
            PrimaryButtonText = strings.InstallManagementUninstallButton,
            SecondaryButtonText = strings.DialogButtonCancel,
            PrimaryButtonRole = DialogButtonRole.Destructive,
            PrimaryResult = AppDialogResult.Continue,
            SecondaryResult = AppDialogResult.Cancel
        };
    }

    private static IReadOnlyList<string> BuildUninstallCandidateLines(InfrastructureUninstall.UninstallPlan plan)
    {
        var lines = new List<string>();
        AppendCandidateLine(lines, plan.Candidates, InfrastructureUninstall.UninstallCandidateKind.OptiScaler, "OptiScaler");
        AppendCandidateLine(lines, plan.Candidates, InfrastructureUninstall.UninstallCandidateKind.ReFramework, "REFramework");
        AppendCandidateLine(lines, plan.Candidates, InfrastructureUninstall.UninstallCandidateKind.UltimateAsiLoader, "Ultimate ASI Loader");
        AppendCandidateLine(lines, plan.Candidates, InfrastructureUninstall.UninstallCandidateKind.SpecialK, "Special K");
        return lines;
    }

    private static void AppendCandidateLine(
        ICollection<string> target,
        IReadOnlyList<InfrastructureUninstall.UninstallCandidate> candidates,
        InfrastructureUninstall.UninstallCandidateKind kind,
        string displayName)
    {
        var fileNames = candidates
            .Where(candidate => candidate.Kind == kind)
            .Select(static candidate => ResolveUninstallDisplayFileName(candidate.RelativePath, candidate.FullPath))
            .Where(static fileName => !string.IsNullOrWhiteSpace(fileName))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (fileNames.Length == 0)
        {
            return;
        }

        target.Add($"{displayName}: {string.Join(", ", fileNames)}");
    }

    private static string ResolveUninstallDisplayFileName(string relativePath, string fullPath)
    {
        var fromRelative = Path.GetFileName((relativePath ?? "").Trim());
        if (!string.IsNullOrWhiteSpace(fromRelative))
        {
            return fromRelative;
        }

        var fromFullPath = Path.GetFileName((fullPath ?? "").Trim());
        if (!string.IsNullOrWhiteSpace(fromFullPath))
        {
            return fromFullPath;
        }

        return (relativePath ?? fullPath ?? "").Trim();
    }

    private static string ResolveFinalProxyDllName(ShellInstallSelectionState selectionState)
    {
        return PickFileName(
            selectionState.SelectedInstallStatus?.DetectedFile,
            selectionState.PrecheckResolvedDllName,
            selectionState.PrecheckSnapshot.ResolvedDllName);
    }

    private static IReadOnlyList<string> ResolveUalDetectedNames(InstallPrecheckSnapshot precheck)
    {
        if (precheck.Findings.Count == 0)
        {
            return Array.Empty<string>();
        }

        var names = new List<string>();
        foreach (var finding in precheck.Findings)
        {
            if (!string.Equals((finding.Kind ?? "").Trim(), ModConflictKinds.UltimateAsiLoader, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            foreach (var evidence in finding.Evidence)
            {
                var fileName = PickFileName(evidence);
                if (!string.IsNullOrWhiteSpace(fileName))
                {
                    names.Add(fileName);
                }
            }
        }

        return names
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string PickFileName(params string?[] candidates)
    {
        foreach (var candidate in candidates)
        {
            var fileName = Path.GetFileName((candidate ?? "").Trim());
            if (!string.IsNullOrWhiteSpace(fileName))
            {
                return fileName;
            }
        }

        return "";
    }

    private static AppDialogRequest BuildUninstallCompletionDialogRequest(
        InfrastructureUninstall.UninstallExecutionResult result,
        AppStrings strings)
    {
        return result.Status switch
        {
            InfrastructureUninstall.UninstallExecutionStatus.Success => new AppDialogRequest
            {
                Kind = AppDialogKind.Success,
                Severity = DialogSeverity.Success,
                Title = strings.UninstallCompletedTitle,
                Summary = strings.UninstallCompletedSummary,
                PrimaryButtonText = strings.DialogButtonOk,
                PrimaryResult = AppDialogResult.Ok,
                PrimaryButtonRole = DialogButtonRole.Success
            },
            InfrastructureUninstall.UninstallExecutionStatus.PartialSuccess => new AppDialogRequest
            {
                Kind = AppDialogKind.Warning,
                Severity = DialogSeverity.Warning,
                Title = strings.UninstallPartialFailedTitle,
                Summary = strings.UninstallPartialFailedSummary,
                PrimaryButtonText = strings.DialogButtonOk,
                PrimaryResult = AppDialogResult.Ok
            },
            InfrastructureUninstall.UninstallExecutionStatus.Failed => new AppDialogRequest
            {
                Kind = AppDialogKind.Warning,
                Severity = DialogSeverity.Warning,
                Title = strings.UninstallFailedTitle,
                Summary = strings.UninstallFailedSummary,
                PrimaryButtonText = strings.DialogButtonOk,
                PrimaryResult = AppDialogResult.Ok
            },
            _ => new AppDialogRequest
            {
                Kind = AppDialogKind.Info,
                Severity = DialogSeverity.Info,
                Title = strings.InstallManagementUninstallButton,
                Summary = strings.UninstallNoRemovableItemsSummary,
                PrimaryButtonText = strings.DialogButtonOk,
                PrimaryResult = AppDialogResult.Ok
            }
        };
    }

    private void LogInfo(string category, string message) => _appLogger.Info(category, message);

    private void LogWarning(string category, string message) => _appLogger.Warning(category, message);

    private static string NormalizeStatusCode(string? value, string fallback)
    {
        var normalized = (value ?? "").Trim();
        return string.IsNullOrWhiteSpace(normalized) ? fallback : normalized;
    }
}

public sealed record UninstallFlowCoordinatorRequest
{
    public required GameCardViewModel SelectedGame { get; init; }
    public required string SelectedGameId { get; init; }
    public required string TargetPath { get; init; }
    public required AppStrings Strings { get; init; }
    public required ShellInstallSelectionState SelectionStateBeforeExecution { get; init; }
    public required Action<bool, ShellInstallSelectionState?, string> ApplyInstallBusyState { get; init; }
    public required Action<string> ApplySettingsStatusText { get; init; }
    public required Func<GameCardViewModel, CancellationToken, Task> RefreshSelectionAfterUninstallAsync { get; init; }
}
