using OptiClick.Wpf.Install.Presentation;
using OptiClick.Wpf.Install.UiState;

namespace OptiClick.Wpf.Install.Flow;

public sealed class InstallFlowController
{
    private readonly InstallFlowExecutionUseCase _executionUseCase;
    private readonly InstallPopupPresenter _installPopupPresenter;
    private readonly IInstallRejectionPresentationResolver? _installRejectionPresentationResolver;

    public InstallFlowController(
        InstallFlowExecutionUseCase executionUseCase,
        InstallPopupPresenter installPopupPresenter,
        IInstallRejectionPresentationResolver? installRejectionPresentationResolver)
    {
        _executionUseCase = executionUseCase ?? throw new ArgumentNullException(nameof(executionUseCase));
        _installPopupPresenter = installPopupPresenter ?? throw new ArgumentNullException(nameof(installPopupPresenter));
        _installRejectionPresentationResolver = installRejectionPresentationResolver;
    }

    public async Task<InstallFlowResult> ExecuteAsync(
        InstallFlowRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _executionUseCase.ExecuteAsync(request, cancellationToken);
        if (!result.WasBlocked || result.GateDecision is null)
        {
            return result;
        }

        var rejectionPopup = _installPopupPresenter.ResolveInstallRejection(
            result.GateDecision,
            _installRejectionPresentationResolver);

        return result with
        {
            PopupRequest = rejectionPopup.Kind == PopupPresentationKind.None ? null : rejectionPopup
        };
    }
}
