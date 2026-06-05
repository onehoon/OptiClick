using OptiClick.Wpf.Shell.RuntimeData;

namespace OptiClick.Wpf.Install.Archives;

public sealed record ArchiveReadinessFlowRequest
{
    public IReadOnlyDictionary<string, object?> ModuleDownloadLinks { get; init; } =
        new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
    public OptiScalerVariantCatalog OptiScalerVariantCatalog { get; init; } =
        OptiScalerVariantCatalog.Empty;
    public string PreferredOptiScalerVariant { get; init; } = OptiScalerVariantCatalogBuilder.StableVariant;
}
