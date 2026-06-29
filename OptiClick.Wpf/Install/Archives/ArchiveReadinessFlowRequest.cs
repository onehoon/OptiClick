using OptiClick.Wpf.Shell.RuntimeData;

namespace OptiClick.Wpf.Install.Archives;

public sealed record ArchiveReadinessFlowRequest
{
    public ModuleDownloadLinkContext ModuleDownloadLinks { get; init; } =
        ModuleDownloadLinkContext.Empty;
    public OptiScalerVariantCatalog OptiScalerVariantCatalog { get; init; } =
        OptiScalerVariantCatalog.Empty;
    public string PreferredOptiScalerVariant { get; init; } = OptiScalerVariantCatalogBuilder.StableVariant;
    public string GpuBundleKey { get; init; } = "";
}
