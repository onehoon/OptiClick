using OptiClick.Wpf.Install.Execution;
using OptiClick.Wpf.Install.UiState;

namespace OptiClick.Wpf.Install.Fallbacks;

internal sealed class UnavailableComponentInstallCoordinator : IComponentInstallCoordinator
{
    public Task<ComponentInstallResult> ExecuteAsync(
        ComponentInstallContext context,
        CancellationToken cancellationToken = default)
    {
        _ = context;
        _ = cancellationToken;

        var failedStep = ComponentInstallStepResult.Failed(
            ComponentInstallName.OptiScalerCore,
            InstallEntryRejectionCodes.InstallExecutionUnavailable,
            "install execution dependencies were not composed");
        return Task.FromResult(new ComponentInstallResult
        {
            IsSuccess = false,
            FailedStep = failedStep,
            Steps = [failedStep]
        });
    }
}
