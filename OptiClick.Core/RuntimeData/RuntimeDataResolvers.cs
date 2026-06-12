using OptiClick.Core.Install;
using System.IO;
using System.Text.Json;
using System.Linq;

namespace OptiClick.Core.RuntimeData;

public sealed record RuntimeDataResourceResolution
{
    public bool Found { get; init; }
    public string Alias { get; init; } = "";
    public string ResourceId { get; init; } = "";
    public string ResourceGroup { get; init; } = "";
    public string Url { get; init; } = "";
    public string Filename { get; init; } = "";
    public string Version { get; init; } = "";
    public string DisplayVersion { get; init; } = "";
    public string Sha256 { get; init; } = "";
    public RuntimeDataResourceRow? Row { get; init; }
    public IReadOnlyDictionary<string, JsonElement> RawValues { get; init; } =
        new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
}

public interface IRuntimeDataResourceResolver
{
    RuntimeDataResourceResolution ResolveByAlias(RemoteRuntimeData runtimeData, string alias);
}

public sealed class RuntimeDataResourceResolver : IRuntimeDataResourceResolver
{
    public RuntimeDataResourceResolution ResolveByAlias(RemoteRuntimeData runtimeData, string alias)
    {
        var normalizedAlias = (alias ?? "").Trim().ToLowerInvariant();
        if (runtimeData is null || runtimeData.ResourceMaster.Count == 0 || string.IsNullOrWhiteSpace(normalizedAlias))
        {
            return new RuntimeDataResourceResolution();
        }

        foreach (var row in runtimeData.ResourceMaster)
        {
            var keys = BuildAliasKeys(row);
            if (!keys.Contains(normalizedAlias))
            {
                continue;
            }

            return new RuntimeDataResourceResolution
            {
                Found = true,
                Alias = normalizedAlias,
                ResourceId = RuntimeDataRowReader.GetString(row, "resource_id"),
                ResourceGroup = RuntimeDataRowReader.GetString(row, "resource_group"),
                Url = RuntimeDataRowReader.GetFirstString(row, "url", "download_url", "source_url"),
                Filename = RuntimeDataRowReader.GetString(row, "filename"),
                Version = RuntimeDataRowReader.GetString(row, "version"),
                DisplayVersion = RuntimeDataRowReader.GetString(row, "display_version"),
                Sha256 = RuntimeDataRowReader.GetString(row, "sha256"),
                Row = row,
                RawValues = row.Values
            };
        }

        return new RuntimeDataResourceResolution
        {
            Alias = normalizedAlias
        };
    }

    private static HashSet<string> BuildAliasKeys(RuntimeDataResourceRow row)
    {
        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void AddKey(string value)
        {
            var normalized = (value ?? "").Trim().ToLowerInvariant();
            if (!string.IsNullOrWhiteSpace(normalized))
            {
                keys.Add(normalized);
            }
        }

        var resourceGroup = RuntimeDataRowReader.GetString(row, "resource_group");
        var resourceId = RuntimeDataRowReader.GetString(row, "resource_id");
        AddKey(resourceGroup);
        AddKey(resourceId);

        if (string.Equals(resourceGroup, "extra_bundle", StringComparison.OrdinalIgnoreCase)
            || string.Equals(resourceId, "extra_bundle", StringComparison.OrdinalIgnoreCase))
        {
            AddKey(RuntimeDataRowReader.GetString(row, "name"));
            var filename = RuntimeDataRowReader.GetString(row, "filename");
            if (!string.IsNullOrWhiteSpace(filename))
            {
                AddKey(Path.GetFileNameWithoutExtension(filename));
            }
        }

        return keys;
    }
}

public sealed record RuntimeDataResolvedProfiles
{
    public RuntimeDataGameProfile? Game { get; init; }
    public IReadOnlyList<RuntimeDataProfileRow> GameIniProfile { get; init; } = [];
    public IReadOnlyList<RuntimeDataProfileRow> GameUnrealIniProfile { get; init; } = [];
    public IReadOnlyList<RuntimeDataProfileRow> GameXmlProfile { get; init; } = [];
    public IReadOnlyList<RuntimeDataProfileRow> GameJsonProfile { get; init; } = [];
    public IReadOnlyList<RuntimeDataProfileRow> EngineIniProfile { get; init; } = [];
    public IReadOnlyList<RuntimeDataProfileRow> RegistryProfile { get; init; } = [];
    public IReadOnlyList<RuntimeDataMessageRow> MessageBinding { get; init; } = [];
    public IReadOnlyList<RuntimeDataMessageRow> MessageCenter { get; init; } = [];
    public string ExcludeListRaw { get; init; } = "";
    public IReadOnlyList<string> ExcludeListPatterns { get; init; } = [];
}

public interface IRuntimeDataProfileResolver
{
    RuntimeDataResolvedProfiles Resolve(RemoteRuntimeData runtimeData, RuntimeDataGameProfile? game);
}

public sealed class RuntimeDataProfileResolver : IRuntimeDataProfileResolver
{
    private readonly RuntimeDataProfileCatalogBuilder _catalogBuilder = new();
    private readonly RuntimeDataProfileCatalogAttacher _catalogAttacher = new();

    public RuntimeDataResolvedProfiles Resolve(RemoteRuntimeData runtimeData, RuntimeDataGameProfile? game)
    {
        if (runtimeData is null)
        {
            return new RuntimeDataResolvedProfiles();
        }

        var excludeRaw = (game?.ExcludeListRaw ?? "").Trim();
        var catalogs = _catalogBuilder.Build(runtimeData);
        var attachedRows = _catalogAttacher.Attach(catalogs, game?.GpuProfileId);

        return new RuntimeDataResolvedProfiles
        {
            Game = game,
            GameIniProfile = attachedRows.GameIniProfileRows.OfType<RuntimeDataProfileRow>().ToArray(),
            GameUnrealIniProfile = attachedRows.GameUnrealIniProfileRows.OfType<RuntimeDataProfileRow>().ToArray(),
            GameXmlProfile = attachedRows.GameXmlProfileRows.OfType<RuntimeDataProfileRow>().ToArray(),
            GameJsonProfile = attachedRows.GameJsonProfileRows.OfType<RuntimeDataProfileRow>().ToArray(),
            EngineIniProfile = attachedRows.EngineIniProfileRows.OfType<RuntimeDataProfileRow>().ToArray(),
            RegistryProfile = attachedRows.RegistryProfileRows.OfType<RuntimeDataProfileRow>().ToArray(),
            MessageBinding = runtimeData.MessageBinding,
            MessageCenter = runtimeData.MessageCenter,
            ExcludeListRaw = excludeRaw,
            ExcludeListPatterns = ParseExcludeListPatterns(excludeRaw)
        };
    }

    public static IReadOnlyList<string> ParseExcludeListPatterns(string raw)
    {
        return InstallExcludeListPatternParser.Parse(raw);
    }
}
