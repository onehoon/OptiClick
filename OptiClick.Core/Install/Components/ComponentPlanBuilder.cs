using OptiClick.Core.Install;
using OptiClick.Core.Models;

namespace OptiClick.Core.Install.Components;

public sealed class ComponentPlanBuilder
{
    public IReadOnlyList<ComponentPlan> CreateComponentPlan(
        GameEntry game,
        ExistingFileSnapshot snapshot,
        ProxyDllResolutionResult proxy,
        ArchiveState archiveState)
    {
        var plans = new List<ComponentPlan>();

        if (!string.IsNullOrWhiteSpace(game.ExtraBundle))
        {
            plans.Add(new ComponentPlan
            {
                Kind = ComponentKind.ExtraBundle,
                Source = game.ExtraBundle,
                Destination = "bundle-root",
                Metadata = new Dictionary<string, string> { ["alias"] = game.ExtraBundle }
            });
        }

        if (!string.IsNullOrWhiteSpace(game.SpecialK))
        {
            var specialKDestination = ResolveSpecialKDestination(game.SpecialK, proxy.FinalDllName);
            var invalidPluginsDestination = string.IsNullOrWhiteSpace(specialKDestination);
            plans.Add(new ComponentPlan
            {
                Kind = ComponentKind.SpecialK,
                Source = "SpecialK64.dll",
                Destination = specialKDestination,
                Skipped = invalidPluginsDestination,
                Reason = invalidPluginsDestination ? "invalid_specialk_plugins_final_dll" : ""
            });
        }

        if (!string.IsNullOrWhiteSpace(game.ReframeworkUrl))
        {
            plans.Add(new ComponentPlan
            {
                Kind = ComponentKind.Reframework,
                Source = "dinput8.dll",
                Destination = game.ReframeworkUrl
            });
        }

        if (game.Unreal5)
        {
            var dxgiExists = snapshot.Contains("dxgi.dll");
            plans.Add(new ComponentPlan
            {
                Kind = ComponentKind.Unreal5,
                Source = "Unreal5",
                Destination = "dxgi.dll",
                Skipped = dxgiExists,
                Reason = dxgiExists ? "root_dxgi_exists" : ""
            });
        }

        if (archiveState.ShouldInstallFsr4)
        {
            plans.Add(new ComponentPlan
            {
                Kind = ComponentKind.Fsr4,
                Source = "FSR4",
                Destination = "target-root"
            });
        }

        return plans;
    }

    private static string ResolveSpecialKDestination(string specialKValue, string finalDllName)
    {
        if (OptiScalerInstallLayout.IsPluginsToken(specialKValue))
        {
            if (string.IsNullOrWhiteSpace(finalDllName)
                || finalDllName.Contains('/')
                || finalDllName.Contains('\\')
                || !finalDllName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            {
                return "";
            }

            return OptiScalerInstallLayout.PluginFile(finalDllName);
        }

        return specialKValue;
    }

}


