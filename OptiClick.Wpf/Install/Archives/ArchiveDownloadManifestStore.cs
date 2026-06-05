namespace OptiClick.Wpf.Install.Archives;

public sealed record ArchiveManifestEntry
{
    public string Version { get; init; } = "";
    public string Filename { get; init; } = "";
    public string CacheKind { get; init; } = "";
    public string CacheEntry { get; init; } = "";
    public string UpdatedAtUtc { get; init; } = "";
    public IReadOnlyDictionary<string, ArchiveManifestEntry> Versions { get; init; } =
        new Dictionary<string, ArchiveManifestEntry>(StringComparer.Ordinal);

    public static ArchiveManifestEntry FromInfrastructure(OptiClick.Infrastructure.Archives.ArchiveManifestEntry entry)
    {
        return new ArchiveManifestEntry
        {
            Version = entry.Version,
            Filename = entry.Filename,
            CacheKind = entry.CacheKind,
            CacheEntry = entry.CacheEntry,
            UpdatedAtUtc = entry.UpdatedAtUtc,
            Versions = (entry.Versions ?? new Dictionary<string, OptiClick.Infrastructure.Archives.ArchiveManifestVersionEntry>(StringComparer.Ordinal)).ToDictionary(
                static pair => pair.Key,
                static pair => FromInfrastructureVersion(pair.Value),
                StringComparer.Ordinal)
        };
    }

    private static ArchiveManifestEntry FromInfrastructureVersion(
        OptiClick.Infrastructure.Archives.ArchiveManifestVersionEntry entry)
    {
        return new ArchiveManifestEntry
        {
            Version = entry.Version,
            Filename = entry.Filename,
            CacheKind = entry.CacheKind,
            CacheEntry = entry.CacheEntry,
            UpdatedAtUtc = entry.UpdatedAtUtc
        };
    }
}

public interface IArchiveDownloadManifestStore
{
    bool IsUpdateNeeded(string assetKey, string version);
    ArchiveManifestEntry? TryGetEntry(string assetKey);
    ArchiveManifestEntry? TryGetVersionEntry(string assetKey, string version);
    void WriteEntry(string assetKey, string version, string filename = "", string cacheKind = "", string cacheEntry = "");
    void WriteVersionEntry(string assetKey, string version, string filename = "", string cacheKind = "", string cacheEntry = "");
    void PruneVersionEntriesByCacheEntry(string assetKey, IEnumerable<string> cacheEntriesToKeep);
}

public sealed class ArchiveDownloadManifestStore : IArchiveDownloadManifestStore
{
    private readonly OptiClick.Infrastructure.Archives.ArchiveDownloadManifestStore _inner;

    public ArchiveDownloadManifestStore(string manifestRoot)
        : this(new OptiClick.Infrastructure.Archives.ArchiveDownloadManifestStore(manifestRoot))
    {
    }

    internal ArchiveDownloadManifestStore(OptiClick.Infrastructure.Archives.ArchiveDownloadManifestStore inner)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    public bool IsUpdateNeeded(string assetKey, string version)
    {
        return _inner.IsUpdateNeeded(assetKey, version);
    }

    public ArchiveManifestEntry? TryGetEntry(string assetKey)
    {
        // Compatibility wrapper: WPF contract stays stable while Infrastructure owns manifest IO implementation.
        var entry = _inner.TryGetEntry(assetKey);
        return entry is null ? null : ArchiveManifestEntry.FromInfrastructure(entry);
    }

    public ArchiveManifestEntry? TryGetVersionEntry(string assetKey, string version)
    {
        var entry = _inner.TryGetVersionEntry(assetKey, version);
        return entry is null ? null : ArchiveManifestEntry.FromInfrastructure(entry);
    }

    public void WriteEntry(string assetKey, string version, string filename = "", string cacheKind = "", string cacheEntry = "")
    {
        _inner.WriteEntry(assetKey, version, filename, cacheKind, cacheEntry);
    }

    public void WriteVersionEntry(string assetKey, string version, string filename = "", string cacheKind = "", string cacheEntry = "")
    {
        _inner.WriteVersionEntry(assetKey, version, filename, cacheKind, cacheEntry);
    }

    public void PruneVersionEntriesByCacheEntry(string assetKey, IEnumerable<string> cacheEntriesToKeep)
    {
        _inner.PruneVersionEntriesByCacheEntry(assetKey, cacheEntriesToKeep);
    }
}
