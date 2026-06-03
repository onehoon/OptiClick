namespace OptiClick.Wpf.Install.Archives;

public sealed record ArchiveManifestEntry
{
    public string Version { get; init; } = "";
    public string Filename { get; init; } = "";
    public string CacheKind { get; init; } = "";
    public string CacheEntry { get; init; } = "";
    public string UpdatedAtUtc { get; init; } = "";

    public static ArchiveManifestEntry FromInfrastructure(OptiClick.Infrastructure.Archives.ArchiveManifestEntry entry)
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
    void WriteEntry(string assetKey, string version, string filename = "", string cacheKind = "", string cacheEntry = "");
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

    public void WriteEntry(string assetKey, string version, string filename = "", string cacheKind = "", string cacheEntry = "")
    {
        _inner.WriteEntry(assetKey, version, filename, cacheKind, cacheEntry);
    }
}
