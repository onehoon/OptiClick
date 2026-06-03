using OptiClick.Core.Abstractions;
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
using OptiClick.Wpf.Shell.Localization;
using OptiClick.Wpf.Shell.Navigation;
using OptiClick.Wpf.Shell.Runtime;
using OptiClick.Wpf.Shell.Scan;
using OptiClick.Wpf.Shell.Settings;
using OptiClick.Wpf.Shell.Startup;
using OptiClick.Wpf.Shell.Support;
using OptiClick.Wpf.Shell.Selection;
using OptiClick.Wpf.Shell.Wiki;
using OptiClick.Infrastructure.FileSystem;

namespace OptiClick.Wpf.ViewModels;

public sealed record MainViewModelResolvedDependencies
{
    public required IWritableAppLanguageProvider LanguageProvider { get; init; }
    public required IShellMockDataProvider MockDataProvider { get; init; }

    public required IOperatingSystemSupportPolicy OperatingSystemSupportPolicy { get; init; }
    public required IShellGameCardViewModelFactory? ShellGameCardViewModelFactory { get; init; }
    public required RuntimeContextFlowController RuntimeContextFlowController { get; init; }
    public required DeviceIdentityRulesFlowController DeviceIdentityRulesFlowController { get; init; }
    public required RuntimeCatalogFlowController RuntimeCatalogFlowController { get; init; }
    public required RuntimeEndpointStatusPresenter RuntimeEndpointStatusPresenter { get; init; }
    public required IRemoteGpuBundleManifestClient GpuBundleManifestClient { get; init; }
    public required IGpuBundleManifestRuleResolver GpuBundleManifestRuleResolver { get; init; }

    public required IFolderPickerService? FolderPickerService { get; init; }
    public required IScanFolderDiscoveryService? ScanFolderDiscoveryService { get; init; }
    public required ScanFlowController ScanFlowController { get; init; }

    public required GameSelectionFlowController GameSelectionFlowController { get; init; }
    public required ArchiveReadinessFlowController ArchiveReadinessFlowController { get; init; }
    public required InstallFlowController InstallFlowController { get; init; }
    public required IOptiClickUninstallPlanBuilder OptiClickUninstallPlanBuilder { get; init; }
    public required IOptiClickUninstallExecutor OptiClickUninstallExecutor { get; init; }

    public required IAppVersionProvider AppVersionProvider { get; init; }
    public required AppUpdateFlowController AppUpdateFlowController { get; init; }
    public required GameDetailsDialogPresenter GameDetailsDialogPresenter { get; init; }
    public required IAppLogger AppLogger { get; init; }
    public required IAppLocalDataPathProvider LocalDataPathProvider { get; init; }
    public required IAppStringsProvider AppStringsProvider { get; init; }
    public required IFirstRunStateStore FirstRunStateStore { get; init; }
    public required ShellNavigationState NavigationState { get; init; }
    public required DialogPresenter DialogPresenter { get; init; }
    public required InstallManagementDialogHostViewModel InstallManagementDialogHost { get; init; }
    public required IInstallManagementDialogService InstallManagementDialogService { get; init; }
    public required OnceDialogGate RemoteCatalogDialogGate { get; init; }
    public required RuntimeHeaderPresenter RuntimeHeaderPresenter { get; init; }
    public required UserSettingsController UserSettingsController { get; init; }
    public required ISupportedGamesWikiMarkdownLoader SupportedGamesWikiMarkdownLoader { get; init; }
    public required ScanFolderListController ScanFolderListController { get; init; }
    public required ScanVisibleGameResolver ScanVisibleGameResolver { get; init; }
    public required StartupNoticePresenter StartupNoticePresenter { get; init; }
    public required StartupAnnouncementFlowController StartupAnnouncementFlowController { get; init; }
    public required ShellCommandActionController ShellCommandActionController { get; init; }
    public required LocalizationStateController LocalizationStateController { get; init; }
    public required RuntimeSummaryStateController RuntimeSummaryStateController { get; init; }
    public required MainViewModelBusyStateApplier BusyStateApplier { get; init; }
    public required ScanFolderDialogPresenter ScanFolderDialogPresenter { get; init; }
    public required ScanFolderActionController ScanFolderActionController { get; init; }
    public required SupportActionController SupportActionController { get; init; }
    public required SupportIssueContextBuilder SupportIssueContextBuilder { get; init; }
    public required InstallPopupPresenter InstallPopupPresenter { get; init; }
    public required FlowLogDispatcher FlowLogDispatcher { get; init; }
    public required MainViewModelFlowRequestFactory FlowRequestFactory { get; init; }
    public required DialogHostViewModel DialogHost { get; init; }
    public required MainViewModelResultApplier ResultApplier { get; init; }
    public required GameCardSelectionStateController GameCardSelectionStateController { get; init; }
    public required IGameMasterCoverPrefetchService GameMasterCoverPrefetchService { get; init; }
    public required ICoverCacheBootstrapService CoverCacheBootstrapService { get; init; }
    public required StartupBackgroundTaskManager StartupBackgroundTaskManager { get; init; }
    public required ArchiveReadinessRefreshCoordinator ArchiveReadinessRefreshCoordinator { get; init; }
    public required ArchiveReadinessWarmupController ArchiveReadinessWarmupController { get; init; }
    public required StartupFlowCoordinator StartupFlowCoordinator { get; init; }
}
