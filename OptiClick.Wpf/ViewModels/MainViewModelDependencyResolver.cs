using OptiClick.Wpf.Install.Archives;
using OptiClick.Wpf.Install.Config;
using OptiClick.Wpf.Install.Execution;
using OptiClick.Wpf.Install.FileSystem;
using OptiClick.Wpf.Install.Flow;
using OptiClick.Wpf.Install.Planning;
using OptiClick.Wpf.Install.Precheck;
using OptiClick.Wpf.Install.Presentation;
using OptiClick.Wpf.Install.Uninstall;
using OptiClick.Wpf.Localization;
using OptiClick.Wpf.Logging;
using OptiClick.Wpf.Shell.Runtime.DeviceIdentity;
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
using OptiClick.Infrastructure.FileSystem;
using System.Net.Http;

namespace OptiClick.Wpf.ViewModels;

public static class MainViewModelDependencyResolver
{
    public static MainViewModelResolvedDependencies Resolve(
        MainViewModelRequiredDependencies requiredDependencies,
        MainViewModelRuntimeDependencies? runtime = null,
        MainViewModelScanDependencies? scan = null,
        MainViewModelInstallDependencies? install = null,
        MainViewModelAppDependencies? app = null,
        bool allowFallbackResolution = true)
    {
        // Resolver is a fallback safety layer for MainViewModel construction.
        // Production wiring should still be created in Composition classes.
        var required = ValidateRequired(requiredDependencies);
        var runtimeDependencies = runtime ?? new MainViewModelRuntimeDependencies();
        var scanDependencies = scan ?? new MainViewModelScanDependencies();
        var installDependencies = install ?? new MainViewModelInstallDependencies();
        var appDependencies = app ?? new MainViewModelAppDependencies();
        ValidateFallbackPolicy(
            runtimeDependencies,
            scanDependencies,
            installDependencies,
            appDependencies,
            allowFallbackResolution);

        var runtimeContextProvider = required.RuntimeContextProvider;
        var languageProvider = required.LanguageProvider;
        var mockDataProvider = required.MockDataProvider;
        var appLogger = appDependencies.AppLogger ?? NullAppLogger.Instance;

        var operatingSystemSupportPolicy = runtimeDependencies.OperatingSystemSupportPolicy
                                           ?? new OverrideableOperatingSystemSupportPolicy(
                                               new Windows11OnlyOperatingSystemSupportPolicy());
        var deviceIdentityResolver = runtimeDependencies.DeviceIdentityResolver ?? new PassthroughDeviceIdentityResolver();
        var resolvedModuleDownloadLinkMapBuilder = runtimeDependencies.ModuleDownloadLinkMapBuilder ?? new ModuleDownloadLinkMapBuilder();
        var runtimeContextFlowController = runtimeDependencies.RuntimeContextFlowController ?? new RuntimeContextFlowController(runtimeContextProvider);
        var deviceIdentityRulesFlowController = runtimeDependencies.DeviceIdentityRulesFlowController ?? new DeviceIdentityRulesFlowController(runtimeDependencies.DeviceIdentityRulesLoader);
        var runtimeCatalogFlowController = runtimeDependencies.RuntimeCatalogFlowController ?? new RuntimeCatalogFlowController(
            runtimeDependencies.RemoteCatalogPipeline,
            resolvedModuleDownloadLinkMapBuilder,
            new RuntimeCatalogDialogPresenter());
        var runtimeEndpointStatusPresenter = runtimeDependencies.RuntimeEndpointStatusPresenter ?? new RuntimeEndpointStatusPresenter();
        var fallbackGpuManifestRequestUriBuilder = new GpuBundleManifestRequestUriBuilder();
        var gpuBundleManifestClient = runtimeDependencies.GpuBundleManifestClient
                                      ?? new CachedRemoteGpuBundleManifestClient(
                                          new RemoteGpuBundleManifestClient(
                                              new HttpClient(),
                                              fallbackGpuManifestRequestUriBuilder,
                                              new RemoteGpuBundleManifestParser(),
                                              appLogger),
                                          fallbackGpuManifestRequestUriBuilder);
        var gpuBundleManifestRuleResolver = runtimeDependencies.GpuBundleManifestRuleResolver
                                            ?? new GpuBundleManifestRuleResolver();

        var resolvedInstallSelectionRequestBuilder = installDependencies.InstallSelectionRequestBuilder ?? new InstallSelectionRequestBuilder(installDependencies.InstallStatusResolver);
        var gameSelectionFlowController = installDependencies.GameSelectionFlowController
                                          ?? new GameSelectionFlowController(installDependencies.InstallSelectionBridge, resolvedInstallSelectionRequestBuilder);
        var scanPipeline = scanDependencies.ScanPipeline;
        var scanFlowController = scanDependencies.ScanFlowController ?? new ScanFlowController(scanPipeline, runtimeDependencies.ShellGameCardViewModelFactory);

        var localDataPathProvider = appDependencies.LocalDataPathProvider ?? new AppLocalDataPathProvider();
        var appStringsProvider = appDependencies.AppStringsProvider ?? new AppStringsProvider();
        var flowLogDispatcher = appDependencies.FlowLogDispatcher ?? new FlowLogDispatcher(appLogger);
        var flowRequestFactory = appDependencies.FlowRequestFactory ?? new MainViewModelFlowRequestFactory();

        var resolvedUserSettingsStore = appDependencies.UserSettingsStore ?? new AppUserSettingsStore(localDataPathProvider, appLogger);
        var firstRunStateStore = appDependencies.FirstRunStateStore ?? new FirstRunStateStore(localDataPathProvider, appLogger);
        ShellNavigationState navigationState;
        ShellChromeViewModels shellChrome;
        if (appDependencies.ShellChrome is null)
        {
            navigationState = appDependencies.NavigationState ?? new ShellNavigationState();
            shellChrome = ShellChromeViewModels.Create(navigationState);
        }
        else
        {
            shellChrome = appDependencies.ShellChrome;
            navigationState = appDependencies.NavigationState ?? shellChrome.NavigationState;
            EnsureShellChromeNavigationState(shellChrome, navigationState);
        }

        var dialogPresenter = appDependencies.DialogPresenter ?? new DialogPresenter(required.DialogService, appLogger);
        var remoteCatalogDialogGate = appDependencies.RemoteCatalogDialogGate ?? new OnceDialogGate();
        var resolvedGpuVendorLogoResolver = appDependencies.GpuVendorLogoResolver ?? new GpuVendorLogoResolver();
        var runtimeHeaderPresenter = appDependencies.RuntimeHeaderPresenter ?? new RuntimeHeaderPresenter(resolvedGpuVendorLogoResolver);
        var runtimeSummaryStateController = appDependencies.RuntimeSummaryStateController
                                            ?? new RuntimeSummaryStateController(deviceIdentityResolver, runtimeHeaderPresenter);
        var gpuSelectionCoordinator = runtimeDependencies.GpuSelectionCoordinator
                                      ?? new GpuSelectionCoordinator(GpuSelectionCoordinator.DefaultMaxSupportedGpuCount);
        var runtimeContextCoordinator = runtimeDependencies.RuntimeContextCoordinator
                                        ?? new RuntimeContextCoordinator(
                                            runtimeContextFlowController,
                                            runtimeSummaryStateController,
                                            flowLogDispatcher,
                                            gpuSelectionCoordinator);
        var runtimeCatalogCoordinator = runtimeDependencies.RuntimeCatalogCoordinator
                                        ?? new RuntimeCatalogCoordinator(
                                            runtimeCatalogFlowController,
                                            runtimeEndpointStatusPresenter);
        var userSettingsController = appDependencies.UserSettingsController ?? new UserSettingsController(resolvedUserSettingsStore, appLogger);
        var supportedGamesWikiMarkdownLoader = appDependencies.SupportedGamesWikiMarkdownLoader
                                                ?? new NoopSupportedGamesWikiMarkdownLoader();
        var scanFolderListController = scanDependencies.ScanFolderListController ?? new ScanFolderListController(scanDependencies.ScanFolderManifestStore);
        var scanVisibleGameResolver = new ScanVisibleGameResolver();
        var installPopupPresenter = installDependencies.InstallPopupPresenter ?? new InstallPopupPresenter();
        var startupNoticePresenter = appDependencies.StartupNoticePresenter ?? new StartupNoticePresenter();
        var startupAnnouncementFlowController = appDependencies.StartupAnnouncementFlowController
                                                ?? new StartupAnnouncementFlowController(startupNoticePresenter);
        var scanFolderDialogPresenter = scanDependencies.ScanFolderDialogPresenter ?? new ScanFolderDialogPresenter();
        var scanFolderActionController = scanDependencies.ScanFolderActionController
                                         ?? new ScanFolderActionController(
                                             scanFolderListController,
                                             scanDependencies.FolderPickerService,
                                             scanFolderDialogPresenter);
        var scanResultCoordinatorFactory = scanDependencies.ScanResultCoordinatorFactory ?? new ScanResultCoordinatorFactory();
        var scanOrchestratorFactory = scanDependencies.ScanOrchestratorFactory ?? new ScanOrchestratorFactory();
        var supportIssueContextBuilder = appDependencies.SupportIssueContextBuilder ?? new SupportIssueContextBuilder();

        var resolvedInstallPlanInputBuilder = installDependencies.InstallPlanInputBuilder ?? new InstallPlanInputBuilder();
        var resolvedComponentInstallContextBuilder = installDependencies.ComponentInstallContextBuilder ?? new ComponentInstallContextBuilder();
        var resolvedInstallCompletionMessageBuilder = installDependencies.InstallCompletionMessageBuilder ?? new InstallCompletionMessageBuilder();
        var archiveReadinessFlowController = installDependencies.ArchiveReadinessFlowController
                                             ?? new ArchiveReadinessFlowController(installDependencies.ArchivePreparationCoordinator);
        var resolvedOptiScalerIniBaseApplier = installDependencies.IniProfileEditor is null
            ? null
            : new OptiScalerIniBaseApplier(installDependencies.IniProfileEditor);
        var resolvedConfigApplyFlowController = installDependencies.ConfigApplyFlowController
                                                ?? new ConfigApplyFlowController(installDependencies.ConfigProfileApplier, resolvedOptiScalerIniBaseApplier);
        var resolvedInstallResultApplier = installDependencies.InstallResultApplier
                                           ?? new InstallResultApplier(
                                               resolvedConfigApplyFlowController,
                                               new RtssProfileApplier(),
                                               installDependencies.InstallResultPresentationResolver,
                                               resolvedInstallCompletionMessageBuilder);
        var installFlowController = installDependencies.InstallFlowController
                                    ?? new InstallFlowController(
                                        installDependencies.InstallPlanBuilder,
                                        installDependencies.InstallStartGateResolver,
                                        installDependencies.ComponentInstallCoordinator,
                                        installDependencies.ComponentInstallParityReviewBuilder,
                                        resolvedInstallPlanInputBuilder,
                                        resolvedComponentInstallContextBuilder,
                                        installPopupPresenter,
                                        resolvedInstallResultApplier,
                                        installDependencies.InstallRejectionPresentationResolver);
        var resolvedUninstallFileSystem = new OptiClick.Wpf.Install.FileSystem.InstallFileSystem();
        var resolvedUninstallSignatures = new OptiClick.Wpf.Install.Execution.FileSignatureDetectors(resolvedUninstallFileSystem);
        var resolvedUninstallVersionInfoReader = new WindowsFileVersionInfoReader();
        var optiClickUninstallPlanBuilder = installDependencies.OptiClickUninstallPlanBuilder
                                            ?? new OptiClickUninstallPlanBuilder(
                                                resolvedUninstallFileSystem,
                                                resolvedUninstallSignatures,
                                                resolvedUninstallVersionInfoReader,
                                                appLogger);
        var optiClickUninstallExecutor = installDependencies.OptiClickUninstallExecutor
                                         ?? new OptiClickUninstallExecutor(
                                             resolvedUninstallFileSystem,
                                             resolvedUninstallSignatures,
                                             resolvedUninstallVersionInfoReader,
                                             appLogger);

        var appVersionProvider = appDependencies.AppVersionProvider ?? new AssemblyAppVersionProvider();
        var resolvedAppUpdateVersionComparer = appDependencies.AppUpdateVersionComparer ?? new AppUpdateVersionComparer();
        var resolvedAppUpdateService = appDependencies.AppUpdateService ?? new AppUpdateService(resolvedAppUpdateVersionComparer);
        var resolvedAppUpdateExecutionService = appDependencies.AppUpdateExecutionService
                                                ?? new AppUpdateExecutionService(
                                                    localDataPathProvider: localDataPathProvider,
                                                    logger: appLogger);
        var contactIssueLinkBuilder = appDependencies.ContactIssueLinkBuilder ?? new ContactIssueLinkBuilder();
        var resolvedExternalUrlLauncher = appDependencies.ExternalUrlLauncher ?? new ExternalUrlLauncher(appLogger);
        var resolvedAppUpdateDialogPresenter = appDependencies.AppUpdateDialogPresenter ?? new AppUpdateDialogPresenter();
        var supportActionController = appDependencies.SupportActionController ?? new SupportActionController(
            contactIssueLinkBuilder,
            resolvedExternalUrlLauncher);
        var resolvedGameMasterCoverPrefetchService = appDependencies.GameMasterCoverPrefetchService
                                                   ?? new GameMasterCoverPrefetchService();
        var resolvedCoverCacheBootstrapService = appDependencies.CoverCacheBootstrapService
                                               ?? NoOpCoverCacheBootstrapService.Instance;
        var shellCommandActionController = appDependencies.ShellCommandActionController
                                           ?? new ShellCommandActionController(
                                               startupNoticePresenter,
                                               supportIssueContextBuilder,
                                               supportActionController);
        var localizationStateController = appDependencies.LocalizationStateController ?? new LocalizationStateController();
        var busyStateApplier = appDependencies.BusyStateApplier ?? new MainViewModelBusyStateApplier();
        var appUpdateFlowController = appDependencies.AppUpdateFlowController ?? new AppUpdateFlowController(
            resolvedAppUpdateService,
            resolvedAppUpdateExecutionService,
            resolvedExternalUrlLauncher,
            resolvedAppUpdateDialogPresenter);
        var appUpdateCoordinator = appDependencies.AppUpdateCoordinator ?? new AppUpdateCoordinator(appUpdateFlowController);
        var selectionPopupCoordinator = appDependencies.SelectionPopupCoordinator
                                        ?? new SelectionPopupCoordinator(
                                            gameSelectionFlowController,
                                            dialogPresenter,
                                            flowLogDispatcher,
                                            appLogger);
        var gameDetailsDialogPresenter = appDependencies.GameDetailsDialogPresenter ?? new GameDetailsDialogPresenter();
        var dialogHost = appDependencies.DialogHost ?? new DialogHostViewModel();
        var installManagementDialogHost = appDependencies.InstallManagementDialogHost ?? new InstallManagementDialogHostViewModel();
        var installManagementDialogService = appDependencies.InstallManagementDialogService
                                             ?? new OverlayInstallManagementDialogService(installManagementDialogHost);
        var resultApplier = appDependencies.ResultApplier ?? new MainViewModelResultApplier();
        var shellSectionsFactory = appDependencies.ShellSectionsFactory ?? new ShellSectionsFactory();
        var shellSectionsCompositionFactory = appDependencies.ShellSectionsCompositionFactory ?? new ShellSectionsCompositionFactory();
        var gameCardSelectionStateController = appDependencies.GameCardSelectionStateController ?? new GameCardSelectionStateController();
        var startupBackgroundTaskManager = appDependencies.StartupBackgroundTaskManager ?? new StartupBackgroundTaskManager();
        var gameMasterCoverPrefetchCoordinator = new GameMasterCoverPrefetchCoordinator(
            resolvedGameMasterCoverPrefetchService,
            startupBackgroundTaskManager);
        var archiveReadinessRefreshCoordinator = appDependencies.ArchiveReadinessRefreshCoordinator ?? new ArchiveReadinessRefreshCoordinator();
        var archiveReadinessWarmupController = appDependencies.ArchiveReadinessWarmupController ?? new ArchiveReadinessWarmupController();
        var startupFlowCoordinator = appDependencies.StartupFlowCoordinator ?? new StartupFlowCoordinator();

        return new MainViewModelResolvedDependencies
        {
            LanguageProvider = languageProvider,
            MockDataProvider = mockDataProvider,
            OperatingSystemSupportPolicy = operatingSystemSupportPolicy,
            ShellGameCardViewModelFactory = runtimeDependencies.ShellGameCardViewModelFactory,
            RuntimeContextFlowController = runtimeContextFlowController,
            DeviceIdentityRulesFlowController = deviceIdentityRulesFlowController,
            RuntimeCatalogFlowController = runtimeCatalogFlowController,
            RuntimeEndpointStatusPresenter = runtimeEndpointStatusPresenter,
            GpuSelectionCoordinator = gpuSelectionCoordinator,
            RuntimeContextCoordinator = runtimeContextCoordinator,
            RuntimeCatalogCoordinator = runtimeCatalogCoordinator,
            GpuBundleManifestClient = gpuBundleManifestClient,
            GpuBundleManifestRuleResolver = gpuBundleManifestRuleResolver,
            FolderPickerService = scanDependencies.FolderPickerService,
            ScanFolderDiscoveryService = scanDependencies.ScanFolderDiscoveryService,
            ScanFlowController = scanFlowController,
            GameSelectionFlowController = gameSelectionFlowController,
            ArchiveReadinessFlowController = archiveReadinessFlowController,
            InstallFlowController = installFlowController,
            OptiClickUninstallPlanBuilder = optiClickUninstallPlanBuilder,
            OptiClickUninstallExecutor = optiClickUninstallExecutor,
            AppVersionProvider = appVersionProvider,
            AppUpdateFlowController = appUpdateFlowController,
            AppUpdateCoordinator = appUpdateCoordinator,
            GameDetailsDialogPresenter = gameDetailsDialogPresenter,
            AppLogger = appLogger,
            LocalDataPathProvider = localDataPathProvider,
            AppStringsProvider = appStringsProvider,
            FirstRunStateStore = firstRunStateStore,
            NavigationState = navigationState,
            ShellChrome = shellChrome,
            DialogPresenter = dialogPresenter,
            InstallManagementDialogHost = installManagementDialogHost,
            InstallManagementDialogService = installManagementDialogService,
            RemoteCatalogDialogGate = remoteCatalogDialogGate,
            RuntimeHeaderPresenter = runtimeHeaderPresenter,
            UserSettingsController = userSettingsController,
            SupportedGamesWikiMarkdownLoader = supportedGamesWikiMarkdownLoader,
            ScanFolderListController = scanFolderListController,
            ScanVisibleGameResolver = scanVisibleGameResolver,
            StartupNoticePresenter = startupNoticePresenter,
            StartupAnnouncementFlowController = startupAnnouncementFlowController,
            SelectionPopupCoordinator = selectionPopupCoordinator,
            ShellCommandActionController = shellCommandActionController,
            LocalizationStateController = localizationStateController,
            RuntimeSummaryStateController = runtimeSummaryStateController,
            BusyStateApplier = busyStateApplier,
            ScanFolderDialogPresenter = scanFolderDialogPresenter,
            ScanFolderActionController = scanFolderActionController,
            ScanResultCoordinatorFactory = scanResultCoordinatorFactory,
            ScanOrchestratorFactory = scanOrchestratorFactory,
            SupportActionController = supportActionController,
            SupportIssueContextBuilder = supportIssueContextBuilder,
            InstallPopupPresenter = installPopupPresenter,
            FlowLogDispatcher = flowLogDispatcher,
            FlowRequestFactory = flowRequestFactory,
            DialogHost = dialogHost,
            ResultApplier = resultApplier,
            ShellSectionsFactory = shellSectionsFactory,
            ShellSectionsCompositionFactory = shellSectionsCompositionFactory,
            GameCardSelectionStateController = gameCardSelectionStateController,
            GameMasterCoverPrefetchCoordinator = gameMasterCoverPrefetchCoordinator,
            CoverCacheBootstrapService = resolvedCoverCacheBootstrapService,
            StartupBackgroundTaskManager = startupBackgroundTaskManager,
            ArchiveReadinessRefreshCoordinator = archiveReadinessRefreshCoordinator,
            ArchiveReadinessWarmupController = archiveReadinessWarmupController,
            StartupFlowCoordinator = startupFlowCoordinator
        };
    }

    private static MainViewModelRequiredDependencies ValidateRequired(MainViewModelRequiredDependencies? required)
    {
        ArgumentNullException.ThrowIfNull(required);
        ArgumentNullException.ThrowIfNull(required.DialogService);
        ArgumentNullException.ThrowIfNull(required.RuntimeContextProvider);
        ArgumentNullException.ThrowIfNull(required.LanguageProvider);
        ArgumentNullException.ThrowIfNull(required.MockDataProvider);
        return required;
    }

    private static void ValidateFallbackPolicy(
        MainViewModelRuntimeDependencies runtimeDependencies,
        MainViewModelScanDependencies scanDependencies,
        MainViewModelInstallDependencies installDependencies,
        MainViewModelAppDependencies appDependencies,
        bool allowFallbackResolution)
    {
        if (allowFallbackResolution)
        {
            return;
        }

        EnsureExplicitRuntimeDependencies(runtimeDependencies);
        EnsureExplicitScanDependencies(scanDependencies);
        EnsureExplicitInstallDependencies(installDependencies);
        EnsureExplicitAppDependencies(appDependencies);
    }

    private static void EnsureExplicitRuntimeDependencies(MainViewModelRuntimeDependencies runtimeDependencies)
    {
        EnsureExplicitDependency(runtimeDependencies.RuntimeContextFlowController, $"{nameof(MainViewModelRuntimeDependencies)}.{nameof(MainViewModelRuntimeDependencies.RuntimeContextFlowController)}");
        EnsureExplicitDependency(runtimeDependencies.DeviceIdentityRulesFlowController, $"{nameof(MainViewModelRuntimeDependencies)}.{nameof(MainViewModelRuntimeDependencies.DeviceIdentityRulesFlowController)}");
        EnsureExplicitDependency(runtimeDependencies.RuntimeCatalogFlowController, $"{nameof(MainViewModelRuntimeDependencies)}.{nameof(MainViewModelRuntimeDependencies.RuntimeCatalogFlowController)}");
        EnsureExplicitDependency(runtimeDependencies.RuntimeEndpointStatusPresenter, $"{nameof(MainViewModelRuntimeDependencies)}.{nameof(MainViewModelRuntimeDependencies.RuntimeEndpointStatusPresenter)}");
        EnsureExplicitDependency(runtimeDependencies.GpuSelectionCoordinator, $"{nameof(MainViewModelRuntimeDependencies)}.{nameof(MainViewModelRuntimeDependencies.GpuSelectionCoordinator)}");
        EnsureExplicitDependency(runtimeDependencies.RuntimeContextCoordinator, $"{nameof(MainViewModelRuntimeDependencies)}.{nameof(MainViewModelRuntimeDependencies.RuntimeContextCoordinator)}");
        EnsureExplicitDependency(runtimeDependencies.RuntimeCatalogCoordinator, $"{nameof(MainViewModelRuntimeDependencies)}.{nameof(MainViewModelRuntimeDependencies.RuntimeCatalogCoordinator)}");
        EnsureExplicitDependency(runtimeDependencies.GpuBundleManifestClient, $"{nameof(MainViewModelRuntimeDependencies)}.{nameof(MainViewModelRuntimeDependencies.GpuBundleManifestClient)}");
        EnsureExplicitDependency(runtimeDependencies.GpuBundleManifestRuleResolver, $"{nameof(MainViewModelRuntimeDependencies)}.{nameof(MainViewModelRuntimeDependencies.GpuBundleManifestRuleResolver)}");
    }

    private static void EnsureExplicitScanDependencies(MainViewModelScanDependencies scanDependencies)
    {
        EnsureExplicitDependency(scanDependencies.ScanFlowController, $"{nameof(MainViewModelScanDependencies)}.{nameof(MainViewModelScanDependencies.ScanFlowController)}");
        EnsureExplicitDependency(scanDependencies.ScanFolderListController, $"{nameof(MainViewModelScanDependencies)}.{nameof(MainViewModelScanDependencies.ScanFolderListController)}");
        EnsureExplicitDependency(scanDependencies.ScanFolderActionController, $"{nameof(MainViewModelScanDependencies)}.{nameof(MainViewModelScanDependencies.ScanFolderActionController)}");
        EnsureExplicitDependency(scanDependencies.ScanResultCoordinatorFactory, $"{nameof(MainViewModelScanDependencies)}.{nameof(MainViewModelScanDependencies.ScanResultCoordinatorFactory)}");
        EnsureExplicitDependency(scanDependencies.ScanOrchestratorFactory, $"{nameof(MainViewModelScanDependencies)}.{nameof(MainViewModelScanDependencies.ScanOrchestratorFactory)}");
    }

    private static void EnsureExplicitInstallDependencies(MainViewModelInstallDependencies installDependencies)
    {
        EnsureExplicitDependency(installDependencies.ArchiveReadinessFlowController, $"{nameof(MainViewModelInstallDependencies)}.{nameof(MainViewModelInstallDependencies.ArchiveReadinessFlowController)}");
        EnsureExplicitDependency(installDependencies.InstallFlowController, $"{nameof(MainViewModelInstallDependencies)}.{nameof(MainViewModelInstallDependencies.InstallFlowController)}");
        EnsureExplicitDependency(installDependencies.InstallPopupPresenter, $"{nameof(MainViewModelInstallDependencies)}.{nameof(MainViewModelInstallDependencies.InstallPopupPresenter)}");
        EnsureExplicitDependency(installDependencies.OptiClickUninstallPlanBuilder, $"{nameof(MainViewModelInstallDependencies)}.{nameof(MainViewModelInstallDependencies.OptiClickUninstallPlanBuilder)}");
        EnsureExplicitDependency(installDependencies.OptiClickUninstallExecutor, $"{nameof(MainViewModelInstallDependencies)}.{nameof(MainViewModelInstallDependencies.OptiClickUninstallExecutor)}");
    }

    private static void EnsureExplicitAppDependencies(MainViewModelAppDependencies appDependencies)
    {
        EnsureExplicitDependency(appDependencies.NavigationState, nameof(MainViewModelAppDependencies.NavigationState));
        EnsureExplicitDependency(appDependencies.DialogPresenter, nameof(MainViewModelAppDependencies.DialogPresenter));
        EnsureExplicitDependency(appDependencies.RemoteCatalogDialogGate, nameof(MainViewModelAppDependencies.RemoteCatalogDialogGate));
        EnsureExplicitDependency(appDependencies.UserSettingsController, nameof(MainViewModelAppDependencies.UserSettingsController));
        EnsureExplicitDependency(appDependencies.StartupAnnouncementFlowController, nameof(MainViewModelAppDependencies.StartupAnnouncementFlowController));
        EnsureExplicitDependency(appDependencies.GameCardSelectionStateController, nameof(MainViewModelAppDependencies.GameCardSelectionStateController));
        EnsureExplicitDependency(appDependencies.InstallManagementDialogHost, nameof(MainViewModelAppDependencies.InstallManagementDialogHost));
        EnsureExplicitDependency(appDependencies.InstallManagementDialogService, nameof(MainViewModelAppDependencies.InstallManagementDialogService));
        EnsureExplicitDependency(appDependencies.ShellChrome, nameof(MainViewModelAppDependencies.ShellChrome));
        EnsureExplicitDependency(appDependencies.AppVersionProvider, nameof(MainViewModelAppDependencies.AppVersionProvider));
        EnsureExplicitDependency(appDependencies.AppUpdateFlowController, nameof(MainViewModelAppDependencies.AppUpdateFlowController));
        EnsureExplicitDependency(appDependencies.AppUpdateCoordinator, nameof(MainViewModelAppDependencies.AppUpdateCoordinator));
        EnsureExplicitDependency(appDependencies.GameDetailsDialogPresenter, nameof(MainViewModelAppDependencies.GameDetailsDialogPresenter));
        EnsureExplicitDependency(appDependencies.AppLogger, nameof(MainViewModelAppDependencies.AppLogger));
        EnsureExplicitDependency(appDependencies.LocalDataPathProvider, nameof(MainViewModelAppDependencies.LocalDataPathProvider));
        EnsureExplicitDependency(appDependencies.AppStringsProvider, nameof(MainViewModelAppDependencies.AppStringsProvider));
        EnsureExplicitDependency(appDependencies.FirstRunStateStore, nameof(MainViewModelAppDependencies.FirstRunStateStore));
        EnsureExplicitDependency(appDependencies.ShellCommandActionController, nameof(MainViewModelAppDependencies.ShellCommandActionController));
        EnsureExplicitDependency(appDependencies.SelectionPopupCoordinator, nameof(MainViewModelAppDependencies.SelectionPopupCoordinator));
        EnsureExplicitDependency(appDependencies.LocalizationStateController, nameof(MainViewModelAppDependencies.LocalizationStateController));
        EnsureExplicitDependency(appDependencies.RuntimeSummaryStateController, nameof(MainViewModelAppDependencies.RuntimeSummaryStateController));
        EnsureExplicitDependency(appDependencies.BusyStateApplier, nameof(MainViewModelAppDependencies.BusyStateApplier));
        EnsureExplicitDependency(appDependencies.FlowLogDispatcher, nameof(MainViewModelAppDependencies.FlowLogDispatcher));
        EnsureExplicitDependency(appDependencies.FlowRequestFactory, nameof(MainViewModelAppDependencies.FlowRequestFactory));
        EnsureExplicitDependency(appDependencies.ResultApplier, nameof(MainViewModelAppDependencies.ResultApplier));
        EnsureExplicitDependency(appDependencies.ShellSectionsFactory, nameof(MainViewModelAppDependencies.ShellSectionsFactory));
        EnsureExplicitDependency(appDependencies.ShellSectionsCompositionFactory, nameof(MainViewModelAppDependencies.ShellSectionsCompositionFactory));
        EnsureExplicitDependency(appDependencies.DialogHost, nameof(MainViewModelAppDependencies.DialogHost));
        EnsureExplicitDependency(
            appDependencies.GameMasterCoverPrefetchService,
            nameof(MainViewModelAppDependencies.GameMasterCoverPrefetchService));
        EnsureExplicitDependency(
            appDependencies.CoverCacheBootstrapService,
            nameof(MainViewModelAppDependencies.CoverCacheBootstrapService));
        EnsureExplicitDependency(
            appDependencies.StartupBackgroundTaskManager,
            nameof(MainViewModelAppDependencies.StartupBackgroundTaskManager));
        EnsureExplicitDependency(
            appDependencies.ArchiveReadinessRefreshCoordinator,
            nameof(MainViewModelAppDependencies.ArchiveReadinessRefreshCoordinator));
        EnsureExplicitDependency(
            appDependencies.ArchiveReadinessWarmupController,
            nameof(MainViewModelAppDependencies.ArchiveReadinessWarmupController));
        EnsureExplicitDependency(
            appDependencies.StartupFlowCoordinator,
            nameof(MainViewModelAppDependencies.StartupFlowCoordinator));
    }

    private static void EnsureExplicitDependency(object? dependency, string dependencyName)
    {
        if (dependency is not null)
        {
            return;
        }

        throw new InvalidOperationException(
            $"MainViewModel dependency '{dependencyName}' must be explicitly provided when fallback resolution is disabled.");
    }

    private static void EnsureShellChromeNavigationState(
        ShellChromeViewModels shellChrome,
        ShellNavigationState navigationState)
    {
        if (ReferenceEquals(shellChrome.NavigationState, navigationState))
        {
            return;
        }

        throw new InvalidOperationException(
            "MainViewModel dependency 'ShellChrome.NavigationState' must reference the same instance as 'NavigationState'.");
    }
}
