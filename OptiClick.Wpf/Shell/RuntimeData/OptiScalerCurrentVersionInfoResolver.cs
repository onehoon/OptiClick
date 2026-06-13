using OptiClick.Core.Install;
using OptiClick.Wpf.Install.Archives;

namespace OptiClick.Wpf.Shell.RuntimeData;

internal sealed record OptiScalerCurrentVersionInfo
{
    public static readonly OptiScalerCurrentVersionInfo Empty = new();

    public string Variant { get; init; } = "";
    public string Version { get; init; } = "";
    public string DisplayVersion { get; init; } = "";
    public string FileVersion { get; init; } = "";
    public string ProductVersion { get; init; } = "";
}

internal static class OptiScalerCurrentVersionInfoResolver
{
    public static OptiScalerCurrentVersionInfo Resolve(
        ModuleDownloadLinkContext? moduleDownloadLinks,
        ArchiveReadinessSnapshot? archiveReadiness = null)
    {
        var linkInfo = ResolveFromModuleLinks(moduleDownloadLinks ?? ModuleDownloadLinkContext.Empty);
        var readiness = archiveReadiness ?? ArchiveReadinessSnapshot.NotReady;
        if (HasReadinessVersionInfo(readiness))
        {
            return new OptiScalerCurrentVersionInfo
            {
                Variant = PickFirst(readiness.OptiScalerVariant, linkInfo.Variant),
                Version = PickFirst(readiness.OptiScalerVersion, linkInfo.Version),
                DisplayVersion = PickFirst(readiness.OptiScalerDisplayVersion, linkInfo.DisplayVersion),
                FileVersion = PickFirst(readiness.OptiScalerFileVersion, linkInfo.FileVersion, readiness.OptiScalerVersion),
                ProductVersion = PickFirst(readiness.OptiScalerProductVersion, linkInfo.ProductVersion)
            };
        }

        return linkInfo;
    }

    private static OptiScalerCurrentVersionInfo ResolveFromModuleLinks(ModuleDownloadLinkContext moduleDownloadLinks)
    {
        if (!moduleDownloadLinks.TryResolveLink(ArchiveAssetRuntimeDataKeys.OptiScaler, out var entry))
        {
            return OptiScalerCurrentVersionInfo.Empty;
        }

        var version = entry.ReadFirstString("version", "current_version");
        return new OptiScalerCurrentVersionInfo
        {
            Variant = entry.ReadFirstString("variant", "channel"),
            Version = version,
            DisplayVersion = entry.ReadFirstString("display_version", "current_display_version", "version_label", "version"),
            FileVersion = entry.ReadFirstString("fileversion", "file_version", "FileVersion", "version", "current_version"),
            ProductVersion = entry.ReadFirstString("productversion", "product_version", "ProductVersion")
        };
    }

    private static bool HasReadinessVersionInfo(ArchiveReadinessSnapshot readiness)
    {
        return !string.IsNullOrWhiteSpace(readiness.OptiScalerVariant)
               || !string.IsNullOrWhiteSpace(readiness.OptiScalerVersion)
               || !string.IsNullOrWhiteSpace(readiness.OptiScalerDisplayVersion)
               || !string.IsNullOrWhiteSpace(readiness.OptiScalerFileVersion)
               || !string.IsNullOrWhiteSpace(readiness.OptiScalerProductVersion);
    }

    private static string PickFirst(params string[] values)
    {
        foreach (var value in values)
        {
            var normalized = (value ?? "").Trim();
            if (!string.IsNullOrWhiteSpace(normalized))
            {
                return normalized;
            }
        }

        return "";
    }
}
