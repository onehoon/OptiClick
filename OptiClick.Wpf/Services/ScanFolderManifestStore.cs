using System.IO;
using System.Text.Json;
using OptiClick.Infrastructure.FileSystem;
using OptiClick.Wpf.Logging;

namespace OptiClick.Wpf.Services;

public sealed class ScanFolderManifest
{
    public int Version { get; init; } = 2;
    public List<ScanFolderManifestEntry> Folders { get; init; } = [];
    public List<ScanFolderManifestEntry> AddedFolders { get; init; } = [];
}

public sealed class ScanFolderManifestEntry
{
    public string Path { get; init; } = "";
    public bool IsChecked { get; init; } = true;
    public bool IsDefault { get; init; }
    public DateTimeOffset AddedAt { get; init; }
}

public interface IScanFolderManifestStore
{
    IReadOnlyList<ScanFolderManifestEntry> Load();
    void Save(IReadOnlyList<ScanFolderManifestEntry> folders);
}

public sealed class ScanFolderManifestStore : IScanFolderManifestStore
{
    private const string ManifestFileName = "scan_folders.json";
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };
    private readonly string _manifestPath;
    private readonly IAppLogger _logger;

    public ScanFolderManifestStore(
        IAppLocalDataPathProvider? pathProvider = null,
        IAppLogger? logger = null)
    {
        var provider = pathProvider ?? new AppLocalDataPathProvider();
        Directory.CreateDirectory(provider.ManifestDirectory);
        _manifestPath = Path.Combine(provider.ManifestDirectory, ManifestFileName);
        _logger = logger ?? NullAppLogger.Instance;
    }

    public IReadOnlyList<ScanFolderManifestEntry> Load()
    {
        if (!File.Exists(_manifestPath))
        {
            return [];
        }

        try
        {
            var json = File.ReadAllText(_manifestPath);
            if (string.IsNullOrWhiteSpace(json))
            {
                return [];
            }

            var document = JsonSerializer.Deserialize<ScanFolderManifest>(json);
            var folders = ResolveStoredFolders(document);
            if (folders.Count == 0)
            {
                return [];
            }

            var deduped = new List<ScanFolderManifestEntry>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in folders)
            {
                var normalizedPath = NormalizePath(entry?.Path);
                if (string.IsNullOrWhiteSpace(normalizedPath) || seen.Contains(normalizedPath))
                {
                    continue;
                }

                seen.Add(normalizedPath);
                deduped.Add(new ScanFolderManifestEntry
                {
                    Path = normalizedPath,
                    IsChecked = entry?.IsChecked ?? true,
                    IsDefault = entry?.IsDefault == true,
                    AddedAt = entry?.AddedAt ?? DateTimeOffset.MinValue
                });
            }

            return deduped;
        }
        catch (JsonException ex)
        {
            var movedPath = MoveCorruptManifestFile();
            _logger.Warning(
                "Scan",
                $"scan folder manifest load failed file={Path.GetFileName(_manifestPath)} type={ex.GetType().Name} moved={Path.GetFileName(movedPath)}");
            return [];
        }
        catch (Exception ex)
        {
            _logger.Warning(
                "Scan",
                $"scan folder manifest load failed file={Path.GetFileName(_manifestPath)} type={ex.GetType().Name}");
            return [];
        }
    }

    public void Save(IReadOnlyList<ScanFolderManifestEntry> folders)
    {
        try
        {
            var entries = new List<ScanFolderManifestEntry>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var folder in folders ?? [])
            {
                var normalizedPath = NormalizePath(folder.Path);
                if (string.IsNullOrWhiteSpace(normalizedPath) || seen.Contains(normalizedPath))
                {
                    continue;
                }

                seen.Add(normalizedPath);
                entries.Add(new ScanFolderManifestEntry
                {
                    Path = normalizedPath,
                    IsChecked = folder.IsChecked,
                    IsDefault = folder.IsDefault,
                    AddedAt = folder.AddedAt == default ? DateTimeOffset.Now : folder.AddedAt
                });
            }

            var document = new ScanFolderManifest
            {
                Version = 2,
                Folders = entries
            };

            var json = JsonSerializer.Serialize(document, SerializerOptions);
            AtomicFileWriter.WriteAllTextAtomic(_manifestPath, json);
        }
        catch (Exception ex)
        {
            _logger.Warning(
                "Scan",
                $"scan folder manifest save failed file={Path.GetFileName(_manifestPath)} type={ex.GetType().Name}");
        }
    }

    private static IReadOnlyList<ScanFolderManifestEntry> ResolveStoredFolders(ScanFolderManifest? document)
    {
        if (document is null)
        {
            return [];
        }

        if (document.Folders.Count > 0)
        {
            return document.Folders;
        }

        return document.AddedFolders;
    }

    private string MoveCorruptManifestFile()
    {
        try
        {
            return AtomicFileWriter.MoveCorruptFile(_manifestPath);
        }
        catch
        {
            return "";
        }
    }

    private static string NormalizePath(string? path)
    {
        var trimmed = (path ?? "").Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return "";
        }

        try
        {
            return Path
                .GetFullPath(trimmed)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
        catch
        {
            return trimmed;
        }
    }
}
