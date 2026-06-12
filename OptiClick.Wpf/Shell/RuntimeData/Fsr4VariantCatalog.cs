using OptiClick.Wpf.Install.Archives;
using OptiClick.Wpf.Shell.Runtime;

namespace OptiClick.Wpf.Shell.RuntimeData;

public sealed record Fsr4VariantOption
{
    public string Variant { get; init; } = "";
    public string Version { get; init; } = "";
    public string DisplayVersion { get; init; } = "";
    public string Url { get; init; } = "";
    public string Filename { get; init; } = "";
    public string Sha256 { get; init; } = "";
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

public sealed record Fsr4VariantCatalog
{
    public static readonly Fsr4VariantCatalog Empty = new();

    public IReadOnlyList<Fsr4VariantOption> Options { get; init; } = [];

    public bool HasRuntimeVariants => Options.Count > 0;

    public Fsr4VariantOption? Find(string variant)
    {
        var normalized = Fsr4VariantCatalogBuilder.NormalizeVariant(variant);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        return Options.FirstOrDefault(option =>
            string.Equals(option.Variant, normalized, StringComparison.OrdinalIgnoreCase));
    }

    public bool HasVariant(string variant)
    {
        return Find(variant) is not null;
    }
}

public sealed record Fsr4VariantCatalogBuildResult
{
    public Fsr4VariantCatalog Catalog { get; init; } = Fsr4VariantCatalog.Empty;
    public IReadOnlyList<RuntimeFlowLogEntry> Logs { get; init; } = [];
}

public sealed class Fsr4VariantCatalogBuilder
{
    public const string VariantResourceKey = "fsr4_variants";

    public Fsr4VariantCatalogBuildResult Build(IReadOnlyList<RuntimeDataResourceRow>? rows)
    {
        var logs = new List<RuntimeFlowLogEntry>();
        if (rows is null || rows.Count == 0)
        {
            return new Fsr4VariantCatalogBuildResult();
        }

        var byVariant = new Dictionary<string, Fsr4VariantOption>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < rows.Count; index++)
        {
            var row = rows[index];
            var resourceId = RuntimeDataRowReader.GetString(row, "resource_id");
            var resourceGroup = RuntimeDataRowReader.GetString(row, "resource_group");
            if (!IsVariantRow(resourceId, resourceGroup))
            {
                continue;
            }

            var option = BuildVariantOption(row, index);
            if (string.IsNullOrWhiteSpace(option.Variant))
            {
                logs.Add(Warning("fsr4-variants", $"skipped variant row index={index} reason=missing_variant"));
                continue;
            }

            if (byVariant.ContainsKey(option.Variant))
            {
                logs.Add(Warning("fsr4-variants", $"duplicate variant={option.Variant} index={index} policy=last_wins"));
            }

            byVariant[option.Variant] = option;
        }

        return new Fsr4VariantCatalogBuildResult
        {
            Catalog = new Fsr4VariantCatalog
            {
                Options = byVariant.Values
                    .OrderBy(static option => option.SourceOrder)
                    .ThenBy(static option => option.Variant, StringComparer.OrdinalIgnoreCase)
                    .ToArray()
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

    private static Fsr4VariantOption BuildVariantOption(RuntimeDataResourceRow row, int sourceOrder)
    {
        var variant = NormalizeVariant(RuntimeDataRowReader.GetString(row, "variant"));
        return new Fsr4VariantOption
        {
            Variant = variant,
            Version = RuntimeDataRowReader.GetString(row, "version"),
            DisplayVersion = RuntimeDataRowReader.GetFirstString(row, "display_version", "current_display_version", "version_label", "version"),
            Url = RuntimeDataRowReader.GetFirstString(row, "url", "download_url", "source_url"),
            Filename = RuntimeDataRowReader.GetFirstString(row, "filename", "file_name"),
            Sha256 = RuntimeDataRowReader.GetFirstString(row, "sha256", "SHA256"),
            SourceOrder = sourceOrder
        };
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
