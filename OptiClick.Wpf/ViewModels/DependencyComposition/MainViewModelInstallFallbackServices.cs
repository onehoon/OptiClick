using OptiClick.Wpf.Install.Execution;
using OptiClick.Wpf.Install.Gates;
using OptiClick.Wpf.Install.Planning;
using OptiClick.Wpf.Install.Uninstall;

namespace OptiClick.Wpf.ViewModels.DependencyComposition;

internal sealed record MainViewModelInstallFallbackServices
{
    public required IInstallPlanBuilder InstallPlanBuilder { get; init; }
    public required IInstallStartGateResolver InstallStartGateResolver { get; init; }
    public required IComponentInstallCoordinator ComponentInstallCoordinator { get; init; }
    public required IComponentInstallParityReviewBuilder ComponentInstallParityReviewBuilder { get; init; }
    public required IOptiClickUninstallPlanBuilder OptiClickUninstallPlanBuilder { get; init; }
    public required IOptiClickUninstallExecutor OptiClickUninstallExecutor { get; init; }
}
