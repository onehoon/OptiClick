using OptiClick.Core.Install.Planning;
using OptiClick.Wpf.Install.Planning;

namespace OptiClick.Wpf.Install.Fallbacks;

internal sealed class UnavailableInstallPlanBuilder : IInstallPlanBuilder
{
    public CoreInstallPlanBuildResult Build(CoreInstallPlanBuildInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return CoreInstallPlanBuildResult.Success(new CoreInstallPlan
        {
            IsAllowed = false,
            GameId = input.GameDescriptor?.GameId ?? "",
            GameDisplayName = input.GameDescriptor?.GameNameEn ?? "",
            TargetFolder = input.TargetFolderHint,
            MatchedExe = input.MatchedExeHint,
            FinalProxyDllName = input.Precheck.ResolvedDllName
        });
    }
}
