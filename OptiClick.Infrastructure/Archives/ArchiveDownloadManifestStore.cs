using System.IO;
using System.Text.Json;
using OptiClick.Infrastructure.FileSystem;

namespace OptiClick.Infrastructure.Archives;

public sealed record ArchiveManifestEntry
{
    public string Version { get; init; } = "";
    public string Filename { get; init; } = "";
    public string CacheKind { get; init; } = "";
    public string CacheEntry { get; init; } = "";
    public string UpdatedAtUtc { get; init; } = "";
}

public sealed class ArchiveDownloadManifestStore
{
    private const string ManifestFileName = "cache_manifest.json";
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };
    private readonly string _manifestPath;

    public ArchiveDownloadManifestStore(string manifestRoot)
    {
        var pathProvider = new AppLocalDataPathProvider();
        var root = string.IsNullOrWhiteSpace(manifestRoot)
            ? pathProvider.ManifestDirectory
            : manifestRoot;
        Directory.CreateDirectory(root);
        _manifestPath = Path.Combine(root, ManifestFileName);
    }

    public bool IsUpdateNeeded(string assetKey, string version)
    {
        if (string.IsNullOrWhiteSpace(assetKey) || string.IsNullOrWhiteSpace(version))
        {
            return true;
        }

        var document = TryReadDocument();
        if (document is null)
        {
            return true;
        }

        var key = assetKey.Trim();
        return !document.TryGetValue(key, out var entry)
               || !string.Equals((entry.Version ?? "").Trim(), version.Trim(), StringComparison.Ordinal);
    }

    public ArchiveManifestEntry? TryGetEntry(string assetKey)
    {
        if (string.IsNullOrWhiteSpace(assetKey))
        {
            return null;
        }

        var document = TryReadDocument();
        if (document is null)
        {
            return null;
        }

        var key = assetKey.Trim();
        return document.TryGetValue(key, out var entry) ? entry : null;
    }

    public void WriteEntry(string assetKey, string version, string filename = "", string cacheKind = "", string cacheEntry = "")
    {
        if (string.IsNullOrWhiteSpace(assetKey) || string.IsNullOrWhiteSpace(version))
        {
            return;
        }

        var document = TryReadDocument() ?? new Dictionary<string, ArchiveManifestEntry>(StringComparer.Ordinal);
        var key = assetKey.Trim();
        document[key] = new ArchiveManifestEntry
        {
            Version = version.Trim(),
            Filename = (filename ?? "").Trim(),
            CacheKind = (cacheKind ?? "").Trim(),
            CacheEntry = (cacheEntry ?? "").Trim(),
            UpdatedAtUtc = DateTime.UtcNow.ToString("O")
        };

        var json = JsonSerializer.Serialize(document, SerializerOptions);
        AtomicFileWriter.WriteAllTextAtomic(_manifestPath, json);
    }

    private Dictionary<string, ArchiveManifestEntry>? TryReadDocument()
    {
        if (!File.Exists(_manifestPath))
        {
            return null;
        }

        try
        {
            var json = File.ReadAllText(_manifestPath);
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            return JsonSerializer.Deserialize<Dictionary<string, ArchiveManifestEntry>>(json);
        }
        catch (JsonException)
        {
            TryMoveCorruptManifest();
            return null;
        }
        catch
        {
            return null;
        }
    }

    private void TryMoveCorruptManifest()
    {
        try
        {
            _ = AtomicFileWriter.MoveCorruptFile(_manifestPath);
        }
        catch
        {
            // Ignore preserve failure and keep safe fallback behavior.
        }
    }
}
