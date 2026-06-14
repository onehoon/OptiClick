using OptiClick.Wpf.Composition;
using OptiClick.Wpf.Composition.Modules;
using OptiClick.Wpf.Configuration;
using OptiClick.Wpf.Install.Archives;
using OptiClick.Wpf.Install.Execution;
using OptiClick.Wpf.Install.FileSystem;
using OptiClick.Wpf.Install.Flow;
using OptiClick.Wpf.Logging;
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

namespace OptiClick.Wpf.Composition.Features;

internal static class MainWindowShellFeatureComposer
{
    public static MainWindowShellFeatureComposition Compose(
        AppSharedServices app,
        InstallCompositionServices install,
        StartupNoticePresenter startupNoticePresenter,
        MainWindowStartupCompositionServices startupComposition,
        SupportCompositionServices support)
    {
        var navigationState = new ShellNavigationState();
        var shellChrome = ShellChromeViewModels.Create(navigationState);
        var flowLogDispatcher = new FlowLogDispatcher(app.AppLogger);
        var dialogPresenter = new DialogPresenter(app.DialogService, app.AppLogger);
        var shellCommandActionController = new ShellCommandActionController(
            startupNoticePresenter,
            support.SupportIssueContextBuilder,
            support.SupportActionController);
        var selectionServices = ComposeShellSelection(
            install,
            startupComposition,
            support,
            dialogPresenter,
            flowLogDispatcher,
            app.AppLogger);
        var shellSectionsFactory = new ShellSectionsFactory();
        var shellSectionsCompositionFactory = new ShellSectionsCompositionFactory();
        var shell = new MainShellResolvedDependencies
        {
            Ui = new MainShellUiResolvedDependencies
            {
                NavigationState = navigationState,
                ShellChrome = shellChrome,
                UserSettingsController = new UserSettingsController(app.UserSettingsStore, app.AppLogger),
                SupportedGamesWikiMarkdownLoader = startupComposition.SupportedGamesWikiMarkdownLoader,
                LocalizationStateController = new LocalizationStateController(),
                BusyStateApplier = new MainViewModelBusyStateApplier(),
                FlowLogDispatcher = flowLogDispatcher,
                FlowRequestFactory = new MainViewModelFlowRequestFactory(),
                ResultApplier = new MainViewModelResultApplier()
            },
            Dialogs = new MainShellDialogResolvedDependencies
            {
                DialogPresenter = dialogPresenter,
                InstallManagementDialogHost = app.InstallManagementDialogHost,
                InstallManagementDialogService = app.InstallManagementDialogService,
                RemoteCatalogDialogGate = new OnceDialogGate(),
                DialogHost = app.DialogHost
            },
            Interactions = new MainShellInteractionResolvedDependencies
            {
                ShellCommandActionController = shellCommandActionController,
                ShellInteractionControllers = new MainShellInteractionControllers
                {
                    OptiScalerDirtyNavigationGuard = new OptiScalerDirtyNavigationGuard(),
                    AppUpdateInteractionController = new MainAppUpdateInteractionController(),
                    UserSettingsApplyController = new MainUserSettingsApplyController()
                }
            },
            Sections = new MainShellSectionResolvedDependencies
            {
                ShellSectionsFactory = shellSectionsFactory,
                ShellSectionsCompositionFactory = shellSectionsCompositionFactory
            }
        };

        return new MainWindowShellFeatureComposition
        {
            Shell = shell,
            SelectionServices = selectionServices
        };
    }

    private static ShellSelectionModuleCompositionServices ComposeShellSelection(
        InstallCompositionServices install,
        MainWindowStartupCompositionServices startupComposition,
        SupportCompositionServices support,
        DialogPresenter dialogPresenter,
        FlowLogDispatcher flowLogDispatcher,
        IAppLogger appLogger)
    {
        return new ShellSelectionModuleCompositionServices
        {
            SelectionPopupCoordinator = new SelectionPopupCoordinator(
                install.GameSelectionFlowController,
                dialogPresenter,
                flowLogDispatcher,
                appLogger),
            GameCardSelectionStateController = new GameCardSelectionStateController(),
            GameMasterCoverPrefetchCoordinator = new GameMasterCoverPrefetchCoordinator(
                new GameMasterCoverPrefetchService(),
                startupComposition.StartupBackgroundTaskManager),
            MainSelectionInteractionController = new MainSelectionInteractionController(),
            MainSelectionRecomputeController = new MainSelectionRecomputeController(),
            MainLanguageChangeController = new MainLanguageChangeController(),
            MainVisibleGameCardRefreshController = new MainVisibleGameCardRefreshController()
        };
    }
}

internal sealed record MainWindowShellFeatureComposition
{
    public required MainShellResolvedDependencies Shell { get; init; }
    public required ShellSelectionModuleCompositionServices SelectionServices { get; init; }
}
