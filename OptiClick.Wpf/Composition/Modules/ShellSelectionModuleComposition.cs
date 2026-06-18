using OptiClick.Wpf.Install.Flow;
using OptiClick.Wpf.Logging;
using OptiClick.Wpf.Services;
using OptiClick.Wpf.Shell.Dialogs;
using OptiClick.Wpf.Shell.Flow;
using OptiClick.Wpf.Shell.Games;
using OptiClick.Wpf.Shell.Selection;
using OptiClick.Wpf.Shell.Startup;
using OptiClick.Wpf.ViewModels;
using OptiClick.Wpf.ViewModels.DependencyComposition;

namespace OptiClick.Wpf.Composition.Modules;

internal sealed record ShellSelectionModuleCompositionServices
{
    public required SelectionPopupCoordinator SelectionPopupCoordinator { get; init; }
    public required GameCardSelectionStateController GameCardSelectionStateController { get; init; }
    public required GameMasterCoverPrefetchCoordinator GameMasterCoverPrefetchCoordinator { get; init; }
    public required MainSelectionInteractionController MainSelectionInteractionController { get; init; }
    public required MainSelectionRecomputeController MainSelectionRecomputeController { get; init; }
    public required MainLanguageChangeController MainLanguageChangeController { get; init; }
    public required MainVisibleGameCardRefreshController MainVisibleGameCardRefreshController { get; init; }
}

internal static class ShellSelectionModuleComposition
{
    public static ShellSelectionModuleCompositionServices Compose(
        MainViewModelAppDependencies appDependencies,
        InstallDependencyComposition installComposition,
        StartupModuleCompositionServices startupComposition,
        DialogPresenter dialogPresenter,
        FlowLogDispatcher flowLogDispatcher,
        IAppLogger appLogger)
    {
        ArgumentNullException.ThrowIfNull(appDependencies);
        ArgumentNullException.ThrowIfNull(installComposition);
        ArgumentNullException.ThrowIfNull(startupComposition);
        ArgumentNullException.ThrowIfNull(dialogPresenter);
        ArgumentNullException.ThrowIfNull(flowLogDispatcher);
        ArgumentNullException.ThrowIfNull(appLogger);

        var selectionPopupCoordinator = appDependencies.SelectionPopupCoordinator
                                        ?? new SelectionPopupCoordinator(
                                             installComposition.GameSelectionFlowController,
                                             dialogPresenter,
                                             flowLogDispatcher,
                                             appLogger,
                                             appDependencies.ExternalUrlLauncher);
        var gameCardSelectionStateController = appDependencies.GameCardSelectionStateController
                                               ?? new GameCardSelectionStateController();
        var gameMasterCoverPrefetchService = appDependencies.GameMasterCoverPrefetchService
                                             ?? new GameMasterCoverPrefetchService();
        var gameMasterCoverPrefetchCoordinator = new GameMasterCoverPrefetchCoordinator(
            gameMasterCoverPrefetchService,
            startupComposition.StartupBackgroundTaskManager);
        var mainSelectionInteractionController = appDependencies.MainSelectionInteractionController
                                                 ?? new MainSelectionInteractionController();
        var mainSelectionRecomputeController = appDependencies.MainSelectionRecomputeController
                                               ?? new MainSelectionRecomputeController();
        var mainLanguageChangeController = appDependencies.MainLanguageChangeController
                                           ?? new MainLanguageChangeController();
        var mainVisibleGameCardRefreshController = appDependencies.MainVisibleGameCardRefreshController
                                                   ?? new MainVisibleGameCardRefreshController();

        return new ShellSelectionModuleCompositionServices
        {
            SelectionPopupCoordinator = selectionPopupCoordinator,
            GameCardSelectionStateController = gameCardSelectionStateController,
            GameMasterCoverPrefetchCoordinator = gameMasterCoverPrefetchCoordinator,
            MainSelectionInteractionController = mainSelectionInteractionController,
            MainSelectionRecomputeController = mainSelectionRecomputeController,
            MainLanguageChangeController = mainLanguageChangeController,
            MainVisibleGameCardRefreshController = mainVisibleGameCardRefreshController
        };
    }
}
