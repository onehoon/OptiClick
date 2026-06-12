using OptiClick.Wpf.ViewModels.Ports.ShellInteractionContext;

namespace OptiClick.Wpf.ViewModels.Ports.ShellInteractionContext.AppUpdate;

internal sealed record MainAppUpdateInteractionContextCompositionInput
{
    public required MainAppResolvedDependencies AppDependencies { get; init; }
    public required MainShellResolvedDependencies ShellDependencies { get; init; }
    public required MainUpdateResolvedDependencies UpdateDependencies { get; init; }
    public required IMainAppUpdateInteractionAccess Access { get; init; }
}

internal static class MainAppUpdateInteractionContextComposer
{
    public static MainAppUpdateInteractionContextInput Compose(
        MainAppUpdateInteractionContextCompositionInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var access = input.Access;

        return new MainAppUpdateInteractionContextInput
        {
            AppUpdateCoordinator = input.UpdateDependencies.AppUpdateCoordinator,
            AppUpdateFlowController = input.UpdateDependencies.AppUpdateFlowController,
            BusyStateApplier = input.ShellDependencies.BusyStateApplier,
            ResultApplier = input.ShellDependencies.ResultApplier,
            ReadStrings = () => access.Strings,
            ReadLatestRuntimeData = () => access.LatestRuntimeData,
            ReadCurrentAppVersion = () => MainShellInteractionContextUtilities.NormalizeAppVersion(
                input.AppDependencies.AppVersionProvider.GetCurrentVersion()),
            IsAppUpdateInProgress = () => access.IsAppUpdateInProgress,
            IsInstallExecutionInProgress = () => access.IsInstallExecutionInProgress,
            ReadSelectionState = () => access.SelectionState,
            SetSettingsStatusText = access.SetSettingsStatusText,
            DispatchFlowLogs = input.ShellDependencies.FlowLogDispatcher.Dispatch,
            ShowDialogAsync = input.ShellDependencies.DialogPresenter.ShowSafelyAsync,
            ApplyBusyStateUpdate = access.ApplyBusyStateUpdate,
            ApplyStateUpdate = access.ApplyStateUpdate,
            LogError = message => input.AppDependencies.AppLogger.Error(MainViewModelLogCategories.AppUpdate, message),
            ShutdownApplication = access.ShutdownApplication
        };
    }
}
