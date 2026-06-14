using OptiClick.Wpf.Install.Config;
using OptiClick.Wpf.Install.Flow;
using OptiClick.Wpf.Install.Uninstall;
using OptiClick.Wpf.Logging;
using OptiClick.Wpf.ViewModels.Sections;
using OptiClick.Wpf.ViewModels.Sections.Scan;
using OptiClick.Wpf.ViewModels;
using OptiClick.Wpf.Shell.Navigation;
using OptiClick.Wpf.Shell.RuntimeData;
using OptiClick.Wpf.Shell.Games;
using OptiClick.Wpf.Shell.Runtime;
using OptiClick.Wpf.Shell.Gpu;
using OptiClick.Wpf.Shell.Startup;
using OptiClick.Wpf.Shell.Update;
using OptiClick.Wpf.Shell.Scan;
using OptiClick.Wpf.Shell.Selection;
using OptiClick.Wpf.Shell.Support;
using OptiClick.Wpf.Shell.Dialogs;
using OptiClick.Wpf.Shell.Actions;
using OptiClick.Wpf.Shell.Wiki;
using OptiClick.Wpf.Shell.Settings;
using OptiClick.Wpf.Shell.Flow;
using OptiClick.Wpf.Localization;
using OptiClick.Wpf.Shell.Localization;
using OptiClick.Core.Abstractions;

namespace OptiClick.Wpf.ViewModels.DependencyComposition;

internal static class MainViewModelFallbackResolvedDependencyComposer
{
    public static MainViewModelCompositionDependencies Compose(
        MainViewModelRequiredDependencies requiredDependencies,
        RuntimeDependencyComposition runtimeComposition,
        ScanDependencyComposition scanComposition,
        InstallDependencyComposition installComposition,
        AppDependencyComposition appComposition,
        IAppLogger appLogger)
    {
        var app = new MainAppResolvedDependencies
        {
            LanguageProvider = requiredDependencies.LanguageProvider,
            MockDataProvider = requiredDependencies.MockDataProvider,
            AppVersionProvider = appComposition.AppVersionProvider,
            AppLogger = appLogger,
            LocalDataPathProvider = appComposition.LocalDataPathProvider,
            AppStringsProvider = appComposition.AppStringsProvider,
            FirstRunStateStore = appComposition.FirstRunStateStore
        };
        var runtime = new MainRuntimeResolvedDependencies
        {
            OperatingSystemSupportPolicy = runtimeComposition.OperatingSystemSupportPolicy,
            ShellGameCardViewModelFactory = runtimeComposition.ShellGameCardViewModelFactory,
            RuntimeContextFlowController = runtimeComposition.RuntimeContextFlowController,
            DeviceIdentityRulesFlowController = runtimeComposition.DeviceIdentityRulesFlowController,
            RuntimeCatalogFlowController = runtimeComposition.RuntimeCatalogFlowController,
            RuntimeEndpointStatusPresenter = runtimeComposition.RuntimeEndpointStatusPresenter,
            RuntimeCatalogUiFlowController = runtimeComposition.RuntimeCatalogUiFlowController,
            GpuSelectionCoordinator = runtimeComposition.GpuSelectionCoordinator,
            RuntimeContextCoordinator = appComposition.RuntimeServices.RuntimeContextCoordinator,
            RuntimeCatalogCoordinator = runtimeComposition.RuntimeCatalogCoordinator,
            GpuBundleManifestClient = runtimeComposition.GpuBundleManifestClient,
            GpuBundleManifestRuleResolver = runtimeComposition.GpuBundleManifestRuleResolver,
            RuntimeHeaderPresenter = appComposition.RuntimeServices.RuntimeHeaderPresenter,
            RuntimeSummaryStateController = appComposition.RuntimeServices.RuntimeSummaryStateController
        };
        var scan = new MainScanResolvedDependencies
        {
            FolderPickerService = scanComposition.FolderPickerService,
            ScanFolderDiscoveryService = scanComposition.ScanFolderDiscoveryService,
            ScanFlowController = scanComposition.ScanFlowController,
            ScanFolderListController = scanComposition.ScanFolderListController,
            ScanVisibleGameResolver = scanComposition.ScanVisibleGameResolver,
            ScanFolderActionController = scanComposition.ScanFolderActionController,
            ScanResultCoordinatorFactory = scanComposition.ScanResultCoordinatorFactory,
            ScanOrchestratorFactory = scanComposition.ScanOrchestratorFactory
        };
        var install = new MainInstallResolvedDependencies
        {
            GameSelectionFlowController = installComposition.GameSelectionFlowController,
            ArchiveReadinessFlowController = installComposition.ArchiveReadinessFlowController,
            InstallFlowController = installComposition.InstallFlowController,
            InstallPopupPresenter = installComposition.InstallPopupPresenter,
            OptiClickUninstallPlanBuilder = installComposition.OptiClickUninstallPlanBuilder,
            OptiClickUninstallExecutor = installComposition.OptiClickUninstallExecutor,
            InstallExecutionCoordinator = appComposition.InstallServices.InstallExecutionCoordinator,
            UninstallFlowCoordinator = appComposition.InstallServices.UninstallFlowCoordinator,
            MainInstallArchiveReadinessController = appComposition.InstallServices.MainInstallArchiveReadinessController,
            MainInstallPreparationController = appComposition.InstallServices.MainInstallPreparationController,
            MainInstallExecutionBridge = appComposition.InstallServices.MainInstallExecutionBridge,
            MainInstallInteractionController = appComposition.InstallServices.MainInstallInteractionController,
            MainUninstallInteractionController = appComposition.InstallServices.MainUninstallInteractionController,
            MainInstallCompletionController = appComposition.InstallServices.MainInstallCompletionController,
            MainOptiScalerSettingsController = appComposition.OptiScalerSettingsServices.MainOptiScalerSettingsController
        };
        var shell = new MainShellResolvedDependencies
        {
            Ui = new MainShellUiResolvedDependencies
            {
                NavigationState = appComposition.ShellUiServices.NavigationState,
                ShellChrome = appComposition.ShellUiServices.ShellChrome,
                UserSettingsController = appComposition.ShellUiServices.UserSettingsController,
                SupportedGamesWikiMarkdownLoader = appComposition.ShellUiServices.SupportedGamesWikiMarkdownLoader,
                LocalizationStateController = appComposition.ShellUiServices.LocalizationStateController,
                BusyStateApplier = appComposition.ShellUiServices.BusyStateApplier,
                FlowLogDispatcher = appComposition.ShellUiServices.FlowLogDispatcher,
                FlowRequestFactory = appComposition.ShellUiServices.FlowRequestFactory,
                ResultApplier = appComposition.ShellUiServices.ResultApplier
            },
            Dialogs = new MainShellDialogResolvedDependencies
            {
                DialogPresenter = appComposition.ShellDialogServices.DialogPresenter,
                InstallManagementDialogHost = appComposition.ShellDialogServices.InstallManagementDialogHost,
                InstallManagementDialogService = appComposition.ShellDialogServices.InstallManagementDialogService,
                RemoteCatalogDialogGate = appComposition.ShellDialogServices.RemoteCatalogDialogGate,
                DialogHost = appComposition.ShellDialogServices.DialogHost
            },
            Interactions = new MainShellInteractionResolvedDependencies
            {
                ShellCommandActionController = appComposition.ShellSupportServices.ShellCommandActionController,
                ShellInteractionControllers = appComposition.ShellUiServices.InteractionControllers
            },
            Sections = new MainShellSectionResolvedDependencies
            {
                ShellSectionsFactory = appComposition.ShellSectionServices.ShellSectionsFactory,
                ShellSectionsCompositionFactory = appComposition.ShellSectionServices.ShellSectionsCompositionFactory
            }
        };
        var startup = new MainStartupResolvedDependencies
        {
            StartupNoticePresenter = appComposition.StartupServices.StartupNoticePresenter,
            StartupAnnouncementFlowController = appComposition.StartupServices.StartupAnnouncementFlowController,
            GameMasterCoverPrefetchCoordinator = appComposition.ShellSelectionServices.GameMasterCoverPrefetchCoordinator,
            CoverCacheBootstrapService = appComposition.StartupServices.CoverCacheBootstrapService,
            StartupBackgroundTaskManager = appComposition.StartupServices.StartupBackgroundTaskManager,
            ArchiveReadinessRefreshCoordinator = appComposition.StartupServices.ArchiveReadinessRefreshCoordinator,
            ArchiveReadinessWarmupController = appComposition.StartupServices.ArchiveReadinessWarmupController,
            StartupPreparationCoordinator = appComposition.StartupServices.StartupPreparationCoordinator,
            StartupFlowCoordinator = appComposition.StartupServices.StartupFlowCoordinator,
            MainStartupRuntimeFacade = appComposition.StartupServices.MainStartupRuntimeFacade,
            MainStartupFlowController = appComposition.StartupServices.MainStartupFlowController,
            MainStartupDialogsController = appComposition.StartupServices.MainStartupDialogsController
        };
        var selection = new MainSelectionResolvedDependencies
        {
            GameSelectionFlowController = installComposition.GameSelectionFlowController,
            SelectionPopupCoordinator = appComposition.ShellSelectionServices.SelectionPopupCoordinator,
            GameCardSelectionStateController = appComposition.ShellSelectionServices.GameCardSelectionStateController,
            MainSelectionInteractionController = appComposition.ShellSelectionServices.MainSelectionInteractionController,
            MainSelectionRecomputeController = appComposition.ShellSelectionServices.MainSelectionRecomputeController,
            MainLanguageChangeController = appComposition.ShellSelectionServices.MainLanguageChangeController,
            MainVisibleGameCardRefreshController = appComposition.ShellSelectionServices.MainVisibleGameCardRefreshController
        };

        return new MainViewModelCompositionDependencies
        {
            App = app,
            Shell = shell,
            Features = new MainFeatureResolvedDependencies
            {
                Runtime = runtime,
                Scan = scan,
                Install = install,
                Startup = startup,
                Selection = selection,
                Support = new MainSupportResolvedDependencies
                {
                    SupportActionController = appComposition.ShellSupportServices.SupportActionController,
                    SupportIssueContextBuilder = appComposition.ShellSupportServices.SupportIssueContextBuilder
                },
                Update = new MainUpdateResolvedDependencies
                {
                    AppUpdateFlowController = appComposition.AppUpdateFlowController,
                    AppUpdateCoordinator = appComposition.AppUpdateCoordinator
                },
                ShellSections = new MainShellSectionsResolvedDependencies
                {
                    MockDataProvider = app.MockDataProvider,
                    DialogPresenter = shell.DialogPresenter,
                    SupportedGamesWikiMarkdownLoader = shell.SupportedGamesWikiMarkdownLoader,
                    StartupBackgroundTaskManager = startup.StartupBackgroundTaskManager,
                    AppLogger = app.AppLogger,
                    LocalDataPathProvider = app.LocalDataPathProvider,
                    ShellSectionsFactory = shell.ShellSectionsFactory,
                    ShellSectionsCompositionFactory = shell.ShellSectionsCompositionFactory
                },
                RuntimeFlow = new MainRuntimeFlowResolvedDependencies
                {
                    RuntimeContextCoordinator = runtime.RuntimeContextCoordinator,
                    DeviceIdentityRulesFlowController = runtime.DeviceIdentityRulesFlowController,
                    GpuSelectionCoordinator = runtime.GpuSelectionCoordinator
                },
                SelectionScan = new MainSelectionScanResolvedDependencies
                {
                    ShellGameCardViewModelFactory = runtime.ShellGameCardViewModelFactory,
                    ScanVisibleGameResolver = scan.ScanVisibleGameResolver,
                    GameSelectionFlowController = selection.GameSelectionFlowController,
                    SelectionPopupCoordinator = selection.SelectionPopupCoordinator,
                    GameCardSelectionStateController = selection.GameCardSelectionStateController,
                    GpuSelectionCoordinator = runtime.GpuSelectionCoordinator
                }
            }
        };
    }
}
