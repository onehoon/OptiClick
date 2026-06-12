using OptiClick.Core.Install;
using OptiClick.Core.Install.Planning;
using OptiClick.Wpf.Install.Execution;

namespace OptiClick.Wpf.Install.Gates;

internal static class InstallStartGatePlanFailureResolver
{
    private static readonly HashSet<string> BlockingComponentReviewCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        ComponentInstallParityReviewCodes.FinalProxyMissing,
        ComponentInstallParityReviewCodes.ProxyChainUnresolved
    };

    public static string Resolve(InstallStartGateInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (input.InstallPlan is not null && string.IsNullOrWhiteSpace(input.InstallPlan.FinalProxyDllName))
        {
            return InstallStartGateReasonCodes.FinalProxyMissing;
        }

        if (input.InstallPlan is not null
            && ProxyDllNamePolicy.BuildCandidateChainForPreferred(input.InstallPlan.FinalProxyDllName).Count == 0)
        {
            return InstallStartGateReasonCodes.ProxyChainUnresolved;
        }

        if (!IsValidInstallPlan(input.InstallPlan, input.ComponentReview))
        {
            return InstallStartGateReasonCodes.InvalidInstallPlan;
        }

        return "";
    }

    private static bool IsValidInstallPlan(CoreInstallPlan? plan, ComponentInstallParityReviewResult? componentReview)
    {
        if (plan is null)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(plan.TargetFolder))
        {
            return false;
        }

        if (!ProxyDllNamePolicy.TryResolvePreferredStart(plan.FinalProxyDllName, out _, out _))
        {
            return false;
        }

        if (componentReview is null)
        {
            return true;
        }

        if (!componentReview.IsSuccess)
        {
            return false;
        }

        return !componentReview.Events.Any(static e => BlockingComponentReviewCodes.Contains(e.Code ?? ""));
    }
}
