using OptiClick.Wpf.Install.Archives;
using OptiClick.Wpf.Shell.Startup;
using OptiClick.Wpf.ViewModels;
using OptiClick.Wpf.ViewModels.DependencyComposition;

namespace OptiClick.Wpf.Composition.Modules;

internal sealed record StartupModuleCompositionServices
{
    public required StartupNoticePresenter StartupNoticePresenter { get; init; }
    public required StartupAnnouncementFlowController StartupAnnouncementFlowController { get; init; }
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

internal static class StartupModuleComposition
{
    public static StartupModuleCompositionServices Compose(
        MainViewModelAppDependencies appDependencies,
        MainViewModelAppFallbackServices fallbackServices)
    {
        ArgumentNullException.ThrowIfNull(appDependencies);
        ArgumentNullException.ThrowIfNull(fallbackServices);

        var startupNoticePresenter = appDependencies.StartupNoticePresenter ?? new StartupNoticePresenter();
        var startupAnnouncementFlowController = appDependencies.StartupAnnouncementFlowController
                                                ?? new StartupAnnouncementFlowController(startupNoticePresenter);
        var coverCacheBootstrapService = appDependencies.CoverCacheBootstrapService
                                         ?? NoOpCoverCacheBootstrapService.Instance;
        var startupBackgroundTaskManager = appDependencies.StartupBackgroundTaskManager ?? new StartupBackgroundTaskManager();
        var archiveReadinessRefreshCoordinator = appDependencies.ArchiveReadinessRefreshCoordinator
                                                ?? new ArchiveReadinessRefreshCoordinator();
        var archiveReadinessWarmupController = appDependencies.ArchiveReadinessWarmupController
                                              ?? new ArchiveReadinessWarmupController();
        var startupPreparationCoordinator = appDependencies.StartupPreparationCoordinator
                                            ?? new StartupPreparationCoordinator(
                                                startupBackgroundTaskManager,
                                                archiveReadinessRefreshCoordinator,
                                                archiveReadinessWarmupController,
                                                coverCacheBootstrapService,
                                                fallbackServices.LocalDataPathProvider);
        var startupFlowCoordinator = appDependencies.StartupFlowCoordinator ?? new StartupFlowCoordinator();
        var mainStartupRuntimeFacade = appDependencies.MainStartupRuntimeFacade
                                       ?? new MainStartupRuntimeFacade(
                                           new MainStartupOrchestrator(),
                                           new MainRuntimeOrchestrator());
        var mainStartupFlowController = appDependencies.MainStartupFlowController
                                        ?? new MainStartupFlowController(mainStartupRuntimeFacade);
        var mainStartupDialogsController = appDependencies.MainStartupDialogsController
                                           ?? new MainStartupDialogsController();

        return new StartupModuleCompositionServices
        {
            StartupNoticePresenter = startupNoticePresenter,
            StartupAnnouncementFlowController = startupAnnouncementFlowController,
            CoverCacheBootstrapService = coverCacheBootstrapService,
            StartupBackgroundTaskManager = startupBackgroundTaskManager,
            ArchiveReadinessRefreshCoordinator = archiveReadinessRefreshCoordinator,
            ArchiveReadinessWarmupController = archiveReadinessWarmupController,
            StartupPreparationCoordinator = startupPreparationCoordinator,
            StartupFlowCoordinator = startupFlowCoordinator,
            MainStartupRuntimeFacade = mainStartupRuntimeFacade,
            MainStartupFlowController = mainStartupFlowController,
            MainStartupDialogsController = mainStartupDialogsController
        };
    }
}
