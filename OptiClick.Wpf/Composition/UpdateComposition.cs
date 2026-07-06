using OptiClick.Wpf.Services;

namespace OptiClick.Wpf.Composition;

public sealed record UpdateCompositionServices
{
    public required IAppVersionProvider AppVersionProvider { get; init; }
    public required IVelopackAppUpdateService VelopackAppUpdateService { get; init; }
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
        var velopackAppUpdateService = _root.CreateVelopackAppUpdateService();
        var appUpdateDialogPresenter = new AppUpdateDialogPresenter();

        return new UpdateCompositionServices
        {
            AppVersionProvider = appVersionProvider,
            VelopackAppUpdateService = velopackAppUpdateService,
            AppUpdateDialogPresenter = appUpdateDialogPresenter,
            AppUpdateFlowController = new AppUpdateFlowController(
                velopackAppUpdateService,
                appUpdateDialogPresenter)
        };
    }
}
