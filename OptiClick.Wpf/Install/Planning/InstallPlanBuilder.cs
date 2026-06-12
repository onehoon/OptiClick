using OptiClick.Core.Install.Planning;

namespace OptiClick.Wpf.Install.Planning;

public interface IInstallPlanBuilder
{
    CoreInstallPlanBuildResult Build(CoreInstallPlanBuildInput input);
}

public sealed class InstallPlanBuilder : IInstallPlanBuilder
{
    private readonly CoreInstallPlanPolicyBuilder _coreBuilder = new();

    public CoreInstallPlanBuildResult Build(CoreInstallPlanBuildInput input)
    {
        if (input is null)
        {
            return CoreInstallPlanBuildResult.Failure(new CoreInstallPlan(), "invalid_input");
        }

        return _coreBuilder.Build(input);
    }
}
