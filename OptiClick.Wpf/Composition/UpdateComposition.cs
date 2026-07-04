using System.Net.Http;
using OptiClick.Wpf.Services;

namespace OptiClick.Wpf.Composition;

public sealed record UpdateCompositionServices
{
    public required IAppVersionProvider AppVersionProvider { get; init; }
    public required IAppUpdateVersionComparer AppUpdateVersionComparer { get; init; }
    public required IAppUpdateService AppUpdateService { get; init; }
    public required IAppUpdateExecutionService AppUpdateExecutionService { get; init; }
    public required AppUpdateDialogPresenter AppUpdateDialogPresenter { get; init; }
    public required AppUpdateFlowController AppUpdateFlowController { get; init; }
}

public sealed class UpdateComposition
{
    private readonly AppCompositionRoot _root;

    public UpdateComposition(AppCompositionRoot root)
    {
        _root = root ?? throw new ArgumentNullException(nameof(root));
    }

    public UpdateCompositionServices CreateUpdateServices(AppSharedServices app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var appVersionProvider = _root.CreateAppVersionProvider();
        var appUpdateVersionComparer = _root.CreateAppUpdateVersionComparer();
        var appUpdateService = _root.CreateAppUpdateService(appUpdateVersionComparer);
        var remoteOptions = app.RemoteEndpointProvider.GetRemoteDataOptions();
        var publicAppUpdateService = new PublicAppUpdateService(
            remoteOptions?.DataV2BaseUrl ?? "",
            new HttpClient(),
            appUpdateVersionComparer,
            app.AppLogger);
        var appUpdateExecutionService = new LazyAppUpdateExecutionService(() =>
            _root.CreateAppUpdateExecutionService(app.LocalDataPathProvider, app.AppLogger));
        var appUpdateDialogPresenter = new AppUpdateDialogPresenter();

        return new UpdateCompositionServices
        {
            AppVersionProvider = appVersionProvider,
            AppUpdateVersionComparer = appUpdateVersionComparer,
            AppUpdateService = appUpdateService,
            AppUpdateExecutionService = appUpdateExecutionService,
            AppUpdateDialogPresenter = appUpdateDialogPresenter,
            AppUpdateFlowController = new AppUpdateFlowController(
                appUpdateService,
                appUpdateExecutionService,
                app.ExternalUrlLauncher,
                publicAppUpdateService,
                appUpdateDialogPresenter)
        };
    }

    private sealed class LazyAppUpdateExecutionService : IAppUpdateExecutionService
    {
        private readonly Lazy<IAppUpdateExecutionService> _inner;

        public LazyAppUpdateExecutionService(Func<IAppUpdateExecutionService> factory)
        {
            _inner = new Lazy<IAppUpdateExecutionService>(factory);
        }

        public Task<AppUpdateExecutionResult> PrepareAndCopyUpdateAsync(
            AppUpdateInfo updateInfo,
            CancellationToken cancellationToken = default)
        {
            return _inner.Value.PrepareAndCopyUpdateAsync(updateInfo, cancellationToken);
        }

        public AppUpdateExecutionResult TryLaunchCopiedExecutable(string finalCopiedExePath)
        {
            return _inner.Value.TryLaunchCopiedExecutable(finalCopiedExePath);
        }
    }
}
