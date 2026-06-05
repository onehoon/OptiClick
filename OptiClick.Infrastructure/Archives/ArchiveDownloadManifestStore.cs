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
    public Dictionary<string, ArchiveManifestVersionEntry> Versions { get; init; } = new(StringComparer.Ordinal);
}

public sealed record ArchiveManifestVersionEntry
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

    public ArchiveManifestEntry? TryGetVersionEntry(string assetKey, string version)
    {
        if (string.IsNullOrWhiteSpace(assetKey) || string.IsNullOrWhiteSpace(version))
        {
            return null;
        }

        var document = TryReadDocument();
        if (document is null)
        {
            return null;
        }

        var key = assetKey.Trim();
        var normalizedVersion = version.Trim();
        if (!document.TryGetValue(key, out var entry))
        {
            return null;
        }

        var versionEntries = entry.Versions ?? new Dictionary<string, ArchiveManifestVersionEntry>(StringComparer.Ordinal);
        if (versionEntries.TryGetValue(normalizedVersion, out var versionEntry))
        {
            return FromVersionEntry(versionEntry, versionEntries);
        }

        return string.Equals((entry.Version ?? "").Trim(), normalizedVersion, StringComparison.Ordinal)
            ? entry
            : null;
    }

    public void WriteEntry(string assetKey, string version, string filename = "", string cacheKind = "", string cacheEntry = "")
    {
        WriteVersionEntry(assetKey, version, filename, cacheKind, cacheEntry);
    }

    public void WriteVersionEntry(string assetKey, string version, string filename = "", string cacheKind = "", string cacheEntry = "")
    {
        if (string.IsNullOrWhiteSpace(assetKey) || string.IsNullOrWhiteSpace(version))
        {
            return;
        }

        var document = TryReadDocument() ?? new Dictionary<string, ArchiveManifestEntry>(StringComparer.Ordinal);
        var key = assetKey.Trim();
        var normalizedVersion = version.Trim();
        var updatedAtUtc = DateTime.UtcNow.ToString("O");
        var versions = document.TryGetValue(key, out var existing)
            ? new Dictionary<string, ArchiveManifestVersionEntry>(
                existing.Versions ?? new Dictionary<string, ArchiveManifestVersionEntry>(StringComparer.Ordinal),
                StringComparer.Ordinal)
            : new Dictionary<string, ArchiveManifestVersionEntry>(StringComparer.Ordinal);
        versions[normalizedVersion] = new ArchiveManifestVersionEntry
        {
            Version = normalizedVersion,
            Filename = (filename ?? "").Trim(),
            CacheKind = (cacheKind ?? "").Trim(),
            CacheEntry = (cacheEntry ?? "").Trim(),
            UpdatedAtUtc = updatedAtUtc
        };

        document[key] = new ArchiveManifestEntry
        {
            Version = normalizedVersion,
            Filename = (filename ?? "").Trim(),
            CacheKind = (cacheKind ?? "").Trim(),
            CacheEntry = (cacheEntry ?? "").Trim(),
            UpdatedAtUtc = updatedAtUtc,
            Versions = versions
        };

        var json = JsonSerializer.Serialize(document, SerializerOptions);
        AtomicFileWriter.WriteAllTextAtomic(_manifestPath, json);
    }

    public void PruneVersionEntriesByCacheEntry(string assetKey, IEnumerable<string> cacheEntriesToKeep)
    {
        if (string.IsNullOrWhiteSpace(assetKey))
        {
            return;
        }

        var keep = cacheEntriesToKeep
            .Where(static entry => !string.IsNullOrWhiteSpace(entry))
            .Select(static entry => entry.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var document = TryReadDocument();
        if (document is null || keep.Count == 0)
        {
            return;
        }

        var key = assetKey.Trim();
        if (!document.TryGetValue(key, out var entry))
        {
            return;
        }

        var existingVersions = entry.Versions ?? new Dictionary<string, ArchiveManifestVersionEntry>(StringComparer.Ordinal);
        var versions = existingVersions
            .Where(pair => keep.Contains((pair.Value.CacheEntry ?? "").Trim()))
            .ToDictionary(static pair => pair.Key, static pair => pair.Value, StringComparer.Ordinal);
        if (versions.Count == existingVersions.Count
            && keep.Contains((entry.CacheEntry ?? "").Trim()))
        {
            return;
        }

        if (versions.Count == 0)
        {
            document.Remove(key);
        }
        else if (keep.Contains((entry.CacheEntry ?? "").Trim()))
        {
            document[key] = entry with { Versions = versions };
        }
        else
        {
            var nextCurrent = versions.Values
                .OrderByDescending(static versionEntry => ParseUpdatedAtUtc(versionEntry.UpdatedAtUtc))
                .First();
            document[key] = FromVersionEntry(nextCurrent, versions);
        }

        var json = JsonSerializer.Serialize(document, SerializerOptions);
        AtomicFileWriter.WriteAllTextAtomic(_manifestPath, json);
    }

    private static ArchiveManifestEntry FromVersionEntry(
        ArchiveManifestVersionEntry versionEntry,
        Dictionary<string, ArchiveManifestVersionEntry> versions)
    {
        return new ArchiveManifestEntry
        {
            Version = versionEntry.Version,
            Filename = versionEntry.Filename,
            CacheKind = versionEntry.CacheKind,
            CacheEntry = versionEntry.CacheEntry,
            UpdatedAtUtc = versionEntry.UpdatedAtUtc,
            Versions = versions
        };
    }

    private static DateTimeOffset ParseUpdatedAtUtc(string value)
    {
        return DateTimeOffset.TryParse(value, out var parsed) ? parsed : DateTimeOffset.MinValue;
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
