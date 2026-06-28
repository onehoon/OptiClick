using OptiClick.Core.Install.Planning;

namespace OptiClick.Wpf.Install.Execution;

internal static class PlannedComponentInstallerMapper
{
    public static IReadOnlyList<ComponentInstallName> ResolveEnabledInstallers(IReadOnlyList<CoreInstallPlanComponent> components)
    {
        if (components.Count == 0)
        {
            return Array.Empty<ComponentInstallName>();
        }

        var planned = new List<ComponentInstallName>();
        foreach (var component in components.Where(static component => component.Enabled))
        {
            if (!TryMapToInstaller(component.Type, out var installerComponent)
                || planned.Contains(installerComponent))
            {
                continue;
            }

            planned.Add(installerComponent);
        }

        return planned;
    }

    public static bool TryMapToInstaller(CoreInstallPlanComponentType type, out ComponentInstallName component)
    {
        switch (type)
        {
            case CoreInstallPlanComponentType.OptiScalerCore:
                component = ComponentInstallName.OptiScalerCore;
                return true;
            case CoreInstallPlanComponentType.REFramework:
                component = ComponentInstallName.ReFramework;
                return true;
            case CoreInstallPlanComponentType.SpecialK:
                component = ComponentInstallName.SpecialK;
                return true;
            case CoreInstallPlanComponentType.Unreal5:
                component = ComponentInstallName.Unreal5;
                return true;
            case CoreInstallPlanComponentType.ExtraBundle:
                component = ComponentInstallName.ExtraBundle;
                return true;
            case CoreInstallPlanComponentType.RtssProfile:
                component = default;
                return false;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, $"Unsupported plan component type: {type}");
        }
    }
}
