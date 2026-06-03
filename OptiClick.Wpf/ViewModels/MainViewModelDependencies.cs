using OptiClick.Core.Abstractions;
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
using OptiClick.Wpf.Shell.Localization;
using OptiClick.Wpf.Shell.Navigation;
using OptiClick.Wpf.Shell.Runtime;
using OptiClick.Wpf.Shell.Scan;
using OptiClick.Wpf.Shell.Settings;
using OptiClick.Wpf.Shell.Selection;
using OptiClick.Wpf.Shell.Startup;
using OptiClick.Wpf.Shell.Support;
using OptiClick.Wpf.Shell.Wiki;
using OptiClick.Infrastructure.FileSystem;

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
    public ModuleDownloadLinkMapBuilder? ModuleDownloadLinkMapBuilder { get; init; }
    public IRemoteGpuBundleManifestClient? GpuBundleManifestClient { get; init; }
    public IGpuBundleManifestRuleResolver? GpuBundleManifestRuleResolver { get; init; }
}

public sealed record MainViewModelScanDependencies
{
    public IFolderPickerService? FolderPickerService { get; init; }
    public IScanFolderDiscoveryService? ScanFolderDiscoveryService { get; init; }
    public IScanFolderManifestStore? ScanFolderManifestStore { get; init; }
    public IShellGameScanPipeline? ScanPipeline { get; init; }
    public ScanFlowController? ScanFlowController { get; init; }
    public ScanFolderListController? ScanFolderListController { get; init; }
    public ScanFolderDialogPresenter? ScanFolderDialogPresenter { get; init; }
    public ScanFolderActionController? ScanFolderActionController { get; init; }
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

public sealed record MainViewModelAppDependencies
{
    public IAppVersionProvider? AppVersionProvider { get; init; }
    public IAppUpdateVersionComparer? AppUpdateVersionComparer { get; init; }
    public IAppUpdateService? AppUpdateService { get; init; }
    public IAppUpdateExecutionService? AppUpdateExecutionService { get; init; }
    public AppUpdateDialogPresenter? AppUpdateDialogPresenter { get; init; }
    public AppUpdateFlowController? AppUpdateFlowController { get; init; }
    public GameDetailsDialogPresenter? GameDetailsDialogPresenter { get; init; }
    public IAppLogger? AppLogger { get; init; }
    public IAppLocalDataPathProvider? LocalDataPathProvider { get; init; }
    public IAppStringsProvider? AppStringsProvider { get; init; }
    public IAppUserSettingsStore? UserSettingsStore { get; init; }
    public IFirstRunStateStore? FirstRunStateStore { get; init; }
    public IGpuVendorLogoResolver? GpuVendorLogoResolver { get; init; }
    public DialogHostViewModel? DialogHost { get; init; }
    public InstallManagementDialogHostViewModel? InstallManagementDialogHost { get; init; }
    public IInstallManagementDialogService? InstallManagementDialogService { get; init; }
    public IContactIssueLinkBuilder? ContactIssueLinkBuilder { get; init; }
    public IExternalUrlLauncher? ExternalUrlLauncher { get; init; }
    public SupportActionController? SupportActionController { get; init; }
    public SupportIssueContextBuilder? SupportIssueContextBuilder { get; init; }
    public ShellNavigationState? NavigationState { get; init; }
    public DialogPresenter? DialogPresenter { get; init; }
    public OnceDialogGate? RemoteCatalogDialogGate { get; init; }
    public RuntimeHeaderPresenter? RuntimeHeaderPresenter { get; init; }
    public UserSettingsController? UserSettingsController { get; init; }
    public ISupportedGamesWikiMarkdownLoader? SupportedGamesWikiMarkdownLoader { get; init; }
    public StartupNoticePresenter? StartupNoticePresenter { get; init; }
    public StartupAnnouncementFlowController? StartupAnnouncementFlowController { get; init; }
    public SettingsDialogPresenter? SettingsDialogPresenter { get; init; }
    public ShellCommandActionController? ShellCommandActionController { get; init; }
    public LocalizationStateController? LocalizationStateController { get; init; }
    public RuntimeSummaryStateController? RuntimeSummaryStateController { get; init; }
    public MainViewModelBusyStateApplier? BusyStateApplier { get; init; }
    public FlowLogDispatcher? FlowLogDispatcher { get; init; }
    public MainViewModelFlowRequestFactory? FlowRequestFactory { get; init; }
    public MainViewModelResultApplier? ResultApplier { get; init; }
    public GameCardSelectionStateController? GameCardSelectionStateController { get; init; }
    public IGameMasterCoverPrefetchService? GameMasterCoverPrefetchService { get; init; }
    public ICoverCacheBootstrapService? CoverCacheBootstrapService { get; init; }
    public StartupBackgroundTaskManager? StartupBackgroundTaskManager { get; init; }
    public ArchiveReadinessRefreshCoordinator? ArchiveReadinessRefreshCoordinator { get; init; }
    public ArchiveReadinessWarmupController? ArchiveReadinessWarmupController { get; init; }
    public StartupFlowCoordinator? StartupFlowCoordinator { get; init; }
}
