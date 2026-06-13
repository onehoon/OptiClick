using OptiClick.Wpf.Install.Archives;
using OptiClick.Wpf.Shell.Runtime;

namespace OptiClick.Wpf.Shell.RuntimeData;

public sealed record OptiScalerVariantOption
{
    public string Variant { get; init; } = "";
    public string Version { get; init; } = "";
    public string DisplayVersion { get; init; } = "";
    public string FileVersion { get; init; } = "";
    public string ProductVersion { get; init; } = "";
    public string Url { get; init; } = "";
    public string Filename { get; init; } = "";
    public string Sha256 { get; init; } = "";
    public string DisplayLabel { get; init; } = "";
    public bool IsChannel { get; init; }
    public int SortOrder { get; init; }
    public int SourceOrder { get; init; }

    public RemoteArchiveEntry ToRemoteArchiveEntry()
    {
        return new RemoteArchiveEntry
        {
            Url = Url,
            Filename = Filename,
            Version = Version,
            Sha256 = Sha256
        };
    }
}

public sealed record OptiScalerVariantCatalog
{
    public static readonly OptiScalerVariantCatalog Empty = new();

    public IReadOnlyList<OptiScalerVariantOption> Options { get; init; } = [];
    public OptiScalerVariantOption? CanonicalFallback { get; init; }

    public bool HasRuntimeVariants => Options.Count > 0;

    public OptiScalerVariantOption? Find(string variant)
    {
        var normalized = OptiScalerVariantCatalogBuilder.NormalizeVariant(variant);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        return Options.FirstOrDefault(option =>
            string.Equals(option.Variant, normalized, StringComparison.OrdinalIgnoreCase));
    }
}

public sealed record OptiScalerVariantCatalogBuildResult
{
    public OptiScalerVariantCatalog Catalog { get; init; } = OptiScalerVariantCatalog.Empty;
    public IReadOnlyList<RuntimeFlowLogEntry> Logs { get; init; } = [];
}

public sealed class OptiScalerVariantCatalogBuilder
{
    public const string VariantResourceKey = "optiscaler_variants";
    public const string CanonicalResourceKey = "optiscaler";
    public const string StableVariant = "stable";
    public const string PreviewVariant = "preview";

    public OptiScalerVariantCatalogBuildResult Build(IReadOnlyList<RuntimeDataResourceRow>? rows)
    {
        var logs = new List<RuntimeFlowLogEntry>();
        if (rows is null || rows.Count == 0)
        {
            return new OptiScalerVariantCatalogBuildResult();
        }

        var byVariant = new Dictionary<string, OptiScalerVariantOption>(StringComparer.OrdinalIgnoreCase);
        OptiScalerVariantOption? canonicalFallback = null;

        for (var index = 0; index < rows.Count; index++)
        {
            var row = rows[index];
            var resourceId = RuntimeDataRowReader.GetString(row, "resource_id");
            var resourceGroup = RuntimeDataRowReader.GetString(row, "resource_group");

            if (IsVariantRow(resourceId, resourceGroup))
            {
                var option = BuildVariantOption(row, index);
                if (string.IsNullOrWhiteSpace(option.Variant))
                {
                    logs.Add(Warning("optiscaler-variants", $"skipped variant row index={index} reason=missing_variant"));
                    continue;
                }

                if (byVariant.ContainsKey(option.Variant))
                {
                    logs.Add(Warning("optiscaler-variants", $"duplicate variant={option.Variant} index={index} policy=last_wins"));
                }

                byVariant[option.Variant] = option;
                continue;
            }

            if (canonicalFallback is null && IsCanonicalOptiScalerRow(resourceId, resourceGroup))
            {
                canonicalFallback = BuildCanonicalFallbackOption(row, index);
            }
        }

        var options = byVariant.Values
            .OrderBy(static option => option.SortOrder)
            .ThenBy(static option => option.SourceOrder)
            .ToArray();

        return new OptiScalerVariantCatalogBuildResult
        {
            Catalog = new OptiScalerVariantCatalog
            {
                Options = options,
                CanonicalFallback = canonicalFallback
            },
            Logs = logs
        };
    }

    public static string NormalizeVariant(string? variant)
    {
        return (variant ?? "").Trim().ToLowerInvariant();
    }

    private static bool IsVariantRow(string resourceId, string resourceGroup)
    {
        return string.Equals(resourceId, VariantResourceKey, StringComparison.OrdinalIgnoreCase)
               || string.Equals(resourceGroup, VariantResourceKey, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsCanonicalOptiScalerRow(string resourceId, string resourceGroup)
    {
        return string.Equals(resourceId, CanonicalResourceKey, StringComparison.OrdinalIgnoreCase)
               || string.Equals(resourceGroup, CanonicalResourceKey, StringComparison.OrdinalIgnoreCase);
    }

    private static OptiScalerVariantOption BuildVariantOption(RuntimeDataResourceRow row, int sourceOrder)
    {
        var variant = NormalizeVariant(RuntimeDataRowReader.GetString(row, "variant"));
        return BuildOption(row, variant, sourceOrder);
    }

    private static OptiScalerVariantOption BuildCanonicalFallbackOption(RuntimeDataResourceRow row, int sourceOrder)
    {
        return BuildOption(row, StableVariant, sourceOrder);
    }

    private static OptiScalerVariantOption BuildOption(
        RuntimeDataResourceRow row,
        string variant,
        int sourceOrder)
    {
        var version = RuntimeDataRowReader.GetString(row, "version");
        var displayVersion = RuntimeDataRowReader.GetFirstString(row, "display_version", "current_display_version", "version_label", "version");
        var fileVersion = RuntimeDataRowReader.GetFirstString(row, "fileversion", "file_version", "FileVersion", "version");
        var productVersion = RuntimeDataRowReader.GetFirstString(row, "productversion", "product_version", "ProductVersion");
        var url = RuntimeDataRowReader.GetFirstString(row, "url", "download_url", "source_url");
        var filename = RuntimeDataRowReader.GetFirstString(row, "filename", "file_name");
        var sha256 = RuntimeDataRowReader.GetFirstString(row, "sha256", "SHA256");
        var labelVersion = string.IsNullOrWhiteSpace(displayVersion) ? version : displayVersion;

        return new OptiScalerVariantOption
        {
            Variant = variant,
            Version = version,
            DisplayVersion = displayVersion,
            FileVersion = fileVersion,
            ProductVersion = productVersion,
            Url = url,
            Filename = filename,
            Sha256 = sha256,
            DisplayLabel = BuildDisplayLabel(variant, labelVersion),
            IsChannel = IsChannelVariant(variant),
            SortOrder = ResolveSortOrder(variant),
            SourceOrder = sourceOrder
        };
    }

    private static string BuildDisplayLabel(string variant, string version)
    {
        if (string.Equals(variant, StableVariant, StringComparison.OrdinalIgnoreCase))
        {
            return string.IsNullOrWhiteSpace(version) ? "Stable" : $"Stable ({version.Trim()})";
        }

        if (string.Equals(variant, PreviewVariant, StringComparison.OrdinalIgnoreCase))
        {
            return string.IsNullOrWhiteSpace(version) ? "Preview" : $"Preview ({version.Trim()})";
        }

        return variant;
    }

    private static bool IsChannelVariant(string variant)
    {
        return string.Equals(variant, StableVariant, StringComparison.OrdinalIgnoreCase)
               || string.Equals(variant, PreviewVariant, StringComparison.OrdinalIgnoreCase);
    }

    private static int ResolveSortOrder(string variant)
    {
        if (string.Equals(variant, StableVariant, StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        if (string.Equals(variant, PreviewVariant, StringComparison.OrdinalIgnoreCase))
        {
            return 10;
        }

        return 100;
    }

    private static RuntimeFlowLogEntry Warning(string category, string message)
    {
        return new RuntimeFlowLogEntry
        {
            Level = "warning",
            Category = category,
            Message = message
        };
    }
}
