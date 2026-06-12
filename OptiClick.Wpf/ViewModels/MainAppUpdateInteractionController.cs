using OptiClick.Wpf.Localization;
using OptiClick.Wpf.Models;
using OptiClick.Wpf.Services;
using OptiClick.Wpf.Shell.Flow;
using OptiClick.Wpf.Shell.RuntimeData;
using OptiClick.Wpf.Shell.Selection;
using OptiClick.Wpf.Shell.Update;

namespace OptiClick.Wpf.ViewModels;

internal sealed class MainAppUpdateInteractionController
{
    public Task ShowStartupUpdateCheckDialogAsync(
        MainAppUpdateInteractionContext context,
        CancellationToken cancellationToken = default)
    {
        return ShowUpdateCheckDialogAsync(context, AppUpdateTrigger.Startup, cancellationToken);
    }

    public async Task ShowUpdateCheckDialogAsync(
        MainAppUpdateInteractionContext context,
        AppUpdateTrigger trigger,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var strings = context.ReadStrings();
        var coordinatorResult = context.AppUpdateCoordinator.BeginCheck(
            new AppUpdateCoordinatorRequest
            {
                Trigger = trigger,
                LatestRuntimeData = context.ReadLatestRuntimeData(),
                CurrentVersion = context.ReadCurrentAppVersion(),
                Text = AppUpdateCoordinatorText.FromAppStrings(strings),
                IsAppUpdateInProgress = context.IsAppUpdateInProgress()
            });

        context.DispatchFlowLogs(coordinatorResult.Logs, MainViewModelLogCategories.AppUpdate);

        if (!string.IsNullOrWhiteSpace(coordinatorResult.StatusText))
        {
            context.SetSettingsStatusText(coordinatorResult.StatusText);
        }

        if (!coordinatorResult.ShouldContinue
            || !coordinatorResult.ShouldShowDialog
            || coordinatorResult.DialogRequest is null)
        {
            return;
        }

        var confirm = await context.ShowDialogAsync(coordinatorResult.DialogRequest, cancellationToken);
        if (!coordinatorResult.IsUpdateAvailable)
        {
            return;
        }

        if (!AppUpdateCoordinator.IsDialogConfirmed(confirm))
        {
            context.SetSettingsStatusText(strings.UpdateCanceled);
            return;
        }

        if (!coordinatorResult.TryGetUpdateInfo(out var updateInfo))
        {
            context.SetSettingsStatusText(strings.UpdateFailed);
            context.LogError(coordinatorResult.MissingUpdateInfoLogMessage);
            return;
        }

        await ExecuteConfirmedAppUpdateAsync(context, updateInfo, cancellationToken);
    }

    private static async Task ExecuteConfirmedAppUpdateAsync(
        MainAppUpdateInteractionContext context,
        AppUpdateInfo updateInfo,
        CancellationToken cancellationToken)
    {
        var strings = context.ReadStrings();
        context.ApplyBusyStateUpdate(
            context.BusyStateApplier.CreateAppUpdateBusyState(
                true,
                context.IsInstallExecutionInProgress(),
                context.ReadSelectionState(),
                strings.OperationOverlayUpdating));
        try
        {
            context.SetSettingsStatusText(strings.UpdatePreparing);
            var executeResult = await context.AppUpdateFlowController.ExecuteConfirmedUpdateAsync(
                new AppUpdateConfirmedRequest
                {
                    UpdateInfo = updateInfo,
                    Text = AppUpdateFlowText.FromAppStrings(strings)
                },
                cancellationToken);
            await ApplyAppUpdateFlowResultAsync(context, executeResult, cancellationToken);
        }
        finally
        {
            context.ApplyBusyStateUpdate(
                context.BusyStateApplier.CreateAppUpdateBusyState(
                    false,
                    context.IsInstallExecutionInProgress(),
                    context.ReadSelectionState()));
        }
    }

    private static async Task ApplyAppUpdateFlowResultAsync(
        MainAppUpdateInteractionContext context,
        AppUpdateExecutionFlowResult result,
        CancellationToken cancellationToken)
    {
        context.DispatchFlowLogs(result.Logs, MainViewModelLogCategories.AppUpdate);
        var update = context.ResultApplier.CreateAppUpdateStateUpdate(result);
        context.ApplyStateUpdate(update);

        if (update.DialogRequest is not null)
        {
            await context.ShowDialogAsync(update.DialogRequest, cancellationToken);
        }

        if (update.ShouldShutdown)
        {
            context.ShutdownApplication();
        }
    }
}

internal sealed class MainAppUpdateInteractionContext
{
    public required AppUpdateCoordinator AppUpdateCoordinator { get; init; }
    public required AppUpdateFlowController AppUpdateFlowController { get; init; }
    public required MainViewModelBusyStateApplier BusyStateApplier { get; init; }
    public required MainViewModelResultApplier ResultApplier { get; init; }
    public required Func<AppStrings> ReadStrings { get; init; }
    public required Func<RemoteRuntimeData> ReadLatestRuntimeData { get; init; }
    public required Func<string> ReadCurrentAppVersion { get; init; }
    public required Func<bool> IsAppUpdateInProgress { get; init; }
    public required Func<bool> IsInstallExecutionInProgress { get; init; }
    public required Func<ShellInstallSelectionState> ReadSelectionState { get; init; }
    public required Action<string> SetSettingsStatusText { get; init; }
    public required Action<IEnumerable<IFlowLogEntry>, string> DispatchFlowLogs { get; init; }
    public required Func<AppDialogRequest, CancellationToken, Task<AppDialogResult>> ShowDialogAsync { get; init; }
    public required Action<MainViewModelBusyStateUpdate> ApplyBusyStateUpdate { get; init; }
    public required Action<MainViewModelStateUpdate> ApplyStateUpdate { get; init; }
    public required Action<string> LogError { get; init; }
    public required Action ShutdownApplication { get; init; }
}
