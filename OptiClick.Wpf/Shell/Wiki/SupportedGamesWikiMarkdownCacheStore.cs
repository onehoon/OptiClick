using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using OptiClick.Infrastructure.FileSystem;
using OptiClick.Wpf.Logging;

namespace OptiClick.Wpf.Shell.Wiki;

public interface ISupportedGamesWikiMarkdownCacheStore
{
    string? TryReadContent();
    void TryWriteContent(string content);
    bool HasEntriesCache();
    bool HasReadableCache();
    SupportedGamesWikiEntriesCacheDocument? TryReadEntriesDocument();
    void TryWriteEntriesDocument(SupportedGamesWikiEntriesCacheDocument document);
    SupportedGamesWikiCacheMetadata? TryReadMetadata();
    void TryWriteMetadata(SupportedGamesWikiCacheMetadata metadata);
}

public sealed class SupportedGamesWikiMarkdownCacheStore : ISupportedGamesWikiMarkdownCacheStore
{
    private const string LegacyMarkdownCacheFileName = "supported_game_list_wiki_cache.md";
    private const string CacheDirectoryName = "SupportedGamesWiki";
    private const string MarkdownCacheFileName = "supported_games.md";
    private const string EntriesCacheFileName = "supported_games_entries.json";
    private const string MetadataCacheFileName = "supported_games_meta.json";
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = true
    };

    private readonly string _cacheDirectory;
    private readonly string _markdownPath;
    private readonly string _legacyMarkdownPath;
    private readonly string _entriesPath;
    private readonly string _metadataPath;
    private readonly IAppLogger _logger;

    public SupportedGamesWikiMarkdownCacheStore(
        IAppLocalDataPathProvider pathProvider,
        IAppLogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(pathProvider);

        _cacheDirectory = Path.Combine(pathProvider.RootDirectory, CacheDirectoryName);
        Directory.CreateDirectory(_cacheDirectory);
        _markdownPath = Path.Combine(_cacheDirectory, MarkdownCacheFileName);
        _legacyMarkdownPath = Path.Combine(pathProvider.ManifestDirectory, LegacyMarkdownCacheFileName);
        _entriesPath = Path.Combine(_cacheDirectory, EntriesCacheFileName);
        _metadataPath = Path.Combine(_cacheDirectory, MetadataCacheFileName);
        _logger = logger ?? NullAppLogger.Instance;
    }

    public string? TryReadContent()
    {
        return TryReadText(_markdownPath, "markdown")
               ?? TryReadText(_legacyMarkdownPath, "legacy_markdown");
    }

    public void TryWriteContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return;
        }

        try
        {
            AtomicFileWriter.WriteAllTextAtomic(_markdownPath, content);
        }
        catch (Exception ex)
        {
            _logger.Warning(
                "wiki-games",
                $"supported games wiki markdown cache write failed file={Path.GetFileName(_markdownPath)} type={ex.GetType().Name}");
        }
    }

    public bool HasEntriesCache()
    {
        return File.Exists(_entriesPath);
    }

    public bool HasReadableCache()
    {
        return File.Exists(_entriesPath)
               || File.Exists(_markdownPath)
               || File.Exists(_legacyMarkdownPath);
    }

    public SupportedGamesWikiEntriesCacheDocument? TryReadEntriesDocument()
    {
        var document = TryReadJson<SupportedGamesWikiEntriesCacheDocument>(_entriesPath, "entries");
        if (document is null)
        {
            return null;
        }

        return document with
        {
            Entries = (document.Entries ?? [])
                .Where(static entry => entry is not null)
                .ToArray()
        };
    }

    public void TryWriteEntriesDocument(SupportedGamesWikiEntriesCacheDocument document)
    {
        if (document.Entries.Count == 0)
        {
            return;
        }

        TryWriteJson(_entriesPath, document, "entries");
    }

    public SupportedGamesWikiCacheMetadata? TryReadMetadata()
    {
        return TryReadJson<SupportedGamesWikiCacheMetadata>(_metadataPath, "metadata");
    }

    public void TryWriteMetadata(SupportedGamesWikiCacheMetadata metadata)
    {
        TryWriteJson(_metadataPath, metadata, "metadata");
    }

    private string? TryReadText(string path, string cacheKind)
    {
        try
        {
            if (!File.Exists(path))
            {
                return null;
            }

            var content = File.ReadAllText(path);
            return string.IsNullOrWhiteSpace(content) ? null : content;
        }
        catch (Exception ex)
        {
            _logger.Warning(
                "wiki-games",
                $"supported games wiki {cacheKind} cache read failed file={Path.GetFileName(path)} type={ex.GetType().Name}");
            return null;
        }
    }

    private T? TryReadJson<T>(string path, string cacheKind)
        where T : class
    {
        try
        {
            if (!File.Exists(path))
            {
                return null;
            }

            var content = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(content))
            {
                return null;
            }

            return JsonSerializer.Deserialize<T>(content, SerializerOptions);
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            _logger.Warning(
                "wiki-games",
                $"supported games wiki {cacheKind} cache read failed file={Path.GetFileName(path)} type={ex.GetType().Name}");
            return null;
        }
    }

    private void TryWriteJson<T>(string path, T value, string cacheKind)
    {
        try
        {
            var content = JsonSerializer.Serialize(value, SerializerOptions);
            AtomicFileWriter.WriteAllTextAtomic(path, content);
        }
        catch (Exception ex)
        {
            _logger.Warning(
                "wiki-games",
                $"supported games wiki {cacheKind} cache write failed file={Path.GetFileName(path)} type={ex.GetType().Name}");
        }
    }
}

public sealed record SupportedGamesWikiCacheMetadata
{
    public string SourceUrl { get; init; } = "";
    [JsonPropertyName("etag")]
    public string ETag { get; init; } = "";
    public string LastModified { get; init; } = "";
    public string CachedAt { get; init; } = "";
}

public sealed record SupportedGamesWikiEntriesCacheDocument
{
    public int Version { get; init; } = 1;
    public string SourceUrl { get; init; } = "";
    [JsonPropertyName("source_etag")]
    public string SourceETag { get; init; } = "";
    public string SourceLastModified { get; init; } = "";
    public string GeneratedAt { get; init; } = "";
    public IReadOnlyList<SupportedGamesWikiEntry> Entries { get; init; } = [];
}
