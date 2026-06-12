namespace OptiClick.Core.Install;

public sealed record ModuleDownloadLinkCatalog
{
    private static readonly IReadOnlyDictionary<string, object?> EmptyRawEntries =
        new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

    public static ModuleDownloadLinkCatalog Empty { get; } = new();

    public IReadOnlyDictionary<string, object?> RawEntries { get; init; } = EmptyRawEntries;

    public IReadOnlyList<ModuleDownloadLinkCatalogEntry> Links { get; init; } = [];

    public bool HasEntries => RawEntries.Count > 0;

    public IReadOnlyList<string> Aliases => Links
        .Select(static link => link.Alias)
        .Where(static alias => !string.IsNullOrWhiteSpace(alias))
        .ToArray();

    public bool HasLinks => Links.Count > 0;

    public static implicit operator ModuleDownloadLinkCatalog(Dictionary<string, object?> entries)
    {
        return FromRaw(entries);
    }

    public static ModuleDownloadLinkCatalog FromRaw(IReadOnlyDictionary<string, object?>? entries)
    {
        if (entries is null || entries.Count == 0)
        {
            return Empty;
        }

        var rawEntries = new Dictionary<string, object?>(entries, StringComparer.OrdinalIgnoreCase);
        var links = rawEntries
            .Where(static pair => pair.Value is IReadOnlyDictionary<string, object?>)
            .Select(static pair => ModuleDownloadLinkCatalogEntry.FromRaw(
                pair.Key,
                (IReadOnlyDictionary<string, object?>)pair.Value!))
            .ToArray();

        return new ModuleDownloadLinkCatalog
        {
            RawEntries = rawEntries,
            Links = links
        };
    }

    public bool TryResolveLink(string alias, out ModuleDownloadLinkCatalogEntry link)
    {
        var normalizedAlias = ModuleDownloadLinkAliasPolicy.Normalize(alias);
        foreach (var candidate in Links)
        {
            if (string.Equals(candidate.Alias, alias, StringComparison.OrdinalIgnoreCase)
                || string.Equals(
                    ModuleDownloadLinkAliasPolicy.Normalize(candidate.Alias),
                    normalizedAlias,
                    StringComparison.OrdinalIgnoreCase))
            {
                link = candidate;
                return true;
            }
        }

        link = ModuleDownloadLinkCatalogEntry.Empty;
        return false;
    }

    public bool TryGetRawEntry(string key, out object? entry)
    {
        var normalizedKey = (key ?? "").Trim();
        if (string.IsNullOrWhiteSpace(normalizedKey))
        {
            entry = null;
            return false;
        }

        return RawEntries.TryGetValue(normalizedKey, out entry);
    }

    public bool IsExtraBundleReady(string extraBundleAlias)
    {
        var alias = (extraBundleAlias ?? "").Trim();
        if (string.IsNullOrWhiteSpace(alias))
        {
            return true;
        }

        return TryResolveLink(alias, out var link)
               && !string.IsNullOrWhiteSpace(link.Url);
    }
}

public static class ModuleDownloadLinkAliasPolicy
{
    public static string Normalize(string value)
    {
        return new string((value ?? "")
            .Where(static ch => char.IsLetterOrDigit(ch))
            .Select(static ch => char.ToLowerInvariant(ch))
            .ToArray());
    }
}

public sealed record ModuleDownloadLinkCatalogEntry
{
    private static readonly IReadOnlyDictionary<string, object?> EmptyRawValues =
        new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

    public static ModuleDownloadLinkCatalogEntry Empty { get; } = new();

    public string Alias { get; init; } = "";
    public string Url { get; init; } = "";
    public string Filename { get; init; } = "";
    public string Sha256 { get; init; } = "";
    public IReadOnlyDictionary<string, object?> RawValues { get; init; } = EmptyRawValues;

    public static ModuleDownloadLinkCatalogEntry FromRaw(
        string alias,
        IReadOnlyDictionary<string, object?> rawValues)
    {
        var values = rawValues ?? EmptyRawValues;
        return new ModuleDownloadLinkCatalogEntry
        {
            Alias = (alias ?? "").Trim(),
            Url = ReadFirstString(values, "url", "download_url", "source_url"),
            Filename = ReadFirstString(values, "filename", "file_name"),
            Sha256 = ReadFirstString(values, "sha256", "SHA256"),
            RawValues = values
        };
    }

    public string ReadString(string key)
    {
        if (!RawValues.TryGetValue(key, out var value) || value is null)
        {
            return "";
        }

        return value.ToString()?.Trim() ?? "";
    }

    public string ReadFirstString(params string[] keys)
    {
        foreach (var key in keys)
        {
            var text = ReadString(key);
            if (!string.IsNullOrWhiteSpace(text))
            {
                return text;
            }
        }

        return "";
    }

    private static string ReadFirstString(
        IReadOnlyDictionary<string, object?> values,
        params string[] keys)
    {
        foreach (var key in keys)
        {
            if (!values.TryGetValue(key, out var value) || value is null)
            {
                continue;
            }

            var text = value.ToString()?.Trim() ?? "";
            if (!string.IsNullOrWhiteSpace(text))
            {
                return text;
            }
        }

        return "";
    }
}
