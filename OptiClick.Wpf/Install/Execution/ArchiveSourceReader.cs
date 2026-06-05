using System.IO;
using OptiClick.Wpf.Install.Archives;
using OptiClick.Wpf.Install.FileSystem;

namespace OptiClick.Wpf.Install.Execution;

public interface IArchiveSourceReader
{
    Task<string> ResolveSourcePathAsync(
        string url,
        string cachedArchivePath,
        string downloadFilename,
        CancellationToken cancellationToken = default,
        string fallbackSha256 = "");

    Task<IReadOnlyList<string>> FindFilesAsync(
        string sourcePath,
        Func<string, bool> predicate,
        CancellationToken cancellationToken = default);

    bool CleanupTemporaryPath(string path);
}

public sealed class ArchiveSourceReader : IArchiveSourceReader
{
    private readonly OptiClick.Infrastructure.Archives.ArchiveSourceReader _inner;

    public ArchiveSourceReader(
        IInstallFileSystem fileSystem,
        IArchiveDownloader downloader,
        IArchiveExtractor extractor,
        TimeSpan? downloadTimeout = null,
        OptiClick.Infrastructure.Archives.ArchiveSourceReaderOptions? options = null)
    {
        _inner = new OptiClick.Infrastructure.Archives.ArchiveSourceReader(
            new InstallFileSystemAdapter(fileSystem),
            new ArchiveDownloaderAdapter(downloader),
            new ArchiveExtractorAdapter(extractor),
            downloadTimeout,
            options);
    }

    internal ArchiveSourceReader(OptiClick.Infrastructure.Archives.ArchiveSourceReader inner)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    public async Task<string> ResolveSourcePathAsync(
        string url,
        string cachedArchivePath,
        string downloadFilename,
        CancellationToken cancellationToken = default,
        string fallbackSha256 = "")
    {
        return await _inner.ResolveSourcePathAsync(url, cachedArchivePath, downloadFilename, cancellationToken, fallbackSha256);
    }

    public async Task<IReadOnlyList<string>> FindFilesAsync(
        string sourcePath,
        Func<string, bool> predicate,
        CancellationToken cancellationToken = default)
    {
        return await _inner.FindFilesAsync(sourcePath, predicate, cancellationToken);
    }

    public bool CleanupTemporaryPath(string path)
    {
        return _inner.CleanupTemporaryPath(path);
    }

    private sealed class InstallFileSystemAdapter : OptiClick.Infrastructure.Archives.IArchiveSourceFileSystem
    {
        private readonly IInstallFileSystem _inner;

        public InstallFileSystemAdapter(IInstallFileSystem inner)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        public bool FileExists(string path) => _inner.FileExists(path);

        public bool DirectoryExists(string path) => _inner.DirectoryExists(path);

        public void CreateDirectory(string path) => _inner.CreateDirectory(path);

        public void DeleteFile(string path) => _inner.DeleteFile(path);

        public IEnumerable<string> EnumerateFiles(string directoryPath, string searchPattern, SearchOption searchOption) =>
            _inner.EnumerateFiles(directoryPath, searchPattern, searchOption);
    }

    private sealed class ArchiveDownloaderAdapter : OptiClick.Infrastructure.Archives.IArchiveSourceDownloader
    {
        private readonly IArchiveDownloader _inner;

        public ArchiveDownloaderAdapter(IArchiveDownloader inner)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        public async Task<OptiClick.Infrastructure.Archives.ArchiveDownloadResult> DownloadAsync(
            string url,
            string destinationPath,
            TimeSpan timeout,
            CancellationToken cancellationToken = default,
            string fallbackSha256 = "")
        {
            var result = await _inner.DownloadAsync(url, destinationPath, timeout, cancellationToken, fallbackSha256);
            return new OptiClick.Infrastructure.Archives.ArchiveDownloadResult
            {
                IsSuccess = result.IsSuccess,
                DestinationPath = result.DestinationPath,
                ErrorCode = result.ErrorCode,
                ErrorMessage = result.ErrorMessage
            };
        }
    }

    private sealed class ArchiveExtractorAdapter : OptiClick.Infrastructure.Archives.IArchiveSourceExtractor
    {
        private readonly IArchiveExtractor _inner;

        public ArchiveExtractorAdapter(IArchiveExtractor inner)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        public async Task<OptiClick.Infrastructure.Archives.ArchiveExtractionResult> ExtractAsync(
            string archivePath,
            string destinationPath,
            CancellationToken cancellationToken = default)
        {
            var result = await _inner.ExtractAsync(archivePath, destinationPath, cancellationToken);
            return new OptiClick.Infrastructure.Archives.ArchiveExtractionResult
            {
                IsSuccess = result.IsSuccess,
                ErrorCode = result.ErrorCode,
                ErrorMessage = result.ErrorMessage
            };
        }
    }
}
