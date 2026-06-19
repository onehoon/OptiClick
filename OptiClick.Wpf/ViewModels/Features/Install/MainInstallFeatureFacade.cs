using OptiClick.Wpf.Install.Archives;
using OptiClick.Wpf.Install.Presentation;
using OptiClick.Wpf.Install.UiState;
using OptiClick.Wpf.Localization;
using OptiClick.Wpf.Models;
using OptiClick.Wpf.ViewModels;

namespace OptiClick.Wpf.ViewModels.Features.Install;

internal sealed class MainInstallFeatureFacade
{
    private readonly InstallPopupPresenter _popupPresenter;
    private readonly MainInstallArchiveReadinessController _archiveReadinessController;
    private readonly MainInstallArchiveReadinessContextFactory _archiveReadinessContextFactory;
    private readonly MainInstallPreparationController _preparationController;
    private readonly MainInstallPreparationContextFactory _preparationContextFactory;
    private readonly MainInstallExecutionBridge _executionBridge;
    private readonly MainInstallExecutionBridgeContextFactory _executionBridgeContextFactory;
    private readonly MainInstallInteractionController _installInteractionController;
    private readonly MainInstallInteractionContextFactory _installInteractionContextFactory;
    private readonly MainUninstallInteractionController _uninstallInteractionController;
    private readonly MainUninstallInteractionContextFactory _uninstallInteractionContextFactory;

    public MainInstallFeatureFacade(
        InstallPopupPresenter popupPresenter,
        MainInstallArchiveReadinessController archiveReadinessController,
        MainInstallPreparationController preparationController,
        MainInstallExecutionBridge executionBridge,
        MainInstallInteractionController installInteractionController,
        MainUninstallInteractionController uninstallInteractionController,
        MainInstallInteractionContextFactory installInteractionContextFactory,
        MainUninstallInteractionContextFactory uninstallInteractionContextFactory,
        MainInstallArchiveReadinessContextFactory archiveReadinessContextFactory,
        MainInstallPreparationContextFactory preparationContextFactory,
        MainInstallExecutionBridgeContextFactory executionBridgeContextFactory)
    {
        _popupPresenter = popupPresenter ?? throw new ArgumentNullException(nameof(popupPresenter));
        _archiveReadinessController =
            archiveReadinessController ?? throw new ArgumentNullException(nameof(archiveReadinessController));
        _archiveReadinessContextFactory =
            archiveReadinessContextFactory ?? throw new ArgumentNullException(nameof(archiveReadinessContextFactory));
        _preparationController = preparationController ?? throw new ArgumentNullException(nameof(preparationController));
        _preparationContextFactory =
            preparationContextFactory ?? throw new ArgumentNullException(nameof(preparationContextFactory));
        _executionBridge = executionBridge ?? throw new ArgumentNullException(nameof(executionBridge));
        _executionBridgeContextFactory =
            executionBridgeContextFactory ?? throw new ArgumentNullException(nameof(executionBridgeContextFactory));
        _installInteractionController =
            installInteractionController ?? throw new ArgumentNullException(nameof(installInteractionController));
        _installInteractionContextFactory =
            installInteractionContextFactory ?? throw new ArgumentNullException(nameof(installInteractionContextFactory));
        _uninstallInteractionController =
            uninstallInteractionController ?? throw new ArgumentNullException(nameof(uninstallInteractionController));
        _uninstallInteractionContextFactory =
            uninstallInteractionContextFactory ?? throw new ArgumentNullException(nameof(uninstallInteractionContextFactory));
    }

    public Task ShowInstallDialogAsync(CancellationToken cancellationToken = default)
    {
        return _installInteractionController.ShowInstallDialogAsync(
            _installInteractionContextFactory.Create(),
            cancellationToken);
    }

    public async Task ExecuteCurrentInstallFlowAsync(CancellationToken cancellationToken)
    {
        var preparation = await _preparationController.PrepareAsync(
            _preparationContextFactory.Create(),
            cancellationToken);
        if (preparation is null)
        {
            return;
        }

        await _executionBridge.ExecuteAsync(
            _executionBridgeContextFactory.Create(preparation),
            cancellationToken);
    }

    public Task HandleUninstallAsync(GameCardViewModel selectedGame, CancellationToken cancellationToken)
    {
        return _uninstallInteractionController.HandleUninstallAsync(
            _uninstallInteractionContextFactory.CreateInteractionContext(selectedGame),
            selectedGame,
            cancellationToken);
    }

    public Task<ArchiveReadinessFlowResult> RefreshArchiveReadinessAsync(CancellationToken cancellationToken)
    {
        return _archiveReadinessController.RefreshAsync(
            _archiveReadinessContextFactory.Create(),
            cancellationToken);
    }

    public Task<ArchiveReadinessFlowResult> RefreshArchiveReadinessForInstallAsync(
        CancellationToken cancellationToken)
    {
        return _archiveReadinessController.RefreshAsync(
            _archiveReadinessContextFactory.Create(refreshVisibleGamesAfterArchiveReadiness: false),
            cancellationToken);
    }

    public Task<ArchiveReadinessFlowResult> RefreshArchiveReadinessWithoutCoordinatorAsync(
        CancellationToken cancellationToken)
    {
        return _archiveReadinessController.RefreshWithoutCoordinatorAsync(
            _archiveReadinessContextFactory.Create(),
            cancellationToken);
    }

    public AppDialogRequest BuildDialogRequest(PopupPresentationRequest popup, AppStrings strings)
    {
        return _popupPresenter.BuildDialogRequest(popup, strings);
    }
}
