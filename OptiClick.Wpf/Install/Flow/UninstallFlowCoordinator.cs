using OptiClick.Core.Install;
using OptiClick.Wpf.Install.Execution;
using OptiClick.Wpf.Install.Planning;
using OptiClick.Wpf.Install.Uninstall;
using OptiClick.Wpf.Logging;
using OptiClick.Wpf.Models;
using OptiClick.Wpf.Shell.Dialogs;
using OptiClick.Wpf.Shell.RuntimeData;
using OptiClick.Wpf.ViewModels;

namespace OptiClick.Wpf.Install.Flow;

public sealed class UninstallFlowCoordinator
{
    private readonly UninstallFlowExecutionUseCase _executionUseCase;
    private readonly UninstallFlowDialogRequestFactory _dialogRequestFactory;
    private readonly DialogPresenter _dialogPresenter;
    private readonly IAppLogger _appLogger;

    public UninstallFlowCoordinator(
        IOptiClickUninstallPlanBuilder planBuilder,
        IOptiClickUninstallExecutor executor,
        DialogPresenter dialogPresenter,
        IAppLogger appLogger)
        : this(
            new UninstallFlowExecutionUseCase(
                planBuilder,
                executor),
            new UninstallFlowDialogRequestFactory(),
            dialogPresenter,
            appLogger)
    {
    }

    internal UninstallFlowCoordinator(
        UninstallFlowExecutionUseCase executionUseCase,
        UninstallFlowDialogRequestFactory dialogRequestFactory,
        DialogPresenter dialogPresenter,
        IAppLogger appLogger)
    {
        _executionUseCase = executionUseCase ?? throw new ArgumentNullException(nameof(executionUseCase));
        _dialogRequestFactory = dialogRequestFactory ?? throw new ArgumentNullException(nameof(dialogRequestFactory));
        _dialogPresenter = dialogPresenter ?? throw new ArgumentNullException(nameof(dialogPresenter));
        _appLogger = appLogger ?? throw new ArgumentNullException(nameof(appLogger));
    }

    public async Task RunAsync(
        UninstallFlowCoordinatorRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Text);
        ArgumentNullException.ThrowIfNull(request.UiActions);

        var executionRequest = new UninstallFlowExecutionRequest
        {
            ExecutionDescriptor = request.ExecutionDescriptor,
            SelectedGameId = request.SelectedGameId,
            TargetPath = request.TargetPath,
            SelectionSnapshot = request.SelectionSnapshot,
            EngineIniProfileRows = request.EngineIniProfileRows
        };
        var planResult = _executionUseCase.BuildPlan(executionRequest);
        WriteLogs(planResult.Logs);

        if (!planResult.CanExecute)
        {
            var dialogRequest = CreateDialogRequest(planResult, request.Text);
            if (dialogRequest is not null)
            {
                await _dialogPresenter.ShowSafelyAsync(dialogRequest, cancellationToken);
            }

            return;
        }

        var confirmationRequest = CreateDialogRequest(planResult, request.Text);
        var confirmationResult = confirmationRequest is null
            ? AppDialogResult.None
            : await _dialogPresenter.ShowSafelyAsync(confirmationRequest, cancellationToken);
        var confirmed = confirmationResult == AppDialogResult.Continue;
        _appLogger.Info(
            MainViewModelLogCategories.UninstallFlow,
            UninstallFlowLogFormatter.FormatConfirmationResult(confirmed, confirmationResult.ToString()));
        if (!confirmed)
        {
            return;
        }

        request.UiActions.ApplyInstallBusyState(true, request.Text.OperationOverlayUninstalling);
        request.UiActions.ApplySettingsStatusText(request.Text.UninstallInProgressStatus);
        UninstallFlowExecutionResult executionResult;
        try
        {
            executionResult = await _executionUseCase.ExecuteAsync(
                executionRequest,
                planResult.Plan,
                cancellationToken);
        }
        finally
        {
            request.UiActions.ApplyInstallBusyState(false, "");
        }

        WriteLogs(executionResult.Logs);
        var completionRequest = CreateDialogRequest(executionResult, request.Text);
        if (completionRequest is not null)
        {
            await _dialogPresenter.ShowSafelyAsync(completionRequest, cancellationToken);
        }

        if (executionResult.ShouldRefreshSelection)
        {
            await request.UiActions.RefreshSelectionAfterUninstallAsync(cancellationToken);
        }
    }

    private AppDialogRequest? CreateDialogRequest(
        UninstallFlowPlanResult result,
        UninstallFlowText text)
    {
        return result.DialogKind switch
        {
            UninstallFlowDialogKind.NoRemovableItems => _dialogRequestFactory.CreateNoRemovableItems(text),
            UninstallFlowDialogKind.ValidationFailed => _dialogRequestFactory.CreateValidationFailed(text),
            UninstallFlowDialogKind.Confirmation => _dialogRequestFactory.CreateConfirmation(result.Plan, text),
            _ => null
        };
    }

    private AppDialogRequest? CreateDialogRequest(
        UninstallFlowExecutionResult result,
        UninstallFlowText text)
    {
        return result.DialogKind == UninstallFlowDialogKind.Completion
            ? _dialogRequestFactory.CreateCompletion(result.ExecutionResult, text)
            : null;
    }

    private void WriteLogs(IEnumerable<UninstallFlowLogEntry> logs)
    {
        foreach (var log in logs ?? [])
        {
            if (log.Level == UninstallFlowLogLevel.Warning)
            {
                _appLogger.Warning(MainViewModelLogCategories.UninstallFlow, log.Message);
                continue;
            }

            _appLogger.Info(MainViewModelLogCategories.UninstallFlow, log.Message);
        }
    }
}

public sealed record UninstallFlowCoordinatorRequest
{
    public required InstallExecutionDescriptor ExecutionDescriptor { get; init; }
    public required string SelectedGameId { get; init; }
    public required string TargetPath { get; init; }
    public required UninstallFlowText Text { get; init; }
    public required UninstallFlowSelectionSnapshot SelectionSnapshot { get; init; }
    public required IReadOnlyList<RuntimeDataRawRow> EngineIniProfileRows { get; init; }
    public required UninstallFlowCoordinatorUiActions UiActions { get; init; }
}

public sealed record UninstallFlowCoordinatorUiActions
{
    public required Action<bool, string> ApplyInstallBusyState { get; init; }
    public required Action<string> ApplySettingsStatusText { get; init; }
    public required Func<CancellationToken, Task> RefreshSelectionAfterUninstallAsync { get; init; }
}
