using OptiClick.Core.Abstractions;
using OptiClick.Core.OptiScaler;
using OptiClick.Core.Scan;
using OptiClick.Wpf.Install.Archives;
using OptiClick.Wpf.Install.Config;
using OptiClick.Wpf.Install.Execution;
using OptiClick.Wpf.Install.Flow;
using OptiClick.Wpf.Install.Gates;
using OptiClick.Wpf.Install.Planning;
using OptiClick.Wpf.Install.Presentation;
using OptiClick.Wpf.Install.UiState;
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
using OptiClick.Wpf.Shell.Selection;
using OptiClick.Wpf.Shell.Startup;
using OptiClick.Wpf.Shell.Support;
using OptiClick.Wpf.Shell.Update;
using OptiClick.Wpf.Shell.Wiki;
using OptiClick.Wpf.ViewModels.Sections;
using OptiClick.Wpf.ViewModels.Sections.Scan;
using OptiClick.Wpf.ViewModels.Shell;

namespace OptiClick.Wpf.ViewModels;

public sealed record MainViewModelRequiredDependencies
{
    public required IDialogService DialogService { get; init; }
    public required IRuntimeContextProvider RuntimeContextProvider { get; init; }
    public required IWritableAppLanguageProvider LanguageProvider { get; init; }
    public required IShellMockDataProvider MockDataProvider { get; init; }
}

public sealed record MainViewModelRuntimeDependencies
{
    public IOperatingSystemSupportPolicy? OperatingSystemSupportPolicy { get; init; }
    public IDeviceIdentityResolver? DeviceIdentityResolver { get; init; }
    public IRemoteDeviceIdentityRulesLoader? DeviceIdentityRulesLoader { get; init; }
    public IShellGameCardViewModelFactory? ShellGameCardViewModelFactory { get; init; }
    public IRemoteCatalogPipeline? RemoteCatalogPipeline { get; init; }
    public RuntimeContextFlowController? RuntimeContextFlowController { get; init; }
    public DeviceIdentityRulesFlowController? DeviceIdentityRulesFlowController { get; init; }
    public RuntimeCatalogFlowController? RuntimeCatalogFlowController { get; init; }
    public RuntimeEndpointStatusPresenter? RuntimeEndpointStatusPresenter { get; init; }
    internal MainRuntimeCatalogUiFlowController? RuntimeCatalogUiFlowController { get; init; }
    public GpuSelectionCoordinator? GpuSelectionCoordinator { get; init; }
    public RuntimeContextCoordinator? RuntimeContextCoordinator { get; init; }
    public RuntimeCatalogCoordinator? RuntimeCatalogCoordinator { get; init; }
    public ModuleDownloadLinkMapBuilder? ModuleDownloadLinkMapBuilder { get; init; }
    public IRemoteGpuBundleManifestClient? GpuBundleManifestClient { get; init; }
    public IGpuBundleManifestRuleResolver? GpuBundleManifestRuleResolver { get; init; }
}

public sealed record MainViewModelScanDependencies
{
    public IFolderPickerService? FolderPickerService { get; init; }
    public IScanFolderDiscoveryService? ScanFolderDiscoveryService { get; init; }
    public IScanFolderManifestStore? ScanFolderManifestStore { get; init; }
    public IScanFileSystemProbe? ScanFileSystemProbe { get; init; }
    public IShellGameScanPipeline? ScanPipeline { get; init; }
    public ScanFlowController? ScanFlowController { get; init; }
    public ScanFolderListController? ScanFolderListController { get; init; }
    public ScanFolderActionController? ScanFolderActionController { get; init; }
    public ScanResultCoordinatorFactory? ScanResultCoordinatorFactory { get; init; }
    public ScanOrchestratorFactory? ScanOrchestratorFactory { get; init; }
}

public sealed record MainViewModelInstallDependencies
{
    public IShellInstallSelectionBridge? InstallSelectionBridge { get; init; }
    public GameSelectionFlowController? GameSelectionFlowController { get; init; }
    public IInstallPlanBuilder? InstallPlanBuilder { get; init; }
    public IInstallStatusResolver? InstallStatusResolver { get; init; }
    public IComponentInstallParityReviewBuilder? ComponentInstallParityReviewBuilder { get; init; }
    public IConfigProfileApplier? ConfigProfileApplier { get; init; }
    public IniProfileEditor? IniProfileEditor { get; init; }
    public IInstallStartGateResolver? InstallStartGateResolver { get; init; }
    public IComponentInstallCoordinator? ComponentInstallCoordinator { get; init; }
    public IArchivePreparationCoordinator? ArchivePreparationCoordinator { get; init; }
    public IInstallRejectionPresentationResolver? InstallRejectionPresentationResolver { get; init; }
    public IInstallResultPresentationResolver? InstallResultPresentationResolver { get; init; }
    public InstallSelectionRequestBuilder? InstallSelectionRequestBuilder { get; init; }
    public InstallPlanInputBuilder? InstallPlanInputBuilder { get; init; }
    public ComponentInstallContextBuilder? ComponentInstallContextBuilder { get; init; }
    public InstallPopupPresenter? InstallPopupPresenter { get; init; }
    public InstallCompletionMessageBuilder? InstallCompletionMessageBuilder { get; init; }
    public ArchiveReadinessFlowController? ArchiveReadinessFlowController { get; init; }
    public ConfigApplyFlowController? ConfigApplyFlowController { get; init; }
    public IInstallResultApplier? InstallResultApplier { get; init; }
    public InstallFlowController? InstallFlowController { get; init; }
    public IOptiClickUninstallPlanBuilder? OptiClickUninstallPlanBuilder { get; init; }
    public IOptiClickUninstallExecutor? OptiClickUninstallExecutor { get; init; }
}

public sealed record MainViewModelShellUiDependencies
{
    public ShellNavigationState? NavigationState { get; init; }
    public ShellChromeViewModels? ShellChrome { get; init; }
    public UserSettingsController? UserSettingsController { get; init; }
    public ISupportedGamesWikiMarkdownLoader? SupportedGamesWikiMarkdownLoader { get; init; }
    public LocalizationStateController? LocalizationStateController { get; init; }
    public MainViewModelBusyStateApplier? BusyStateApplier { get; init; }
    public FlowLogDispatcher? FlowLogDispatcher { get; init; }
    public MainViewModelFlowRequestFactory? FlowRequestFactory { get; init; }
    public MainViewModelResultApplier? ResultApplier { get; init; }
    internal MainShellInteractionControllers? ShellInteractionControllers { get; init; }
}

public sealed record MainViewModelShellDialogDependencies
{
    public DialogHostViewModel? DialogHost { get; init; }
    public DialogPresenter? DialogPresenter { get; init; }
    public OnceDialogGate? RemoteCatalogDialogGate { get; init; }
    public InstallManagementDialogHostViewModel? InstallManagementDialogHost { get; init; }
    public IInstallManagementDialogService? InstallManagementDialogService { get; init; }
}

public sealed record MainViewModelShellSectionDependencies
{
    public ShellSectionsFactory? ShellSectionsFactory { get; init; }
    public ShellSectionsCompositionFactory? ShellSectionsCompositionFactory { get; init; }
}

public sealed record MainViewModelAppDependencies
{
    public IAppVersionProvider? AppVersionProvider { get; init; }
    public IAppUpdateVersionComparer? AppUpdateVersionComparer { get; init; }
    public IAppUpdateService? AppUpdateService { get; init; }
    public IAppUpdateExecutionService? AppUpdateExecutionService { get; init; }
    public AppUpdateDialogPresenter? AppUpdateDialogPresenter { get; init; }
    public AppUpdateFlowController? AppUpdateFlowController { get; init; }
    public AppUpdateCoordinator? AppUpdateCoordinator { get; init; }
    public IAppLogger? AppLogger { get; init; }
    public IAppLocalDataPathProvider? LocalDataPathProvider { get; init; }
    public IAppStringsProvider? AppStringsProvider { get; init; }
    public IAppUserSettingsStore? UserSettingsStore { get; init; }
    public IFirstRunStateStore? FirstRunStateStore { get; init; }
    public IOptiScalerSettingsApplicationService? OptiScalerSettingsApplicationService { get; init; }
    public MainViewModelShellUiDependencies? ShellUi { get; init; }
    public MainViewModelShellDialogDependencies? ShellDialogs { get; init; }
    public MainViewModelShellSectionDependencies? ShellSections { get; init; }
    public DialogHostViewModel? DialogHost { get; init; }
    public InstallManagementDialogHostViewModel? InstallManagementDialogHost { get; init; }
    public IInstallManagementDialogService? InstallManagementDialogService { get; init; }
    public IContactIssueLinkBuilder? ContactIssueLinkBuilder { get; init; }
    public IExternalUrlLauncher? ExternalUrlLauncher { get; init; }
    public SupportActionController? SupportActionController { get; init; }
    public SupportIssueContextBuilder? SupportIssueContextBuilder { get; init; }
    public ShellNavigationState? NavigationState { get; init; }
    public ShellChromeViewModels? ShellChrome { get; init; }
    public DialogPresenter? DialogPresenter { get; init; }
    public OnceDialogGate? RemoteCatalogDialogGate { get; init; }
    public RuntimeHeaderPresenter? RuntimeHeaderPresenter { get; init; }
    public UserSettingsController? UserSettingsController { get; init; }
    public ISupportedGamesWikiMarkdownLoader? SupportedGamesWikiMarkdownLoader { get; init; }
    public StartupNoticePresenter? StartupNoticePresenter { get; init; }
    public StartupAnnouncementFlowController? StartupAnnouncementFlowController { get; init; }
    public SelectionPopupCoordinator? SelectionPopupCoordinator { get; init; }
    public ShellCommandActionController? ShellCommandActionController { get; init; }
    public LocalizationStateController? LocalizationStateController { get; init; }
    public RuntimeSummaryStateController? RuntimeSummaryStateController { get; init; }
    public MainViewModelBusyStateApplier? BusyStateApplier { get; init; }
    public FlowLogDispatcher? FlowLogDispatcher { get; init; }
    public MainViewModelFlowRequestFactory? FlowRequestFactory { get; init; }
    public MainViewModelResultApplier? ResultApplier { get; init; }
    internal MainShellInteractionControllers? ShellInteractionControllers { get; init; }
    public ShellSectionsFactory? ShellSectionsFactory { get; init; }
    public ShellSectionsCompositionFactory? ShellSectionsCompositionFactory { get; init; }
    public GameCardSelectionStateController? GameCardSelectionStateController { get; init; }
    public IGameMasterCoverPrefetchService? GameMasterCoverPrefetchService { get; init; }
    public ICoverCacheBootstrapService? CoverCacheBootstrapService { get; init; }
    public StartupBackgroundTaskManager? StartupBackgroundTaskManager { get; init; }
    public ArchiveReadinessRefreshCoordinator? ArchiveReadinessRefreshCoordinator { get; init; }
    public ArchiveReadinessWarmupController? ArchiveReadinessWarmupController { get; init; }
    public StartupPreparationCoordinator? StartupPreparationCoordinator { get; init; }
    public StartupFlowCoordinator? StartupFlowCoordinator { get; init; }
    internal InstallExecutionCoordinator? InstallExecutionCoordinator { get; init; }
    internal UninstallFlowCoordinator? UninstallFlowCoordinator { get; init; }
    internal MainInstallArchiveReadinessController? MainInstallArchiveReadinessController { get; init; }
    internal MainInstallPreparationController? MainInstallPreparationController { get; init; }
    internal MainInstallExecutionBridge? MainInstallExecutionBridge { get; init; }
    internal MainInstallInteractionController? MainInstallInteractionController { get; init; }
    internal MainUninstallInteractionController? MainUninstallInteractionController { get; init; }
    internal MainInstallCompletionController? MainInstallCompletionController { get; init; }
    internal MainOptiScalerSettingsController? MainOptiScalerSettingsController { get; init; }
    internal MainSelectionInteractionController? MainSelectionInteractionController { get; init; }
    internal MainSelectionRecomputeController? MainSelectionRecomputeController { get; init; }
    internal MainLanguageChangeController? MainLanguageChangeController { get; init; }
    internal MainVisibleGameCardRefreshController? MainVisibleGameCardRefreshController { get; init; }
    internal MainStartupRuntimeFacade? MainStartupRuntimeFacade { get; init; }
    internal MainStartupFlowController? MainStartupFlowController { get; init; }
    internal MainStartupDialogsController? MainStartupDialogsController { get; init; }
}
