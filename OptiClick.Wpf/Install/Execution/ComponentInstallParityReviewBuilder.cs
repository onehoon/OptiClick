using OptiClick.Core.Install;
using OptiClick.Wpf.Install.Planning;

namespace OptiClick.Wpf.Install.Execution;

public sealed class ComponentInstallParityReviewBuilder : IComponentInstallParityReviewBuilder
{
    private static readonly ComponentInstallName[] ExpectedExecutionOrder =
    [
        ComponentInstallName.OptiScalerCore,
        ComponentInstallName.UltimateAsiLoader,
        ComponentInstallName.SpecialK,
        ComponentInstallName.ReFramework,
        ComponentInstallName.OptiPatcher,
        ComponentInstallName.Unreal5,
        ComponentInstallName.Fsr4,
        ComponentInstallName.ExtraBundle
    ];

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

        var plan = input.Plan ?? new InstallPlan();
        var finalProxy = (plan.FinalProxyDllName ?? "").Trim();
        var chain = ProxyDllNameResolver.BuildCandidateChainForPreferred(finalProxy);
        var events = new List<ComponentInstallParityEvent>
        {
            new()
            {
                Code = ComponentInstallParityReviewCodes.ComponentOrder,
                Detail = string.Join(
                    " -> ",
                    ExpectedExecutionOrder.Select(static name => name.ToString()))
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

        var profileRows = plan.ProfileRows;
        return new ComponentInstallParityReviewResult
        {
            IsSuccess = true,
            Events = events,
            FinalProxyDllName = finalProxy,
            ProxyCandidateChain = chain,
            ManagedBackupCandidates = OptiScalerManagedBackupPolicy.TargetFileNames.ToArray(),
            LegacyCleanupTargets = OptiScalerLegacyCleanupPolicy.TargetFileNames.ToArray(),
            OptiPatcher = IsEnabled(plan, InstallPlanComponentType.OptiPatcher),
            ReFramework = IsEnabled(plan, InstallPlanComponentType.REFramework),
            SpecialK = IsEnabled(plan, InstallPlanComponentType.SpecialK),
            UltimateAsiLoader = IsEnabled(plan, InstallPlanComponentType.UltimateAsiLoader),
            Unreal5 = IsEnabled(plan, InstallPlanComponentType.Unreal5),
            ExtraBundle = IsEnabled(plan, InstallPlanComponentType.ExtraBundle),
            RtssOverlay = IsEnabled(plan, InstallPlanComponentType.RtssProfile),
            ExcludeListPatterns = plan.ExcludeListPatterns ?? Array.Empty<string>(),
            GameIniProfileRowCount = profileRows.GameIniProfileRows.Count,
            GameUnrealIniProfileRowCount = profileRows.GameUnrealIniProfileRows.Count,
            GameXmlProfileRowCount = profileRows.GameXmlProfileRows.Count,
            GameJsonProfileRowCount = profileRows.GameJsonProfileRows.Count,
            EngineIniProfileRowCount = profileRows.EngineIniProfileRows.Count,
            RegistryProfileRowCount = profileRows.RegistryProfileRows.Count
        };
    }

    private static bool IsEnabled(InstallPlan plan, InstallPlanComponentType type)
    {
        return plan.Components.Any(component => component.Type == type && component.Enabled);
    }

}
