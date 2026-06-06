using System.Globalization;
using System.Diagnostics;
using OptiClick.Wpf.Install.Config;
using OptiClick.Wpf.Install.Execution;
using OptiClick.Wpf.Install.Gates;
using OptiClick.Wpf.Install.Planning;
using OptiClick.Wpf.Install.Precheck;
using OptiClick.Wpf.Install.Presentation;
using OptiClick.Wpf.Install.UiState;
using OptiClick.Wpf.Shell.Games;
using OptiClick.Wpf.Shell.Games.Actions;
using OptiClick.Wpf.Shell.Scan;

namespace OptiClick.Wpf.Install.Flow;

public sealed class InstallFlowController
{
    private readonly IInstallPlanBuilder? _installPlanBuilder;
    private readonly IInstallStartGateResolver? _installStartGateResolver;
    private readonly IComponentInstallCoordinator? _componentInstallCoordinator;
    private readonly IComponentInstallParityReviewBuilder? _componentInstallParityReviewBuilder;
    private readonly InstallPlanInputBuilder _installPlanInputBuilder;
    private readonly ComponentInstallContextBuilder _componentInstallContextBuilder;
    private readonly InstallPopupPresenter _installPopupPresenter;
    private readonly IInstallResultApplier _installResultApplier;
    private readonly IInstallRejectionPresentationResolver? _installRejectionPresentationResolver;

    public InstallFlowController(
        IInstallPlanBuilder? installPlanBuilder,
        IInstallStartGateResolver? installStartGateResolver,
        IComponentInstallCoordinator? componentInstallCoordinator,
        IComponentInstallParityReviewBuilder? componentInstallParityReviewBuilder,
        InstallPlanInputBuilder? installPlanInputBuilder,
        ComponentInstallContextBuilder? componentInstallContextBuilder,
        InstallPopupPresenter? installPopupPresenter,
        IInstallResultApplier installResultApplier,
        IInstallRejectionPresentationResolver? installRejectionPresentationResolver)
    {
        _installPlanBuilder = installPlanBuilder;
        _installStartGateResolver = installStartGateResolver;
        _componentInstallCoordinator = componentInstallCoordinator;
        _componentInstallParityReviewBuilder = componentInstallParityReviewBuilder;
        _installPlanInputBuilder = installPlanInputBuilder ?? new InstallPlanInputBuilder();
        _componentInstallContextBuilder = componentInstallContextBuilder ?? new ComponentInstallContextBuilder();
        _installPopupPresenter = installPopupPresenter ?? new InstallPopupPresenter();
        _installResultApplier = installResultApplier ?? throw new ArgumentNullException(nameof(installResultApplier));
        _installRejectionPresentationResolver = installRejectionPresentationResolver;
    }

    public async Task<InstallFlowResult> ExecuteAsync(
        InstallFlowRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Strings);
        ArgumentNullException.ThrowIfNull(request.SelectedGame);

        var logs = new List<InstallFlowLogEntry>();
        if (_installPlanBuilder is null
            || _installStartGateResolver is null
            || _componentInstallCoordinator is null
            || _componentInstallParityReviewBuilder is null)
        {
            logs.Add(Error("install", "install requested but execution dependencies are missing"));
            return new InstallFlowResult
            {
                DidStart = false,
                WasBlocked = true,
                StatusText = request.Strings.InstallDependenciesMissing,
                Logs = logs
            };
        }

        var planBuildInput = _installPlanInputBuilder.Build(new InstallPlanInputBuildContext
        {
            SelectedGame = request.SelectedGame,
            LatestRuntimeContext = request.LatestRuntimeContext,
            SelectionState = request.SelectionState,
            LatestArchiveReadiness = request.LatestArchiveReadiness,
            MatchByGameId = request.MatchByGameId,
            TargetPathByGameId = request.TargetPathByGameId,
            IsInstallExecutionInProgress = request.IsInstallExecutionInProgress,
            IsAppUpdateInProgress = request.IsAppUpdateInProgress
        });
        var planBuildResult = _installPlanBuilder.Build(planBuildInput);
        var plan = planBuildResult.Plan;

        var selectedShellGame = ShellGameCardMapper.Map(request.SelectedGame);
        var fsr4Required = planBuildInput.IsFsr4Required;
        var componentReview = _componentInstallParityReviewBuilder.Build(new ComponentInstallParityReviewInput
        {
            Plan = plan,
            ArchiveReadiness = request.LatestArchiveReadiness,
            Precheck = request.SelectionState.PrecheckSnapshot
        });

        var hasRemoteCatalogError = !string.IsNullOrWhiteSpace((request.LatestRemoteCatalogErrorCode ?? "").Trim());

        var gateDecision = _installStartGateResolver.Resolve(new InstallStartGateInput
        {
            IsWindowsSupported = request.IsWindowsSupported,
            IsMultiGpuBlocked = request.SelectionState.MultiGpuBlocked,
            IsGpuSelectionPending = request.SelectionState.GpuSelectionPending,
            IsSheetLoading = request.SelectionState.SheetLoading,
            IsSheetReady = !hasRemoteCatalogError && request.SelectionState.SheetReady,
            IsInstallInProgress = request.IsInstallExecutionInProgress,
            IsAppUpdateInProgress = request.IsAppUpdateInProgress,
            IsPredownloadInProgress = false,
            HasSelectedGame = true,
            HasValidMatch = request.SelectionState.SelectedMatchResult is not null
                            && request.SelectionState.SelectedMatchResult.Status == ShellGameMatchStatus.Matched,
            TargetPath = plan.TargetFolder,
            ArchiveReadiness = request.LatestArchiveReadiness,
            Precheck = request.SelectionState.PrecheckSnapshot,
            RequiresFsr4 = fsr4Required,
            IsFsr4Ready = request.LatestArchiveReadiness.Fsr4State == ArchiveReadinessState.Ready,
            IsExtraBundleReady = IsExtraBundleReady(selectedShellGame, request.ModuleDownloadLinks),
            IsUnsupportedGpu = string.Equals(
                request.SelectionState.ActionAvailabilityReasonCode,
                ShellGameActionReasonCodes.UnsupportedGpu,
                StringComparison.OrdinalIgnoreCase),
            IsDisabledGame = !selectedShellGame.Enabled,
            IsPopupConfirmed = request.SelectionState.PopupConfirmed,
            HasPendingPopupRequests = request.SelectionState.PendingPopupRequests.Count > 0,
            InstallPlan = plan,
            ComponentReview = componentReview,
            RequireWritePermissionProbe = true
        });

        if (!gateDecision.CanStart)
        {
            logs.Add(Warning(
                "install-gate",
                $"blocked reason={NormalizeStatusCode(gateDecision.ReasonCode, "unknown")} stage={NormalizeStatusCode(gateDecision.Stage, "none")}"));
            var rejectionPopup = _installPopupPresenter.ResolveInstallRejection(
                gateDecision,
                _installRejectionPresentationResolver);
            return new InstallFlowResult
            {
                DidStart = false,
                WasBlocked = true,
                StatusText = Format(request.Strings.InstallBlocked, gateDecision.ReasonCode),
                PopupRequest = rejectionPopup.Kind == PopupPresentationKind.None ? null : rejectionPopup,
                Plan = plan,
                Logs = logs
            };
        }

        var context = _componentInstallContextBuilder.Build(new ComponentInstallContextBuildInput
        {
            Plan = plan,
            SelectedGame = selectedShellGame,
            LatestRuntimeContext = request.LatestRuntimeContext,
            LatestArchiveReadiness = request.LatestArchiveReadiness,
            ModuleDownloadLinks = request.ModuleDownloadLinks
        });
        if (context.UseUltimateAsiLoader)
        {
            var ualDetectedNames = ResolveUalDetectedNames(request.SelectionState.PrecheckSnapshot);
            if (ualDetectedNames.Count > 0)
            {
                context = context with
                {
                    UalDetectedNames = ualDetectedNames
                };
            }
        }
        ComponentInstallResult installResult;
        var stopwatch = Stopwatch.StartNew();
        try
        {
            installResult = await _componentInstallCoordinator.ExecuteAsync(context, cancellationToken);
        }
        catch (Exception ex)
        {
            logs.Add(Error("install", "execution failed with exception", ex));
            installResult = new ComponentInstallResult
            {
                IsSuccess = false,
                FailedStep = ComponentInstallStepResult.Failed(
                    ComponentInstallName.OptiScalerCore,
                    "execution_exception",
                    ex.Message)
            };
        }

        var applyResult = _installResultApplier.Apply(new InstallResultApplyRequest
        {
            Plan = plan,
            InstallResult = installResult,
            SelectedGame = selectedShellGame,
            SelectionState = request.SelectionState,
            Strings = request.Strings
        });
        stopwatch.Stop();
        logs.AddRange(applyResult.Logs);
        logs.Add(CreateInstallCompletionLog(
            selectedShellGame,
            context,
            installResult,
            applyResult,
            stopwatch.ElapsedMilliseconds));

        return new InstallFlowResult
        {
            DidStart = true,
            WasBlocked = false,
            StatusText = applyResult.StatusText,
            PopupRequest = applyResult.PopupRequest,
            Plan = plan,
            ComponentInstallResult = installResult,
            ApplyResult = applyResult,
            Logs = logs
        };
    }

    private static InstallFlowLogEntry CreateInstallCompletionLog(
        ShellGameCardModel selectedGame,
        ComponentInstallContext context,
        ComponentInstallResult installResult,
        InstallResultApplyResult applyResult,
        long durationMs)
    {
        var message = FormatInstallCompletionLog(
            selectedGame,
            context,
            installResult,
            applyResult,
            durationMs);

        return applyResult.FinalSuccess
            ? Info("install", message)
            : Error("install", message);
    }

    private static string FormatInstallCompletionLog(
        ShellGameCardModel selectedGame,
        ComponentInstallContext context,
        ComponentInstallResult installResult,
        InstallResultApplyResult applyResult,
        long durationMs)
    {
        var steps = installResult?.Steps ?? Array.Empty<ComponentInstallStepResult>();
        var failedStep = installResult?.FailedStep
            ?? steps.FirstOrDefault(static step => step.Status == ComponentInstallStatus.Failed);
        var failureCode = !string.IsNullOrWhiteSpace(applyResult.ConfigFailureCode)
            ? applyResult.ConfigFailureCode
            : NormalizeStatusCode(failedStep?.ErrorCode, "none");
        var baseMessage =
            $"completed success={FormatBool(applyResult.FinalSuccess)} game_id={NormalizeStatusCode(selectedGame.GameId, "missing")} variant={NormalizeStatusCode(context.OptiScalerVariant, "missing")} version={NormalizeStatusCode(context.OptiScalerDisplayVersion, NormalizeStatusCode(context.OptiScalerVersion, "missing"))} final_dll={NormalizeStatusCode(context.FinalDllName, "missing")} components={FormatComponentCounts(steps)} config_errors={applyResult.ConfigErrorCount} duration_ms={durationMs}";

        if (applyResult.FinalSuccess)
        {
            return baseMessage;
        }

        var failedComponent = failedStep is not null
            ? failedStep.Component.ToString()
            : !string.IsNullOrWhiteSpace(applyResult.ConfigFailureCode)
                ? "config"
                : "none";
        var failureMessage = failedStep is not null
            ? failedStep.Message
            : !string.IsNullOrWhiteSpace(applyResult.ConfigFailureCode)
                ? "config apply failed"
                : "-";

        return $"{baseMessage} failed_component={NormalizeStatusCode(failedComponent, "none")} code={NormalizeStatusCode(failureCode, "unknown_error")} message={Quote(NormalizeStatusCode(failureMessage, "-"))}";
    }

    private static bool IsExtraBundleReady(
        ShellGameCardModel selectedShellGame,
        IReadOnlyDictionary<string, object?> moduleDownloadLinks)
    {
        var alias = (ShellGameInstallMetadataResolver.GetExtraBundle(selectedShellGame) ?? "").Trim();
        if (string.IsNullOrWhiteSpace(alias))
        {
            return true;
        }

        if (!TryResolveModuleLinkEntry(moduleDownloadLinks, alias, out var row))
        {
            return false;
        }

        var url = InstallExecutionHelpers.ReadString(row, "url");
        return !string.IsNullOrWhiteSpace(url);
    }

    private static InstallFlowLogEntry Info(string category, string message)
    {
        return new InstallFlowLogEntry
        {
            Level = "info",
            Category = category,
            Message = message
        };
    }

    private static InstallFlowLogEntry Warning(string category, string message)
    {
        return new InstallFlowLogEntry
        {
            Level = "warning",
            Category = category,
            Message = message
        };
    }

    private static InstallFlowLogEntry Error(string category, string message, Exception? exception = null)
    {
        return new InstallFlowLogEntry
        {
            Level = "error",
            Category = category,
            Message = message,
            Exception = exception
        };
    }

    private static string Format(string template, params object[] args)
    {
        return string.Format(CultureInfo.CurrentCulture, template ?? "", args ?? []);
    }

    private static string NormalizeStatusCode(string? value, string fallback)
    {
        var normalized = (value ?? "").Trim();
        return string.IsNullOrWhiteSpace(normalized) ? fallback : normalized;
    }

    private static string FormatBool(bool value)
    {
        return value.ToString().ToLowerInvariant();
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

            foreach (var token in finding.Evidence)
            {
                var normalized = (token ?? "").Trim();
                if (!string.IsNullOrWhiteSpace(normalized))
                {
                    names.Add(normalized);
                }
            }
        }

        return names
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static bool TryResolveModuleLinkEntry(
        IReadOnlyDictionary<string, object?> moduleDownloadLinks,
        string alias,
        out IReadOnlyDictionary<string, object?> entry)
    {
        if (moduleDownloadLinks.TryGetValue(alias, out var direct)
            && direct is IReadOnlyDictionary<string, object?> directEntry)
        {
            entry = directEntry;
            return true;
        }

        var normalizedAlias = InstallExecutionHelpers.NormalizeAlias(alias);
        foreach (var pair in moduleDownloadLinks)
        {
            if (!string.Equals(InstallExecutionHelpers.NormalizeAlias(pair.Key), normalizedAlias, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (pair.Value is not IReadOnlyDictionary<string, object?> normalizedEntry)
            {
                continue;
            }

            entry = normalizedEntry;
            return true;
        }

        entry = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        return false;
    }

    private static string FormatComponentCounts(IReadOnlyList<ComponentInstallStepResult> steps)
    {
        if (steps.Count == 0)
        {
            return "none";
        }

        var success = steps.Count(static step => step.Status == ComponentInstallStatus.Success);
        var skipped = steps.Count(static step => step.Status == ComponentInstallStatus.Skipped);
        var failed = steps.Count(static step => step.Status == ComponentInstallStatus.Failed);
        return $"success:{success},skipped:{skipped},failed:{failed}";
    }

    private static string Quote(string value)
    {
        var safeValue = value ?? "";
        var escaped = safeValue
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal)
            .Replace("\r", "\\r", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal);
        return $"\"{escaped}\"";
    }
}
