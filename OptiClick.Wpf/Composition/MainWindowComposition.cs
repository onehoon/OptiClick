using OptiClick.Wpf.Logging;
using OptiClick.Wpf.Configuration;
using OptiClick.Wpf.Install.Archives;
using OptiClick.Wpf.Install.Execution;
using OptiClick.Wpf.Install.FileSystem;
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
using OptiClick.Wpf.Shell.Selection;
using OptiClick.Wpf.Shell.Settings;
using OptiClick.Wpf.Shell.Startup;
using OptiClick.Wpf.Shell.Update;
using OptiClick.Wpf.Shell.Wiki;
using OptiClick.Wpf.ViewModels;
using OptiClick.Wpf.ViewModels.Sections;
using OptiClick.Wpf.ViewModels.Sections.Scan;
using OptiClick.Wpf.ViewModels.Shell;
using System.Net.Http;

namespace OptiClick.Wpf.Composition;

public sealed class MainWindowComposition
{
    public MainViewModel CreateMainViewModel(
        AppSharedServices app,
        RuntimeCompositionServices runtime,
        ScanCompositionServices scan,
        InstallCompositionServices install,
        UpdateCompositionServices update,
        SupportCompositionServices support,
        bool seedMockGameCards = false,
        bool seedMockScanFolders = false)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(scan);
        ArgumentNullException.ThrowIfNull(install);
        ArgumentNullException.ThrowIfNull(update);
        ArgumentNullException.ThrowIfNull(support);

        var flowLogDispatcher = new FlowLogDispatcher(app.AppLogger);
        var flowRequestFactory = new MainViewModelFlowRequestFactory();
        var resultApplier = new MainViewModelResultApplier();
        var gpuVendorLogoResolver = new GpuVendorLogoResolver();
        var runtimeHeaderPresenter = new RuntimeHeaderPresenter(gpuVendorLogoResolver);
        var scanFolderListController = new ScanFolderListController(scan.ScanFolderManifestStore);
        var scanFolderDialogPresenter = new ScanFolderDialogPresenter();
        var scanFolderActionController = new ScanFolderActionController(
            scanFolderListController,
            scan.FolderPickerService,
            scanFolderDialogPresenter);
        var scanResultCoordinatorFactory = new ScanResultCoordinatorFactory();
        var scanOrchestratorFactory = new ScanOrchestratorFactory();
        var startupNoticePresenter = new StartupNoticePresenter();
        var gameMasterCoverPrefetchService = new GameMasterCoverPrefetchService();
        var shellCommandActionController = new ShellCommandActionController(
            startupNoticePresenter,
            support.SupportIssueContextBuilder,
            support.SupportActionController);
        var localizationStateController = new LocalizationStateController();
        var runtimeSummaryStateController = new RuntimeSummaryStateController(
            runtime.DeviceIdentityResolver,
            runtimeHeaderPresenter);
        var gpuSelectionCoordinator = new GpuSelectionCoordinator(GpuSelectionCoordinator.DefaultMaxSupportedGpuCount);
        var runtimeContextCoordinator = new RuntimeContextCoordinator(
            runtime.RuntimeContextFlowController,
            runtimeSummaryStateController,
            flowLogDispatcher,
            gpuSelectionCoordinator);
        var runtimeCatalogCoordinator = new RuntimeCatalogCoordinator(
            runtime.RuntimeCatalogFlowController,
            runtime.RuntimeEndpointStatusPresenter);
        var navigationState = new ShellNavigationState();
        var shellChrome = ShellChromeViewModels.Create(navigationState);
        var dialogPresenter = new DialogPresenter(app.DialogService, app.AppLogger);
        var remoteCatalogDialogGate = new OnceDialogGate();
        var userSettingsController = new UserSettingsController(app.UserSettingsStore, app.AppLogger);
        var supportedGamesWikiOptionsLoader = new SupportedGamesWikiOptionsLoader();
        var supportedGamesWikiMarkdownLoader = new SupportedGamesWikiMarkdownLoader(
            supportedGamesWikiOptionsLoader.Load(),
            new HttpClient(),
            new SupportedGamesWikiMarkdownParser(),
            new SupportedGamesWikiMarkdownCacheStore(app.LocalDataPathProvider, app.AppLogger),
            app.AppLogger);
        var startupAnnouncementFlowController = new StartupAnnouncementFlowController(startupNoticePresenter);
        var selectionPopupCoordinator = new SelectionPopupCoordinator(
            install.GameSelectionFlowController,
            dialogPresenter,
            flowLogDispatcher,
            app.AppLogger);
        var appUpdateCoordinator = new AppUpdateCoordinator(update.AppUpdateFlowController);
        var gameCardSelectionStateController = new GameCardSelectionStateController();
        var startupBackgroundTaskManager = new StartupBackgroundTaskManager();
        var archiveReadinessRefreshCoordinator = new ArchiveReadinessRefreshCoordinator();
        var archiveReadinessWarmupController = new ArchiveReadinessWarmupController();
        var startupFlowCoordinator = new StartupFlowCoordinator();
        var coverCacheBootstrapFileSystem = new InstallFileSystem();
        var coverCacheBootstrapExtraBundleInstaller = new ExtraBundleInstaller(
            new ArchiveDownloader(new HttpClient()),
            new ZipArchiveExtractor(),
            coverCacheBootstrapFileSystem,
            app.LocalDataPathProvider.InstallExecutionTempDirectory);
        var coverCacheBootstrapService = new CoverCacheBootstrapService(
            coverCacheBootstrapExtraBundleInstaller,
            coverCacheBootstrapFileSystem,
            app.LocalDataPathProvider,
            app.AppLogger);
        var busyStateApplier = new MainViewModelBusyStateApplier();
        var shellSectionsFactory = new ShellSectionsFactory();
        var viewModelFactory = new MainViewModelFactory();

        return viewModelFactory.Create(new MainViewModelFactoryInput
        {
            AllowDependencyFallbacks = false,
            Required = new MainViewModelRequiredDependencies
            {
                DialogService = app.DialogService,
                RuntimeContextProvider = runtime.RuntimeContextProvider,
                LanguageProvider = app.LanguageProvider,
                MockDataProvider = app.MockDataProvider
            },
            Runtime = new MainViewModelRuntimeDependencies
            {
                OperatingSystemSupportPolicy = new OverrideableOperatingSystemSupportPolicy(
                    new Windows11OnlyOperatingSystemSupportPolicy()),
                DeviceIdentityResolver = runtime.DeviceIdentityResolver,
                DeviceIdentityRulesLoader = runtime.DeviceIdentityRulesLoader,
                ShellGameCardViewModelFactory = runtime.ShellGameCardViewModelFactory,
                RemoteCatalogPipeline = runtime.RemoteCatalogPipeline,
                RuntimeContextFlowController = runtime.RuntimeContextFlowController,
                DeviceIdentityRulesFlowController = runtime.DeviceIdentityRulesFlowController,
                RuntimeCatalogFlowController = runtime.RuntimeCatalogFlowController,
                RuntimeEndpointStatusPresenter = runtime.RuntimeEndpointStatusPresenter,
                GpuSelectionCoordinator = gpuSelectionCoordinator,
                RuntimeContextCoordinator = runtimeContextCoordinator,
                RuntimeCatalogCoordinator = runtimeCatalogCoordinator,
                ModuleDownloadLinkMapBuilder = runtime.ModuleDownloadLinkMapBuilder,
                GpuBundleManifestClient = runtime.GpuBundleManifestClient,
                GpuBundleManifestRuleResolver = runtime.GpuBundleManifestRuleResolver
            },
            Scan = new MainViewModelScanDependencies
            {
                FolderPickerService = scan.FolderPickerService,
                ScanFolderDiscoveryService = scan.ScanFolderDiscoveryService,
                ScanFolderManifestStore = scan.ScanFolderManifestStore,
                ScanPipeline = scan.ScanPipeline,
                ScanFlowController = scan.ScanFlowController,
                ScanFolderListController = scanFolderListController,
                ScanFolderDialogPresenter = scanFolderDialogPresenter,
                ScanFolderActionController = scanFolderActionController,
                ScanResultCoordinatorFactory = scanResultCoordinatorFactory,
                ScanOrchestratorFactory = scanOrchestratorFactory
            },
            Install = new MainViewModelInstallDependencies
            {
                InstallSelectionBridge = install.InstallSelectionBridge,
                GameSelectionFlowController = install.GameSelectionFlowController,
                InstallPlanBuilder = install.InstallPlanBuilder,
                InstallStatusResolver = install.InstallStatusResolver,
                ComponentInstallParityReviewBuilder = install.ComponentInstallParityReviewBuilder,
                ConfigProfileApplier = install.ConfigProfileApplier,
                IniProfileEditor = install.IniProfileEditor,
                InstallStartGateResolver = install.InstallStartGateResolver,
                ComponentInstallCoordinator = install.ComponentInstallCoordinator,
                ArchivePreparationCoordinator = install.ArchivePreparationCoordinator,
                InstallRejectionPresentationResolver = install.InstallRejectionPresentationResolver,
                InstallResultPresentationResolver = install.InstallResultPresentationResolver,
                InstallSelectionRequestBuilder = install.InstallSelectionRequestBuilder,
                InstallPlanInputBuilder = install.InstallPlanInputBuilder,
                ComponentInstallContextBuilder = install.ComponentInstallContextBuilder,
                InstallPopupPresenter = install.InstallPopupPresenter,
                InstallCompletionMessageBuilder = install.InstallCompletionMessageBuilder,
                ArchiveReadinessFlowController = install.ArchiveReadinessFlowController,
                ConfigApplyFlowController = install.ConfigApplyFlowController,
                InstallResultApplier = install.InstallResultApplier,
                InstallFlowController = install.InstallFlowController,
                OptiClickUninstallPlanBuilder = install.OptiClickUninstallPlanBuilder,
                OptiClickUninstallExecutor = install.OptiClickUninstallExecutor
            },
            App = new MainViewModelAppDependencies
            {
                AppVersionProvider = update.AppVersionProvider,
                AppUpdateVersionComparer = update.AppUpdateVersionComparer,
                AppUpdateService = update.AppUpdateService,
                AppUpdateExecutionService = update.AppUpdateExecutionService,
                AppUpdateDialogPresenter = update.AppUpdateDialogPresenter,
                AppUpdateFlowController = update.AppUpdateFlowController,
                AppUpdateCoordinator = appUpdateCoordinator,
                GameDetailsDialogPresenter = support.GameDetailsDialogPresenter,
                AppLogger = app.AppLogger,
                LocalDataPathProvider = app.LocalDataPathProvider,
                AppStringsProvider = app.StringsProvider,
                UserSettingsStore = app.UserSettingsStore,
                FirstRunStateStore = app.FirstRunStateStore,
                GpuVendorLogoResolver = gpuVendorLogoResolver,
                DialogHost = app.DialogHost,
                InstallManagementDialogHost = app.InstallManagementDialogHost,
                InstallManagementDialogService = app.InstallManagementDialogService,
                ContactIssueLinkBuilder = support.ContactIssueLinkBuilder,
                ExternalUrlLauncher = app.ExternalUrlLauncher,
                SupportActionController = support.SupportActionController,
                SupportIssueContextBuilder = support.SupportIssueContextBuilder,
                NavigationState = navigationState,
                ShellChrome = shellChrome,
                DialogPresenter = dialogPresenter,
                RemoteCatalogDialogGate = remoteCatalogDialogGate,
                UserSettingsController = userSettingsController,
                SupportedGamesWikiMarkdownLoader = supportedGamesWikiMarkdownLoader,
                RuntimeHeaderPresenter = runtimeHeaderPresenter,
                StartupNoticePresenter = startupNoticePresenter,
                StartupAnnouncementFlowController = startupAnnouncementFlowController,
                SelectionPopupCoordinator = selectionPopupCoordinator,
                ShellCommandActionController = shellCommandActionController,
                LocalizationStateController = localizationStateController,
                RuntimeSummaryStateController = runtimeSummaryStateController,
                BusyStateApplier = busyStateApplier,
                FlowLogDispatcher = flowLogDispatcher,
                FlowRequestFactory = flowRequestFactory,
                ResultApplier = resultApplier,
                ShellSectionsFactory = shellSectionsFactory,
                GameCardSelectionStateController = gameCardSelectionStateController,
                GameMasterCoverPrefetchService = gameMasterCoverPrefetchService,
                CoverCacheBootstrapService = coverCacheBootstrapService,
                StartupBackgroundTaskManager = startupBackgroundTaskManager,
                ArchiveReadinessRefreshCoordinator = archiveReadinessRefreshCoordinator,
                ArchiveReadinessWarmupController = archiveReadinessWarmupController,
                StartupFlowCoordinator = startupFlowCoordinator
            },
            SeedMockGameCards = seedMockGameCards,
            SeedMockScanFolders = seedMockScanFolders
        });
    }
}
