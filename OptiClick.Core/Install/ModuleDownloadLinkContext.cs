namespace OptiClick.Core.Install;

public sealed record ModuleDownloadLinkContext
{
    public ModuleDownloadLinkCatalog Catalog { get; init; } = ModuleDownloadLinkCatalog.Empty;

    public IReadOnlyDictionary<string, object?> Entries => Catalog.RawEntries;

    public static ModuleDownloadLinkContext Empty { get; } = new();

    public static ModuleDownloadLinkContext FromEntries(IReadOnlyDictionary<string, object?>? entries)
    {
        return FromCatalog(ModuleDownloadLinkCatalog.FromRaw(entries));
    }

    public static ModuleDownloadLinkContext FromCatalog(ModuleDownloadLinkCatalog? catalog)
    {
        return catalog is null || ReferenceEquals(catalog, ModuleDownloadLinkCatalog.Empty)
            ? Empty
            : new ModuleDownloadLinkContext { Catalog = catalog };
    }

    public bool TryResolveEntry(string alias, out IReadOnlyDictionary<string, object?> entry)
    {
        if (TryResolveLink(alias, out var link))
        {
            entry = link.RawValues;
            return true;
        }

        entry = ModuleDownloadLinkCatalogEntry.Empty.RawValues;
        return false;
    }

    public bool TryResolveLink(string alias, out ModuleDownloadLinkEntry link)
    {
        if (Catalog.TryResolveLink(alias, out var catalogEntry))
        {
            link = ModuleDownloadLinkEntry.FromCatalogEntry(catalogEntry);
            return true;
        }

        link = ModuleDownloadLinkEntry.Empty;
        return false;
    }

    public bool IsExtraBundleReady(string extraBundleAlias)
    {
        var alias = (extraBundleAlias ?? "").Trim();
        if (string.IsNullOrWhiteSpace(alias))
        {
            return true;
        }

        return Catalog.IsExtraBundleReady(alias);
    }
}

public sealed record ModuleDownloadLinkEntry
{
    private static readonly IReadOnlyDictionary<string, object?> EmptyRawValues =
        new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

    public static ModuleDownloadLinkEntry Empty { get; } = new();

    public string Alias { get; init; } = "";
    public string Url { get; init; } = "";
    public string Filename { get; init; } = "";
    public string Sha256 { get; init; } = "";
    public IReadOnlyDictionary<string, object?> RawValues { get; init; } = EmptyRawValues;

    public static ModuleDownloadLinkEntry FromRaw(
        string alias,
        IReadOnlyDictionary<string, object?> rawValues)
    {
        return FromCatalogEntry(ModuleDownloadLinkCatalogEntry.FromRaw(alias, rawValues));
    }

    public static ModuleDownloadLinkEntry FromCatalogEntry(ModuleDownloadLinkCatalogEntry entry)
    {
        var safeEntry = entry ?? ModuleDownloadLinkCatalogEntry.Empty;
        return new ModuleDownloadLinkEntry
        {
            Alias = safeEntry.Alias,
            Url = safeEntry.Url,
            Filename = safeEntry.Filename,
            Sha256 = safeEntry.Sha256,
            RawValues = safeEntry.RawValues
        };
    }

    public string ReadFirstString(params string[] keys)
    {
        foreach (var key in keys)
        {
            if (!RawValues.TryGetValue(key, out var value) || value is null)
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
