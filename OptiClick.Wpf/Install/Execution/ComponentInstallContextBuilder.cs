using OptiClick.Core.Install;
using OptiClick.Core.Install.Planning;
using OptiClick.Core.Runtime;
using OptiClick.Wpf.Install.Planning;

namespace OptiClick.Wpf.Install.Execution;

public sealed class ComponentInstallContextBuilder
{
    public ComponentInstallContext Build(ComponentInstallContextBuildInput input)
    {
        var descriptor = input.ExecutionDescriptor;
        var selectedGpu = ResolveSelectedGpu(input.LatestRuntimeContext);
        var plannedComponentInstallers = PlannedComponentInstallerMapper.ResolveEnabledInstallers(input.Plan.Components);
        return new ComponentInstallContext
        {
            ExecutionDescriptor = descriptor,
            TargetPath = InstallTargetPathNormalizer.NormalizeTargetDirectory(input.Plan.TargetFolder),
            FinalDllName = input.Plan.FinalProxyDllName,
            OptiScalerPayloadDirectory = input.LatestArchiveReadiness.OptiScalerSourceArchive,
            OptiScalerVariant = input.LatestArchiveReadiness.OptiScalerVariant,
            OptiScalerVersion = input.LatestArchiveReadiness.OptiScalerVersion,
            OptiScalerDisplayVersion = input.LatestArchiveReadiness.OptiScalerDisplayVersion,
            GpuVendor = (selectedGpu?.Vendor ?? "").Trim(),
            GpuName = (selectedGpu?.Name ?? "").Trim(),
            Fsr4SourceArchive = input.LatestArchiveReadiness.ResolveFsr4VariantSourceArchive(descriptor.Fsr4Variant),
            Fsr4Variant = descriptor.Fsr4Variant,
            UalDetectedNames = input.UalDetectedNames,
            HasPlannedComponentInstallers = input.Plan.Components.Count > 0,
            PlannedComponentInstallers = plannedComponentInstallers,
            ShouldInstallOptiPatcher = false,
            ShouldInstallUltimateAsiLoader = ResolveShouldInstall(
                input.Plan.Components,
                CoreInstallPlanComponentType.UltimateAsiLoader,
                descriptor.RequiresUltimateAsiLoader),
            ShouldInstallUnreal5 = ResolveShouldInstall(
                input.Plan.Components,
                CoreInstallPlanComponentType.Unreal5,
                descriptor.RequiresUnreal5),
            ShouldInstallFsr4 = ResolveShouldInstall(
                input.Plan.Components,
                CoreInstallPlanComponentType.Fsr4,
                descriptor.ShouldInstallFsr4),
            GpuBundleKey = descriptor.GpuBundleKey,
            GpuGroup = descriptor.GpuGroup,
            ReFrameworkDestination = ResolveComponentDestination(
                input.Plan.Components,
                CoreInstallPlanComponentType.REFramework,
                descriptor.ReFrameworkDestination),
            SpecialKValue = ResolveComponentDestination(
                input.Plan.Components,
                CoreInstallPlanComponentType.SpecialK,
                descriptor.SpecialK),
            ExtraBundleAlias = ResolveExtraBundleAlias(input.Plan.Components, descriptor),
            UalCachedArchivePath = input.LatestArchiveReadiness.UalSourceArchive,
            OptiPatcherCachedArchivePath = input.LatestArchiveReadiness.OptiPatcherSourceArchive,
            SpecialKCachedArchivePath = input.LatestArchiveReadiness.SpecialKSourceArchive,
            ReFrameworkCachedArchivePath = input.LatestArchiveReadiness.ReframeworkSourceArchive,
            Unreal5CachedArchivePath = input.LatestArchiveReadiness.Unreal5SourceArchive,
            ModuleDownloadLinks = input.ModuleDownloadLinks
        };
    }

    private static GpuInfo? ResolveSelectedGpu(RuntimeContext runtimeContext)
    {
        if (runtimeContext.SelectedGpu is not null)
        {
            return runtimeContext.SelectedGpu;
        }

        var gpus = runtimeContext.Gpus ?? [];
        if (gpus.Count == 0)
        {
            return null;
        }

        return gpus.FirstOrDefault(static gpu => gpu.IsPrimary) ?? gpus[0];
    }

    private static bool ResolveShouldInstall(
        IReadOnlyList<CoreInstallPlanComponent> components,
        CoreInstallPlanComponentType type,
        bool fallback)
    {
        var planned = components.FirstOrDefault(component => component.Type == type);
        return planned is null ? fallback : planned.Enabled;
    }

    private static string ResolveComponentDestination(
        IReadOnlyList<CoreInstallPlanComponent> components,
        CoreInstallPlanComponentType type,
        string fallback)
    {
        var planned = components.FirstOrDefault(component =>
            component.Enabled
            && component.Type == type);
        var destination = (planned?.DestinationHint ?? "").Trim();
        if (!string.IsNullOrWhiteSpace(destination))
        {
            return destination;
        }

        return fallback;
    }

    private static string ResolveExtraBundleAlias(
        IReadOnlyList<CoreInstallPlanComponent> components,
        InstallExecutionDescriptor descriptor)
    {
        var planned = components.FirstOrDefault(static component =>
            component.Enabled
            && component.Type == CoreInstallPlanComponentType.ExtraBundle);
        var alias = (planned?.RequiredArchiveAlias ?? "").Trim();
        if (!string.IsNullOrWhiteSpace(alias))
        {
            return alias;
        }

        return descriptor.ExtraBundle;
    }
}
