using OptiClick.Wpf.Install.Execution;
using OptiClick.Wpf.Install.Flow;
using OptiClick.Wpf.Install.Gates;
using OptiClick.Wpf.Install.Planning;
using OptiClick.Wpf.Install.Presentation;
using OptiClick.Wpf.Install.UiState;

namespace OptiClick.Wpf.Composition.Modules;

internal sealed record InstallFlowCompositionServices
{
    public required InstallFlowController InstallFlowController { get; init; }
}

internal sealed record InstallFlowCompositionRequest
{
    public required IInstallPlanBuilder InstallPlanBuilder { get; init; }
    public required IComponentInstallParityReviewBuilder ComponentInstallParityReviewBuilder { get; init; }
    public required InstallPlanInputBuilder InstallPlanInputBuilder { get; init; }
    public required IInstallStartGateResolver InstallStartGateResolver { get; init; }
    public required ComponentInstallContextBuilder ComponentInstallContextBuilder { get; init; }
    public required IComponentInstallCoordinator ComponentInstallCoordinator { get; init; }
    public required IInstallResultApplier InstallResultApplier { get; init; }
    public required InstallPopupPresenter InstallPopupPresenter { get; init; }
    public required IInstallRejectionPresentationResolver InstallRejectionPresentationResolver { get; init; }
}

internal static class InstallFlowCompositionFactory
{
    public static InstallFlowCompositionServices Create(InstallFlowCompositionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var installFlowApplyService = new InstallFlowApplyService(request.InstallResultApplier);
        var planPreparationService = new InstallFlowPlanPreparationService(
            request.InstallPlanBuilder,
            request.ComponentInstallParityReviewBuilder,
            request.InstallPlanInputBuilder);
        var startGateService = new InstallFlowStartGateService(request.InstallStartGateResolver);
        var componentExecutionService = new InstallFlowComponentExecutionService(
            request.ComponentInstallContextBuilder,
            request.ComponentInstallCoordinator);
        var installFlowExecutionUseCase = new InstallFlowExecutionUseCase(
            planPreparationService,
            startGateService,
            componentExecutionService,
            installFlowApplyService);

        return new InstallFlowCompositionServices
        {
            InstallFlowController = new InstallFlowController(
                installFlowExecutionUseCase,
                request.InstallPopupPresenter,
                request.InstallRejectionPresentationResolver)
        };
    }
}
