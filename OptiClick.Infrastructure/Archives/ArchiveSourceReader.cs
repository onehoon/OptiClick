using System.IO;
using System.Net.Http;
using OptiClick.Infrastructure.FileSystem;

namespace OptiClick.Infrastructure.Archives;

public interface IArchiveSourceFileSystem
{
    bool FileExists(string path);
    bool DirectoryExists(string path);
    void CreateDirectory(string path);
    void DeleteFile(string path);
    IEnumerable<string> EnumerateFiles(string directoryPath, string searchPattern, SearchOption searchOption);
}

public interface IArchiveSourceDownloader
{
    Task<ArchiveDownloadResult> DownloadAsync(
        string url,
        string destinationPath,
        TimeSpan timeout,
        CancellationToken cancellationToken = default,
        string fallbackSha256 = "");
}

public interface IArchiveSourceExtractor
{
    Task<ArchiveExtractionResult> ExtractAsync(
        string archivePath,
        string destinationPath,
        CancellationToken cancellationToken = default);
}

public sealed record ArchiveSourceReaderOptions
{
    public string InstallExecutionTempRoot { get; init; } =
        new AppLocalDataPathProvider().InstallExecutionTempDirectory;
}

public sealed class ArchiveSourceReader
{
    private readonly IArchiveSourceFileSystem _fileSystem;
    private readonly IArchiveSourceDownloader _downloader;
    private readonly IArchiveSourceExtractor _extractor;
    private readonly IArchiveFileVerifier _fileVerifier;
    private readonly TimeSpan _downloadTimeout;
    private readonly string _ownedTempRoot;

    public ArchiveSourceReader(
        IArchiveSourceFileSystem fileSystem,
        IArchiveSourceDownloader downloader,
        IArchiveSourceExtractor extractor,
        TimeSpan? downloadTimeout = null,
        ArchiveSourceReaderOptions? options = null,
        IArchiveFileVerifier? fileVerifier = null)
    {
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        _downloader = downloader ?? throw new ArgumentNullException(nameof(downloader));
        _extractor = extractor ?? throw new ArgumentNullException(nameof(extractor));
        _fileVerifier = fileVerifier ?? new ArchiveFileVerifier(new HttpClient());
        _downloadTimeout = downloadTimeout ?? TimeSpan.FromSeconds(60);
        _ownedTempRoot = ResolveOwnedTempRoot(options);
    }

    public async Task<string> ResolveSourcePathAsync(
        string url,
        string cachedArchivePath,
        string downloadFilename,
        CancellationToken cancellationToken = default,
        string fallbackSha256 = "")
    {
        var cached = (cachedArchivePath ?? "").Trim();
        if (!string.IsNullOrWhiteSpace(cached) && _fileSystem.DirectoryExists(cached))
        {
            return Path.GetFullPath(cached);
        }

        if (!string.IsNullOrWhiteSpace(cached) && _fileSystem.FileExists(cached))
        {
            var verification = await _fileVerifier.VerifyArchiveFileAsync(cached, url, fallbackSha256, cancellationToken);
            if (verification.IsSuccess)
            {
                return Path.GetFullPath(cached);
            }

            TryDeleteCachedFile(cached);
        }

        if (string.IsNullOrWhiteSpace(url))
        {
            return "";
        }

        var scope = TemporaryExtractionScope.Create(_ownedTempRoot, "source");
        var fileName = ResolveDownloadFileName(url, downloadFilename, "download.bin");
        var destination = Path.Combine(scope.Path, fileName);
        var download = await _downloader.DownloadAsync(url, destination, _downloadTimeout, cancellationToken, fallbackSha256);
        if (!download.IsSuccess)
        {
            scope.Dispose();
            return "";
        }

        return Path.GetFullPath(destination);
    }

    public async Task<IReadOnlyList<string>> FindFilesAsync(
        string sourcePath,
        Func<string, bool> predicate,
        CancellationToken cancellationToken = default)
    {
        var normalized = (sourcePath ?? "").Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return Array.Empty<string>();
        }

        if (_fileSystem.DirectoryExists(normalized))
        {
            return _fileSystem
                .EnumerateFiles(normalized, "*", SearchOption.AllDirectories)
                .Where(predicate)
                .ToArray();
        }

        if (!_fileSystem.FileExists(normalized))
        {
            return Array.Empty<string>();
        }

        var extension = Path.GetExtension(normalized).ToLowerInvariant();
        if (!string.Equals(extension, ".zip", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(extension, ".7z", StringComparison.OrdinalIgnoreCase))
        {
            return predicate(normalized) ? [normalized] : Array.Empty<string>();
        }

        var scope = TemporaryExtractionScope.Create(_ownedTempRoot, "extract");
        var extractRoot = Path.Combine(scope.Path, "payload");
        _fileSystem.CreateDirectory(extractRoot);
        var extract = await _extractor.ExtractAsync(normalized, extractRoot, cancellationToken);
        if (!extract.IsSuccess)
        {
            scope.Dispose();
            return Array.Empty<string>();
        }

        var matches = _fileSystem
            .EnumerateFiles(extractRoot, "*", SearchOption.AllDirectories)
            .Where(predicate)
            .ToArray();
        if (matches.Length == 0)
        {
            scope.Dispose();
        }

        return matches;
    }

    public bool CleanupTemporaryPath(string path)
    {
        var normalized = (path ?? "").Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return false;
        }

        if (!TryGetOwnedRootFirstSegment(_ownedTempRoot, normalized, out var firstSegment))
        {
            return false;
        }

        var ownedRoot = Path.Combine(_ownedTempRoot, firstSegment);
        try
        {
            if (Directory.Exists(ownedRoot))
            {
                Directory.Delete(ownedRoot, recursive: true);
                return true;
            }
        }
        catch
        {
            // Ignore cleanup failure.
        }

        return false;
    }

    private static string ResolveDownloadFileName(string url, string requestedFileName, string fallback)
    {
        var requested = Path.GetFileName((requestedFileName ?? "").Trim());
        if (!string.IsNullOrWhiteSpace(requested))
        {
            return requested;
        }

        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            var parsed = Path.GetFileName(Uri.UnescapeDataString(uri.AbsolutePath));
            if (!string.IsNullOrWhiteSpace(parsed))
            {
                return parsed;
            }
        }

        return fallback;
    }

    private void TryDeleteCachedFile(string path)
    {
        try
        {
            if (_fileSystem.FileExists(path))
            {
                _fileSystem.DeleteFile(path);
            }
        }
        catch
        {
            // Ignore cleanup failure.
        }
    }

    private static string ResolveOwnedTempRoot(ArchiveSourceReaderOptions? options)
    {
        var configured = (options?.InstallExecutionTempRoot ?? "").Trim();
        if (string.IsNullOrWhiteSpace(configured))
        {
            configured = new AppLocalDataPathProvider().InstallExecutionTempDirectory;
        }

        return Path.GetFullPath(configured);
    }

    private static bool TryGetOwnedRootFirstSegment(string root, string path, out string firstSegment)
    {
        firstSegment = "";

        string fullPath;
        try
        {
            fullPath = Path.GetFullPath(path);
        }
        catch
        {
            return false;
        }

        if (!IsUnderRoot(root, fullPath))
        {
            return false;
        }

        var relative = Path.GetRelativePath(root, fullPath);
        if (relative.StartsWith("..", StringComparison.Ordinal))
        {
            return false;
        }

        var segment = relative
            .Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault();
        if (string.IsNullOrWhiteSpace(segment))
        {
            return false;
        }

        firstSegment = segment;
        return true;
    }

    private static bool IsUnderRoot(string root, string path)
    {
        var fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;
        var fullPath = Path.GetFullPath(path);
        return fullPath.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class TemporaryExtractionScope : IDisposable
    {
        private readonly bool _ownsPath;
        private bool _disposed;

        public TemporaryExtractionScope(string path, bool ownsPath = true)
        {
            Path = System.IO.Path.GetFullPath(path ?? "");
            _ownsPath = ownsPath;
        }

        public string Path { get; }

        public static TemporaryExtractionScope Create(string ownedTempRoot, string prefix)
        {
            var root = System.IO.Path.Combine(ownedTempRoot, $"{prefix}_{Guid.NewGuid():N}");
            Directory.CreateDirectory(root);
            return new TemporaryExtractionScope(root, ownsPath: true);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            if (!_ownsPath)
            {
                return;
            }

            try
            {
                if (Directory.Exists(Path))
                {
                    Directory.Delete(Path, recursive: true);
                }
            }
            catch
            {
                // Ignore cleanup failure.
            }
        }
    }
}
