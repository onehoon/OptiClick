using OptiClick.Core.Install;
using OptiClick.Core.Install.Planning;
using OptiClick.Core.RuntimeData;
using CoreComponentInstallExecutionOrderPolicy = OptiClick.Core.Install.Components.ComponentInstallExecutionOrderPolicy;

namespace OptiClick.Wpf.Install.Execution;

public sealed class ComponentInstallParityReviewBuilder : IComponentInstallParityReviewBuilder
{
    public ComponentInstallParityReviewResult Build(ComponentInstallParityReviewInput input)
    {
        if (input is null)
        {
            return new ComponentInstallParityReviewResult
            {
                IsSuccess = false,
                ErrorCode = ComponentInstallParityReviewCodes.InvalidInput
            };
        }

        var plan = input.Plan ?? new CoreInstallPlan();
        var finalProxy = (plan.FinalProxyDllName ?? "").Trim();
        var chain = ProxyDllNamePolicy.BuildCandidateChainForPreferred(finalProxy);
        var events = new List<ComponentInstallParityEvent>
        {
            new()
            {
                Code = ComponentInstallParityReviewCodes.ComponentOrder,
                Detail = string.Join(
                    " -> ",
                    CoreComponentInstallExecutionOrderPolicy.GetCoreThenMiddleThenExtraOrder()
                        .Select(static name => name.ToString()))
            }
        };

        if (string.IsNullOrWhiteSpace(finalProxy))
        {
            events.Add(new ComponentInstallParityEvent
            {
                Code = ComponentInstallParityReviewCodes.FinalProxyMissing,
                Detail = "FinalProxyDllName is empty."
            });
        }

        if (chain.Count == 0 && !string.IsNullOrWhiteSpace(finalProxy))
        {
            events.Add(new ComponentInstallParityEvent
            {
                Code = ComponentInstallParityReviewCodes.ProxyChainUnresolved,
                Detail = finalProxy
            });
        }

        var profileRows = input.ProfileRows ?? AttachedRuntimeProfileRows.Empty;
        return new ComponentInstallParityReviewResult
        {
            IsSuccess = true,
            Events = events,
            FinalProxyDllName = finalProxy,
            ProxyCandidateChain = chain,
            ManagedBackupCandidates = OptiScalerManagedBackupPolicy.TargetFileNames.ToArray(),
            LegacyCleanupTargets = OptiScalerLegacyCleanupPolicy.TargetFileNames.ToArray(),
            ReFramework = IsEnabled(plan, CoreInstallPlanComponentType.REFramework),
            SpecialK = IsEnabled(plan, CoreInstallPlanComponentType.SpecialK),
            Unreal5 = IsEnabled(plan, CoreInstallPlanComponentType.Unreal5),
            ExtraBundle = IsEnabled(plan, CoreInstallPlanComponentType.ExtraBundle),
            RtssOverlay = IsEnabled(plan, CoreInstallPlanComponentType.RtssProfile),
            ExcludeListPatterns = plan.ExcludeListPatterns ?? Array.Empty<string>(),
            GameIniProfileRowCount = profileRows.GameIniProfileRows.Count,
            GameUnrealIniProfileRowCount = profileRows.GameUnrealIniProfileRows.Count,
            GameXmlProfileRowCount = profileRows.GameXmlProfileRows.Count,
            GameJsonProfileRowCount = profileRows.GameJsonProfileRows.Count,
            EngineIniProfileRowCount = profileRows.EngineIniProfileRows.Count,
            RegistryProfileRowCount = profileRows.RegistryProfileRows.Count
        };
    }

    private static bool IsEnabled(CoreInstallPlan plan, CoreInstallPlanComponentType type)
    {
        return plan.Components.Any(component => component.Type == type && component.Enabled);
    }
}
