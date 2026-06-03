using OptiClick.Wpf.Shell.Games;
using OptiClick.Core.Runtime;
using OptiClick.Wpf.Install.Planning;

namespace OptiClick.Wpf.Install.Execution;

public sealed class ComponentInstallContextBuilder
{
    public ComponentInstallContext Build(ComponentInstallContextBuildInput input)
    {
        var fsr4Required = ShellGameCardMapper.ResolveFsr4Required(input.SelectedGame);
        var selectedGpu = ResolveSelectedGpu(input.LatestRuntimeContext);
        var bundleKey = ShellGameInstallMetadataResolver.GetGpuBundleKey(input.SelectedGame);
        var gpuGroup = ShellGameInstallMetadataResolver.GetGpuGroup(input.SelectedGame);
        return new ComponentInstallContext
        {
            Game = input.SelectedGame,
            TargetPath = InstallTargetPathNormalizer.NormalizeTargetDirectory(input.Plan.TargetFolder),
            FinalDllName = input.Plan.FinalProxyDllName,
            OptiScalerPayloadDirectory = input.LatestArchiveReadiness.OptiScalerSourceArchive,
            GpuVendor = (selectedGpu?.Vendor ?? "").Trim(),
            GpuName = (selectedGpu?.Name ?? "").Trim(),
            GpuBundleKey = bundleKey,
            GpuGroup = gpuGroup,
            Fsr4SourceArchive = input.LatestArchiveReadiness.Fsr4SourceArchive,
            Fsr4Required = fsr4Required,
            UseUltimateAsiLoader = ShellGameInstallMetadataResolver.GetUltimateAsiLoader(input.SelectedGame),
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
}
