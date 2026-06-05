using OptiClick.Wpf.Install.FileSystem;

namespace OptiClick.Wpf.Install.Execution;

public sealed record DllPayloadInstallRequest
{
    public string TargetPath { get; init; } = "";
    public string DestinationRelativePath { get; init; } = "";
    public string SourceDllName { get; init; } = "";
    public string Url { get; init; } = "";
    public string CachedArchivePath { get; init; } = "";
    public string DownloadFileName { get; init; } = "";
    public string Sha256 { get; init; } = "";
}

public sealed record DllPayloadInstallResult
{
    public bool IsSuccess { get; init; }
    public bool IsSkipped { get; init; }
    public string ErrorCode { get; init; } = "";
    public string DestinationPath { get; init; } = "";
}

public interface IDllPayloadInstaller
{
    Task<DllPayloadInstallResult> InstallAsync(DllPayloadInstallRequest request, CancellationToken cancellationToken = default);
}

public sealed class DllPayloadInstaller : IDllPayloadInstaller
{
    private readonly OptiClick.Infrastructure.Install.DllPayloadInstaller _inner;

    public DllPayloadInstaller(IInstallFileSystem fileSystem, IArchiveSourceReader archiveSourceReader)
        : this(new OptiClick.Infrastructure.Install.DllPayloadInstaller(
            new InstallFileSystemAdapter(fileSystem),
            new ArchiveSourceReaderAdapter(archiveSourceReader)))
    {
    }

    internal DllPayloadInstaller(OptiClick.Infrastructure.Install.DllPayloadInstaller inner)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    public async Task<DllPayloadInstallResult> InstallAsync(DllPayloadInstallRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var result = await _inner.InstallAsync(new OptiClick.Infrastructure.Install.DllPayloadInstallRequest
        {
            TargetPath = request.TargetPath,
            DestinationRelativePath = request.DestinationRelativePath,
            SourceDllName = request.SourceDllName,
            Url = request.Url,
            CachedArchivePath = request.CachedArchivePath,
            DownloadFileName = request.DownloadFileName,
            Sha256 = request.Sha256
        }, cancellationToken);

        return new DllPayloadInstallResult
        {
            IsSuccess = result.IsSuccess,
            IsSkipped = result.IsSkipped,
            ErrorCode = result.ErrorCode,
            DestinationPath = result.DestinationPath
        };
    }

    private sealed class InstallFileSystemAdapter : OptiClick.Infrastructure.Install.IDllPayloadFileSystem
    {
        private readonly IInstallFileSystem _inner;

        public InstallFileSystemAdapter(IInstallFileSystem inner)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        public bool DirectoryExists(string path) => _inner.DirectoryExists(path);
        public void CreateDirectory(string path) => _inner.CreateDirectory(path);
        public bool FileExists(string path) => _inner.FileExists(path);
        public bool IsWritable(string path) => _inner.IsWritable(path);
        public void SetWritable(string path) => _inner.SetWritable(path);
        public void CopyFile(string sourcePath, string destinationPath, bool overwrite) => _inner.CopyFile(sourcePath, destinationPath, overwrite);
    }

    private sealed class ArchiveSourceReaderAdapter : OptiClick.Infrastructure.Install.IDllPayloadArchiveSourceReader
    {
        private readonly IArchiveSourceReader _inner;

        public ArchiveSourceReaderAdapter(IArchiveSourceReader inner)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        public Task<string> ResolveSourcePathAsync(
            string url,
            string cachedArchivePath,
            string downloadFilename,
            CancellationToken cancellationToken = default,
            string fallbackSha256 = "")
        {
            return _inner.ResolveSourcePathAsync(url, cachedArchivePath, downloadFilename, cancellationToken, fallbackSha256);
        }

        public Task<IReadOnlyList<string>> FindFilesAsync(
            string sourcePath,
            Func<string, bool> predicate,
            CancellationToken cancellationToken = default)
        {
            return _inner.FindFilesAsync(sourcePath, predicate, cancellationToken);
        }

        public bool CleanupTemporaryPath(string path)
        {
            return _inner.CleanupTemporaryPath(path);
        }
    }
}
