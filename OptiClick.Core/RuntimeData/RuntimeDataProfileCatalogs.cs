namespace OptiClick.Core.RuntimeData;

public sealed class RuntimeDataProfileCatalogs
{
    public static readonly RuntimeDataProfileCatalogs Empty = new();

    public IReadOnlyDictionary<string, IReadOnlyList<RuntimeDataRawRow>> GameIniProfile { get; init; } =
        new Dictionary<string, IReadOnlyList<RuntimeDataRawRow>>(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, IReadOnlyList<RuntimeDataRawRow>> GameUnrealIniProfile { get; init; } =
        new Dictionary<string, IReadOnlyList<RuntimeDataRawRow>>(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, IReadOnlyList<RuntimeDataRawRow>> EngineIniProfile { get; init; } =
        new Dictionary<string, IReadOnlyList<RuntimeDataRawRow>>(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, IReadOnlyList<RuntimeDataRawRow>> GameXmlProfile { get; init; } =
        new Dictionary<string, IReadOnlyList<RuntimeDataRawRow>>(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, IReadOnlyList<RuntimeDataRawRow>> RegistryProfile { get; init; } =
        new Dictionary<string, IReadOnlyList<RuntimeDataRawRow>>(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, IReadOnlyList<RuntimeDataRawRow>> GameJsonProfile { get; init; } =
        new Dictionary<string, IReadOnlyList<RuntimeDataRawRow>>(StringComparer.OrdinalIgnoreCase);
}

public static class RuntimeDataProfileJoinKey
{
    public static string NormalizeProfileId(string? value)
    {
        return (value ?? "").Trim().ToLowerInvariant();
    }

    public static string DeriveAllProfileId(string profileId)
    {
        var normalized = NormalizeProfileId(profileId);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return "";
        }

        var separator = normalized.IndexOf('_');
        if (separator <= 0)
        {
            return "";
        }

        var gameId = normalized[..separator];
        if (string.IsNullOrWhiteSpace(gameId))
        {
            return "";
        }

        var allId = $"{gameId}_all";
        if (string.Equals(allId, normalized, StringComparison.Ordinal))
        {
            return "";
        }

        return allId;
    }
}

public sealed class RuntimeDataProfileCatalogBuilder
{
    public RuntimeDataProfileCatalogs Build(RemoteRuntimeData runtimeData)
    {
        if (runtimeData is null)
        {
            return RuntimeDataProfileCatalogs.Empty;
        }

        return new RuntimeDataProfileCatalogs
        {
            GameIniProfile = BuildProfileIndex(runtimeData.GameIniProfile),
            GameUnrealIniProfile = BuildProfileIndex(runtimeData.GameUnrealIniProfile),
            EngineIniProfile = BuildProfileIndex(runtimeData.EngineIniProfile),
            GameXmlProfile = BuildProfileIndex(runtimeData.GameXmlProfile),
            RegistryProfile = BuildProfileIndex(runtimeData.RegistryProfile),
            GameJsonProfile = BuildProfileIndex(runtimeData.GameJsonProfile)
        };
    }

    private static IReadOnlyDictionary<string, IReadOnlyList<RuntimeDataRawRow>> BuildProfileIndex(
        IReadOnlyList<RuntimeDataProfileRow> rows)
    {
        var indexed = new Dictionary<string, List<RuntimeDataRawRow>>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows ?? [])
        {
            if (row?.Values is null)
            {
                continue;
            }

            var profileId = RuntimeDataProfileJoinKey.NormalizeProfileId(RuntimeDataRowReader.GetString(row, "profile_id"));
            if (string.IsNullOrWhiteSpace(profileId))
            {
                continue;
            }

            if (!indexed.TryGetValue(profileId, out var list))
            {
                list = new List<RuntimeDataRawRow>();
                indexed[profileId] = list;
            }

            list.Add(row);
        }

        return indexed.ToDictionary(
            static pair => pair.Key,
            static pair => (IReadOnlyList<RuntimeDataRawRow>)pair.Value,
            StringComparer.OrdinalIgnoreCase);
    }
}

public sealed class AttachedRuntimeProfileRows
{
    public static readonly AttachedRuntimeProfileRows Empty = new();

    public IReadOnlyList<RuntimeDataRawRow> GameIniProfileRows { get; init; } = [];
    public IReadOnlyList<RuntimeDataRawRow> GameUnrealIniProfileRows { get; init; } = [];
    public IReadOnlyList<RuntimeDataRawRow> EngineIniProfileRows { get; init; } = [];
    public IReadOnlyList<RuntimeDataRawRow> GameXmlProfileRows { get; init; } = [];
    public IReadOnlyList<RuntimeDataRawRow> RegistryProfileRows { get; init; } = [];
    public IReadOnlyList<RuntimeDataRawRow> GameJsonProfileRows { get; init; } = [];
}

public sealed class RuntimeDataProfileCatalogAttacher
{
    public AttachedRuntimeProfileRows Attach(RuntimeDataProfileCatalogs catalogs, string? profileId)
    {
        var normalizedProfileId = RuntimeDataProfileJoinKey.NormalizeProfileId(profileId);
        if (string.IsNullOrWhiteSpace(normalizedProfileId))
        {
            return AttachedRuntimeProfileRows.Empty;
        }

        catalogs ??= RuntimeDataProfileCatalogs.Empty;
        return new AttachedRuntimeProfileRows
        {
            GameIniProfileRows = GetProfileRows(catalogs.GameIniProfile, normalizedProfileId),
            GameUnrealIniProfileRows = GetProfileRows(catalogs.GameUnrealIniProfile, normalizedProfileId),
            EngineIniProfileRows = GetProfileRows(catalogs.EngineIniProfile, normalizedProfileId),
            GameXmlProfileRows = GetProfileRows(catalogs.GameXmlProfile, normalizedProfileId),
            RegistryProfileRows = GetProfileRows(catalogs.RegistryProfile, normalizedProfileId),
            GameJsonProfileRows = GetProfileRows(catalogs.GameJsonProfile, normalizedProfileId)
        };
    }

    private static IReadOnlyList<RuntimeDataRawRow> GetProfileRows(
        IReadOnlyDictionary<string, IReadOnlyList<RuntimeDataRawRow>> sectionCatalog,
        string normalizedProfileId)
    {
        var rows = new List<RuntimeDataRawRow>();
        var allProfileId = RuntimeDataProfileJoinKey.DeriveAllProfileId(normalizedProfileId);
        if (!string.IsNullOrWhiteSpace(allProfileId)
            && sectionCatalog.TryGetValue(allProfileId, out var allRows))
        {
            rows.AddRange(allRows);
        }

        if (sectionCatalog.TryGetValue(normalizedProfileId, out var specificRows))
        {
            rows.AddRange(specificRows);
        }

        return rows;
    }
}
