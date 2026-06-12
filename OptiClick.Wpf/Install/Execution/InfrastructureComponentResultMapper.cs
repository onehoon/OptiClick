using InfrastructureComponents = OptiClick.Infrastructure.Install.Components;

namespace OptiClick.Wpf.Install.Execution;

internal static class InfrastructureComponentResultMapper
{
    public static ComponentInstallStepResult ToWpfStepResult(InfrastructureComponents.ComponentInstallStepResult result)
    {
        return result.ToCore();
    }
}
