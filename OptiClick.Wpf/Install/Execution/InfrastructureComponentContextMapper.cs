using InfrastructureComponents = OptiClick.Infrastructure.Install.Components;

namespace OptiClick.Wpf.Install.Execution;

internal static class InfrastructureComponentContextMapper
{
    public static InfrastructureComponents.OptiPatcherInstallContext ToOptiPatcherContext(ComponentInstallContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return new InfrastructureComponents.OptiPatcherInstallContext
        {
            TargetPath = context.TargetPath,
            UseOptiPatcher = context.ShouldInstallOptiPatcher,
            ModuleDownloadLinks = context.ModuleDownloadLinks,
            OptiPatcherCachedArchivePath = context.OptiPatcherCachedArchivePath
        };
    }

    public static InfrastructureComponents.ReFrameworkInstallContext ToReFrameworkContext(ComponentInstallContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return new InfrastructureComponents.ReFrameworkInstallContext
        {
            TargetPath = context.TargetPath,
            ReFrameworkDestination = context.ReFrameworkDestination,
            ModuleDownloadLinks = context.ModuleDownloadLinks,
            ReFrameworkCachedArchivePath = context.ReFrameworkCachedArchivePath
        };
    }

    public static InfrastructureComponents.SpecialKInstallContext ToSpecialKContext(ComponentInstallContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return new InfrastructureComponents.SpecialKInstallContext
        {
            TargetPath = context.TargetPath,
            FinalDllName = context.FinalDllName,
            SpecialKValue = context.SpecialKValue,
            ModuleDownloadLinks = context.ModuleDownloadLinks,
            SpecialKCachedArchivePath = context.SpecialKCachedArchivePath
        };
    }

    public static InfrastructureComponents.UltimateAsiLoaderInstallContext ToUltimateAsiLoaderContext(ComponentInstallContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return new InfrastructureComponents.UltimateAsiLoaderInstallContext
        {
            TargetPath = context.TargetPath,
            UseUltimateAsiLoader = context.ShouldInstallUltimateAsiLoader,
            UalDetectedNames = context.UalDetectedNames,
            ModuleDownloadLinks = context.ModuleDownloadLinks,
            UalCachedArchivePath = context.UalCachedArchivePath
        };
    }

    public static InfrastructureComponents.ExtraBundleInstallContext ToExtraBundleContext(ComponentInstallContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return new InfrastructureComponents.ExtraBundleInstallContext
        {
            TargetPath = context.TargetPath,
            ExtraBundleAlias = context.ExtraBundleAlias,
            ModuleDownloadLinks = context.ModuleDownloadLinks
        };
    }

    public static InfrastructureComponents.Unreal5InstallContext ToUnreal5Context(ComponentInstallContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return new InfrastructureComponents.Unreal5InstallContext
        {
            TargetPath = context.TargetPath,
            UseUnreal5 = context.ShouldInstallUnreal5,
            ModuleDownloadLinks = context.ModuleDownloadLinks,
            Unreal5CachedArchivePath = context.Unreal5CachedArchivePath
        };
    }

    public static InfrastructureComponents.Fsr4InstallContext ToFsr4Context(ComponentInstallContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return new InfrastructureComponents.Fsr4InstallContext
        {
            TargetPath = context.TargetPath,
            UseFsr4 = context.ShouldInstallFsr4,
            Fsr4Variant = context.Fsr4Variant,
            Fsr4SourceArchivePath = context.Fsr4SourceArchive,
            GpuVendor = context.GpuVendor,
            GpuName = context.GpuName,
            GpuBundleKey = context.GpuBundleKey,
            GpuGroup = context.GpuGroup
        };
    }
}
