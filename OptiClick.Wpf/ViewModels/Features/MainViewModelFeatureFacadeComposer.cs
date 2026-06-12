using OptiClick.Wpf.ViewModels;
using OptiClick.Wpf.Shell.Flow;
using OptiClick.Wpf.Shell.Runtime;
using OptiClick.Wpf.ViewModels.Features.Runtime;
using OptiClick.Wpf.ViewModels.Features.Runtime.DeviceIdentity;
using OptiClick.Wpf.ViewModels.Features.Runtime.GpuManifest;
using OptiClick.Wpf.ViewModels.Features.Shell;
using OptiClick.Wpf.ViewModels.Features.Shell.ShellCommand;
using OptiClick.Wpf.ViewModels.Features.Shell.StartupAnnouncement;
using OptiClick.Wpf.ViewModels.Features.Shell.UserSettings;
using OptiClick.Wpf.ViewModels.Features.Startup;
using OptiClick.Wpf.ViewModels.Ports.ShellInteractionContext;

namespace OptiClick.Wpf.ViewModels.Features;

internal static class MainViewModelFeatureFacadeComposer
{
    public static MainShellInteractionFeatureFacade ComposeShellInteraction(
        MainShellInteractionFeatureCompositionInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var contextFactory = new MainShellInteractionContextFactory(input.Context);
        var shellCommand = new MainShellCommandInteractionFeature(
            input.ShellDependencies.ShellCommandActionController,
            contextFactory);
        var userSettings = new MainUserSettingsInteractionFeature(
            input.ShellDependencies.UserSettingsController,
            input.ShellDependencies.ShellInteractionControllers.UserSettingsApplyController,
            contextFactory);
        var startupAnnouncement = new MainStartupAnnouncementInteractionFeature(
            input.StartupDependencies.StartupAnnouncementFlowController,
            contextFactory);

        return new MainShellInteractionFeatureFacade(
            input.AppDependencies.AppLogger,
            input.AppDependencies.AppVersionProvider,
            input.ShellDependencies.LocalizationStateController,
            input.ShellDependencies.BusyStateApplier,
            input.ShellDependencies.ShellInteractionControllers.OptiScalerDirtyNavigationGuard,
            input.ShellDependencies.ShellInteractionControllers.AppUpdateInteractionController,
            input.SelectionDependencies.GameDetailsDialogPresenter,
            contextFactory,
            shellCommand,
            userSettings,
            startupAnnouncement);
    }

    public static MainStartupFeatureFacade ComposeStartup(MainStartupFeatureCompositionInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new MainStartupFeatureFacade(
            input.StartupDependencies.MainStartupFlowController,
            input.StartupDependencies.MainStartupDialogsController,
            input.StartupShellFacade.StartupFlowContextFactory,
            input.StartupShellFacade.StartupPreparationContextFactory,
            input.StartupDependencies.GameMasterCoverPrefetchCoordinator,
            input.StartupDependencies.StartupBackgroundTaskManager,
            input.StartupDependencies.StartupPreparationCoordinator,
            input.DialogsPort,
            input.CoverPrefetchPort);
    }

    public static MainRuntimeFeatureFacade ComposeRuntime(MainRuntimeFeatureCompositionInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var deviceIdentity = new MainRuntimeDeviceIdentityFeature(
            input.RuntimeShellState,
            input.StartupDependencies.MainStartupRuntimeFacade,
            input.OperationLocks.DeviceRulesRefreshLock,
            input.RuntimeDependencies.DeviceIdentityRulesFlowController,
            input.RuntimeShellFacade.RuntimeFlowContextFactory,
            input.RuntimeDependencies.RuntimeSummaryStateController,
            input.FlowLogDispatcher);
        var gpuManifest = new MainRuntimeGpuManifestFeature(
            input.RuntimeDependencies.GpuSelectionCoordinator,
            input.RuntimeDependencies.RuntimeCatalogUiFlowController,
            input.RuntimeShellFacade.CatalogUiFlowContextFactory);

        return new MainRuntimeFeatureFacade(
            input.RuntimeShellState,
            input.RuntimeDependencies.OperatingSystemSupportPolicy,
            input.RuntimeDependencies.ShellGameCardViewModelFactory is not null,
            input.StartupDependencies.MainStartupRuntimeFacade,
            input.RuntimeDependencies.RuntimeCatalogCoordinator,
            input.RuntimeDependencies.RuntimeCatalogUiFlowController,
            input.RuntimeShellFacade.CatalogUiFlowContextFactory,
            input.RuntimeShellFacade.RuntimeFlowContextFactory,
            input.RuntimeDependencies.RuntimeSummaryStateController,
            input.FlowLogDispatcher,
            input.BuildCatalogRefreshRequest,
            deviceIdentity,
            gpuManifest);
    }

}

internal sealed record MainShellInteractionFeatureCompositionInput
{
    public required MainAppResolvedDependencies AppDependencies { get; init; }
    public required MainShellResolvedDependencies ShellDependencies { get; init; }
    public required MainStartupResolvedDependencies StartupDependencies { get; init; }
    public required MainSelectionResolvedDependencies SelectionDependencies { get; init; }
    public required MainShellInteractionContextFactoryInput Context { get; init; }
}

internal sealed record MainStartupFeatureCompositionInput
{
    public required MainStartupResolvedDependencies StartupDependencies { get; init; }
    public required MainStartupShellFacade StartupShellFacade { get; init; }
    public required MainStartupDialogsPort DialogsPort { get; init; }
    public required MainStartupCoverPrefetchPort CoverPrefetchPort { get; init; }
}

internal sealed record MainRuntimeFeatureCompositionInput
{
    public required MainRuntimeResolvedDependencies RuntimeDependencies { get; init; }
    public required MainStartupResolvedDependencies StartupDependencies { get; init; }
    public required MainRuntimeShellFacade RuntimeShellFacade { get; init; }
    public required RuntimeShellState RuntimeShellState { get; init; }
    public required MainShellOperationLocks OperationLocks { get; init; }
    public required FlowLogDispatcher FlowLogDispatcher { get; init; }
    public required Func<MainRuntimeDataCatalogRefreshRequest> BuildCatalogRefreshRequest { get; init; }
}
