using System.Net.Http;
using OptiClick.Core.Games.Wiki;
using OptiClick.Wpf.Configuration;
using OptiClick.Wpf.Install.Archives;
using OptiClick.Wpf.Install.Execution;
using OptiClick.Wpf.Install.FileSystem;
using OptiClick.Wpf.Shell.Startup;
using OptiClick.Wpf.Shell.Wiki;

namespace OptiClick.Wpf.Composition.Modules;

internal sealed record MainWindowStartupCompositionServices
{
    public required SupportedGamesWikiMarkdownLoader SupportedGamesWikiMarkdownLoader { get; init; }
    public required StartupAnnouncementFlowController StartupAnnouncementFlowController { get; init; }
    public required StartupBackgroundTaskManager StartupBackgroundTaskManager { get; init; }
    public required ArchiveReadinessRefreshCoordinator ArchiveReadinessRefreshCoordinator { get; init; }
    public required ArchiveReadinessWarmupController ArchiveReadinessWarmupController { get; init; }
    public required StartupFlowCoordinator StartupFlowCoordinator { get; init; }
    public required CoverCacheBootstrapService CoverCacheBootstrapService { get; init; }
    public required StartupPreparationCoordinator StartupPreparationCoordinator { get; init; }
}

internal static class MainWindowStartupCompositionFactory
{
    public static MainWindowStartupCompositionServices Create(
        AppSharedServices app,
        StartupNoticePresenter startupNoticePresenter)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(startupNoticePresenter);

        var supportedGamesWikiOptionsLoader = new SupportedGamesWikiOptionsLoader();
        var supportedGamesWikiMarkdownLoader = new SupportedGamesWikiMarkdownLoader(
            supportedGamesWikiOptionsLoader.Load(),
            new HttpClient(),
            new SupportedGamesWikiMarkdownParser(),
            new SupportedGamesWikiMarkdownCacheStore(app.LocalDataPathProvider, app.AppLogger),
            app.AppLogger);
        var startupAnnouncementFlowController = new StartupAnnouncementFlowController(startupNoticePresenter);
        var startupBackgroundTaskManager = new StartupBackgroundTaskManager();
        var archiveReadinessRefreshCoordinator = new ArchiveReadinessRefreshCoordinator();
        var archiveReadinessWarmupController = new ArchiveReadinessWarmupController();
        var startupFlowCoordinator = new StartupFlowCoordinator();
        var coverCacheBootstrapFileSystem = new InstallFileSystem();
        var coverCacheBootstrapExtraBundleInstaller = new ExtraBundleInstaller(
            new ArchiveDownloader(
                new HttpClient(),
                requestPreparer: app.SecurityServices.ArchiveDownloadRequestPreparer,
                serverClock: app.SecurityServices.ServerClock,
                logger: app.AppLogger),
            new ZipArchiveExtractor(),
            coverCacheBootstrapFileSystem,
            app.LocalDataPathProvider.InstallExecutionTempDirectory);
        var coverCacheBootstrapService = new CoverCacheBootstrapService(
            coverCacheBootstrapExtraBundleInstaller,
            coverCacheBootstrapFileSystem,
            app.LocalDataPathProvider,
            app.AppLogger);
        var startupPreparationCoordinator = new StartupPreparationCoordinator(
            startupBackgroundTaskManager,
            archiveReadinessRefreshCoordinator,
            archiveReadinessWarmupController,
            app.FirstRunStateStore,
            coverCacheBootstrapService,
            app.LocalDataPathProvider);

        return new MainWindowStartupCompositionServices
        {
            SupportedGamesWikiMarkdownLoader = supportedGamesWikiMarkdownLoader,
            StartupAnnouncementFlowController = startupAnnouncementFlowController,
            StartupBackgroundTaskManager = startupBackgroundTaskManager,
            ArchiveReadinessRefreshCoordinator = archiveReadinessRefreshCoordinator,
            ArchiveReadinessWarmupController = archiveReadinessWarmupController,
            StartupFlowCoordinator = startupFlowCoordinator,
            CoverCacheBootstrapService = coverCacheBootstrapService,
            StartupPreparationCoordinator = startupPreparationCoordinator
        };
    }
}
