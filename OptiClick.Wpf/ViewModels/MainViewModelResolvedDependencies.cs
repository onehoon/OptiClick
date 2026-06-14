using OptiClick.Core.Abstractions;
using OptiClick.Core.Scan;
using OptiClick.Wpf.Install.Archives;
using OptiClick.Wpf.Install.Flow;
using OptiClick.Wpf.Install.Presentation;
using OptiClick.Wpf.Install.Uninstall;
using OptiClick.Wpf.Localization;
using OptiClick.Wpf.Logging;
using OptiClick.Wpf.Shell.Runtime.DeviceIdentity;
using OptiClick.Wpf.Shell.RuntimeData;
using OptiClick.Wpf.Shell.Games.GpuBundle;
using OptiClick.Wpf.Services;
using OptiClick.Wpf.Shell.Actions;
using OptiClick.Wpf.Shell.Dialogs;
using OptiClick.Wpf.Shell.Flow;
using OptiClick.Wpf.Shell.Games;
using OptiClick.Wpf.Shell.Gpu;
using OptiClick.Wpf.Shell.Localization;
using OptiClick.Wpf.Shell.Navigation;
using OptiClick.Wpf.Shell.Runtime;
using OptiClick.Wpf.Shell.Scan;
using OptiClick.Wpf.Shell.Settings;
using OptiClick.Wpf.Shell.Startup;
using OptiClick.Wpf.Shell.Support;
using OptiClick.Wpf.Shell.Selection;
using OptiClick.Wpf.Shell.Update;
using OptiClick.Wpf.Shell.Wiki;
using OptiClick.Wpf.ViewModels.Sections;
using OptiClick.Wpf.ViewModels.Sections.Scan;
using OptiClick.Wpf.ViewModels.Shell;

namespace OptiClick.Wpf.ViewModels;

internal sealed record MainViewModelCompositionDependencies
{
    public required MainAppResolvedDependencies App { get; init; }
    public required MainShellResolvedDependencies Shell { get; init; }
    public required MainFeatureResolvedDependencies Features { get; init; }

    public MainRuntimeResolvedDependencies Runtime => Features.Runtime;
    public MainScanResolvedDependencies Scan => Features.Scan;
    public MainInstallResolvedDependencies Install => Features.Install;
    public MainStartupResolvedDependencies Startup => Features.Startup;
    public MainSelectionResolvedDependencies Selection => Features.Selection;
    public MainSupportResolvedDependencies Support => Features.Support;
    public MainUpdateResolvedDependencies Update => Features.Update;
    public MainShellSectionsResolvedDependencies ShellSections => Features.ShellSections;
    public MainRuntimeFlowResolvedDependencies RuntimeFlow => Features.RuntimeFlow;
    public MainSelectionScanResolvedDependencies SelectionScan => Features.SelectionScan;
}

internal sealed record MainFeatureResolvedDependencies
{
    public required MainRuntimeResolvedDependencies Runtime { get; init; }
    public required MainScanResolvedDependencies Scan { get; init; }
    public required MainInstallResolvedDependencies Install { get; init; }
    public required MainStartupResolvedDependencies Startup { get; init; }
    public required MainSelectionResolvedDependencies Selection { get; init; }
    public required MainSupportResolvedDependencies Support { get; init; }
    public required MainUpdateResolvedDependencies Update { get; init; }
    public required MainShellSectionsResolvedDependencies ShellSections { get; init; }
    public required MainRuntimeFlowResolvedDependencies RuntimeFlow { get; init; }
    public required MainSelectionScanResolvedDependencies SelectionScan { get; init; }
}

internal sealed record MainAppResolvedDependencies
{
    public required IWritableAppLanguageProvider LanguageProvider { get; init; }
    public required IShellMockDataProvider MockDataProvider { get; init; }
    public required IAppVersionProvider AppVersionProvider { get; init; }
    public required IAppLogger AppLogger { get; init; }
    public required IAppLocalDataPathProvider LocalDataPathProvider { get; init; }
    public required IAppStringsProvider AppStringsProvider { get; init; }
    public required IFirstRunStateStore FirstRunStateStore { get; init; }
}

internal sealed record MainRuntimeResolvedDependencies
{
    public required IOperatingSystemSupportPolicy OperatingSystemSupportPolicy { get; init; }
    public required IShellGameCardViewModelFactory? ShellGameCardViewModelFactory { get; init; }
    public required RuntimeContextFlowController RuntimeContextFlowController { get; init; }
    public required DeviceIdentityRulesFlowController DeviceIdentityRulesFlowController { get; init; }
    public required RuntimeCatalogFlowController RuntimeCatalogFlowController { get; init; }
    public required RuntimeEndpointStatusPresenter RuntimeEndpointStatusPresenter { get; init; }
    public required MainRuntimeCatalogUiFlowController RuntimeCatalogUiFlowController { get; init; }
    public required GpuSelectionCoordinator GpuSelectionCoordinator { get; init; }
    public required RuntimeContextCoordinator RuntimeContextCoordinator { get; init; }
    public required RuntimeCatalogCoordinator RuntimeCatalogCoordinator { get; init; }
    public required IRemoteGpuBundleManifestClient GpuBundleManifestClient { get; init; }
    public required IGpuBundleManifestRuleResolver GpuBundleManifestRuleResolver { get; init; }
    public required RuntimeHeaderPresenter RuntimeHeaderPresenter { get; init; }
    public required RuntimeSummaryStateController RuntimeSummaryStateController { get; init; }
}

internal sealed record MainScanResolvedDependencies
{
    public required IFolderPickerService? FolderPickerService { get; init; }
    public required IScanFolderDiscoveryService? ScanFolderDiscoveryService { get; init; }
    public required ScanFlowController ScanFlowController { get; init; }
    public required ScanFolderListController ScanFolderListController { get; init; }
    public required ScanVisibleGameResolver ScanVisibleGameResolver { get; init; }
    public required ScanFolderActionController ScanFolderActionController { get; init; }
    public required ScanResultCoordinatorFactory ScanResultCoordinatorFactory { get; init; }
    public required ScanOrchestratorFactory ScanOrchestratorFactory { get; init; }
}

internal sealed record MainShellResolvedDependencies
{
    public required MainShellUiResolvedDependencies Ui { get; init; }
    public required MainShellDialogResolvedDependencies Dialogs { get; init; }
    public required MainShellInteractionResolvedDependencies Interactions { get; init; }
    public required MainShellSectionResolvedDependencies Sections { get; init; }

    public ShellNavigationState NavigationState => Ui.NavigationState;
    public ShellChromeViewModels ShellChrome => Ui.ShellChrome;
    public DialogPresenter DialogPresenter => Dialogs.DialogPresenter;
    public InstallManagementDialogHostViewModel InstallManagementDialogHost => Dialogs.InstallManagementDialogHost;
    public IInstallManagementDialogService InstallManagementDialogService => Dialogs.InstallManagementDialogService;
    public OnceDialogGate RemoteCatalogDialogGate => Dialogs.RemoteCatalogDialogGate;
    public UserSettingsController UserSettingsController => Ui.UserSettingsController;
    public ISupportedGamesWikiMarkdownLoader SupportedGamesWikiMarkdownLoader => Ui.SupportedGamesWikiMarkdownLoader;
    public ShellCommandActionController ShellCommandActionController => Interactions.ShellCommandActionController;
    public LocalizationStateController LocalizationStateController => Ui.LocalizationStateController;
    public MainViewModelBusyStateApplier BusyStateApplier => Ui.BusyStateApplier;
    public FlowLogDispatcher FlowLogDispatcher => Ui.FlowLogDispatcher;
    public MainViewModelFlowRequestFactory FlowRequestFactory => Ui.FlowRequestFactory;
    public MainShellInteractionControllers ShellInteractionControllers => Interactions.ShellInteractionControllers;
    public DialogHostViewModel DialogHost => Dialogs.DialogHost;
    public MainViewModelResultApplier ResultApplier => Ui.ResultApplier;
    public ShellSectionsFactory ShellSectionsFactory => Sections.ShellSectionsFactory;
    public ShellSectionsCompositionFactory ShellSectionsCompositionFactory => Sections.ShellSectionsCompositionFactory;
}

internal sealed record MainShellUiResolvedDependencies
{
    public required ShellNavigationState NavigationState { get; init; }
    public required ShellChromeViewModels ShellChrome { get; init; }
    public required UserSettingsController UserSettingsController { get; init; }
    public required ISupportedGamesWikiMarkdownLoader SupportedGamesWikiMarkdownLoader { get; init; }
    public required LocalizationStateController LocalizationStateController { get; init; }
    public required MainViewModelBusyStateApplier BusyStateApplier { get; init; }
    public required FlowLogDispatcher FlowLogDispatcher { get; init; }
    public required MainViewModelFlowRequestFactory FlowRequestFactory { get; init; }
    public required MainViewModelResultApplier ResultApplier { get; init; }
}

internal sealed record MainShellDialogResolvedDependencies
{
    public required DialogPresenter DialogPresenter { get; init; }
    public required InstallManagementDialogHostViewModel InstallManagementDialogHost { get; init; }
    public required IInstallManagementDialogService InstallManagementDialogService { get; init; }
    public required OnceDialogGate RemoteCatalogDialogGate { get; init; }
    public required DialogHostViewModel DialogHost { get; init; }
}

internal sealed record MainShellInteractionResolvedDependencies
{
    public required ShellCommandActionController ShellCommandActionController { get; init; }
    public required MainShellInteractionControllers ShellInteractionControllers { get; init; }
}

internal sealed record MainShellSectionResolvedDependencies
{
    public required ShellSectionsFactory ShellSectionsFactory { get; init; }
    public required ShellSectionsCompositionFactory ShellSectionsCompositionFactory { get; init; }
}

internal sealed record MainStartupResolvedDependencies
{
    public required StartupNoticePresenter StartupNoticePresenter { get; init; }
    public required StartupAnnouncementFlowController StartupAnnouncementFlowController { get; init; }
    public required GameMasterCoverPrefetchCoordinator GameMasterCoverPrefetchCoordinator { get; init; }
    public required ICoverCacheBootstrapService CoverCacheBootstrapService { get; init; }
    public required StartupBackgroundTaskManager StartupBackgroundTaskManager { get; init; }
    public required ArchiveReadinessRefreshCoordinator ArchiveReadinessRefreshCoordinator { get; init; }
    public required ArchiveReadinessWarmupController ArchiveReadinessWarmupController { get; init; }
    public required StartupPreparationCoordinator StartupPreparationCoordinator { get; init; }
    public required StartupFlowCoordinator StartupFlowCoordinator { get; init; }
    public required MainStartupRuntimeFacade MainStartupRuntimeFacade { get; init; }
    public required MainStartupFlowController MainStartupFlowController { get; init; }
    public required MainStartupDialogsController MainStartupDialogsController { get; init; }
}

internal sealed record MainSelectionResolvedDependencies
{
    public required GameSelectionFlowController GameSelectionFlowController { get; init; }
    public required SelectionPopupCoordinator SelectionPopupCoordinator { get; init; }
    public required GameCardSelectionStateController GameCardSelectionStateController { get; init; }
    public required MainSelectionInteractionController MainSelectionInteractionController { get; init; }
    public required MainSelectionRecomputeController MainSelectionRecomputeController { get; init; }
    public required MainLanguageChangeController MainLanguageChangeController { get; init; }
    public required MainVisibleGameCardRefreshController MainVisibleGameCardRefreshController { get; init; }
}

internal sealed record MainSupportResolvedDependencies
{
    public required SupportActionController SupportActionController { get; init; }
    public required SupportIssueContextBuilder SupportIssueContextBuilder { get; init; }
}

internal sealed record MainUpdateResolvedDependencies
{
    public required AppUpdateFlowController AppUpdateFlowController { get; init; }
    public required AppUpdateCoordinator AppUpdateCoordinator { get; init; }
}

internal sealed record MainShellSectionsResolvedDependencies
{
    public required IShellMockDataProvider MockDataProvider { get; init; }
    public required DialogPresenter DialogPresenter { get; init; }
    public required ISupportedGamesWikiMarkdownLoader SupportedGamesWikiMarkdownLoader { get; init; }
    public required StartupBackgroundTaskManager StartupBackgroundTaskManager { get; init; }
    public required IAppLogger AppLogger { get; init; }
    public required IAppLocalDataPathProvider LocalDataPathProvider { get; init; }
    public required ShellSectionsFactory ShellSectionsFactory { get; init; }
    public required ShellSectionsCompositionFactory ShellSectionsCompositionFactory { get; init; }
}

internal sealed record MainRuntimeFlowResolvedDependencies
{
    public required RuntimeContextCoordinator RuntimeContextCoordinator { get; init; }
    public required DeviceIdentityRulesFlowController DeviceIdentityRulesFlowController { get; init; }
    public required GpuSelectionCoordinator GpuSelectionCoordinator { get; init; }
}

internal sealed record MainSelectionScanResolvedDependencies
{
    public required IShellGameCardViewModelFactory? ShellGameCardViewModelFactory { get; init; }
    public required ScanVisibleGameResolver ScanVisibleGameResolver { get; init; }
    public required GameSelectionFlowController GameSelectionFlowController { get; init; }
    public required SelectionPopupCoordinator SelectionPopupCoordinator { get; init; }
    public required GameCardSelectionStateController GameCardSelectionStateController { get; init; }
    public required GpuSelectionCoordinator GpuSelectionCoordinator { get; init; }
}

internal sealed record MainInstallResolvedDependencies
{
    public required GameSelectionFlowController GameSelectionFlowController { get; init; }
    public required ArchiveReadinessFlowController ArchiveReadinessFlowController { get; init; }
    public required InstallFlowController InstallFlowController { get; init; }
    public required InstallPopupPresenter InstallPopupPresenter { get; init; }
    public required IOptiClickUninstallPlanBuilder OptiClickUninstallPlanBuilder { get; init; }
    public required IOptiClickUninstallExecutor OptiClickUninstallExecutor { get; init; }
    public required InstallExecutionCoordinator InstallExecutionCoordinator { get; init; }
    public required UninstallFlowCoordinator UninstallFlowCoordinator { get; init; }
    public required MainInstallArchiveReadinessController MainInstallArchiveReadinessController { get; init; }
    public required MainInstallPreparationController MainInstallPreparationController { get; init; }
    public required MainInstallExecutionBridge MainInstallExecutionBridge { get; init; }
    public required MainInstallInteractionController MainInstallInteractionController { get; init; }
    public required MainUninstallInteractionController MainUninstallInteractionController { get; init; }
    public required MainInstallCompletionController MainInstallCompletionController { get; init; }
    public required MainOptiScalerSettingsController MainOptiScalerSettingsController { get; init; }
}

internal sealed record MainShellInteractionControllers
{
    public required OptiScalerDirtyNavigationGuard OptiScalerDirtyNavigationGuard { get; init; }
    public required MainAppUpdateInteractionController AppUpdateInteractionController { get; init; }
    public required MainUserSettingsApplyController UserSettingsApplyController { get; init; }
}
