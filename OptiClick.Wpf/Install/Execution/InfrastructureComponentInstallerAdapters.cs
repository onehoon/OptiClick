using OptiClick.Wpf.Install.Archives;
using OptiClick.Wpf.Install.FileSystem;

namespace OptiClick.Wpf.Install.Execution;

internal sealed class ComponentArchiveSourceReaderAdapter : OptiClick.Infrastructure.Install.Components.IComponentArchiveSourceReader
{
    private readonly IArchiveSourceReader _inner;

    public ComponentArchiveSourceReaderAdapter(IArchiveSourceReader inner)
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
}

internal sealed class DllPayloadInstallerAdapter : OptiClick.Infrastructure.Install.Components.IDllPayloadInstaller
{
    private readonly IDllPayloadInstaller _inner;

    public DllPayloadInstallerAdapter(IDllPayloadInstaller inner)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    public async Task<OptiClick.Infrastructure.Install.Components.DllPayloadInstallResult> InstallAsync(
        OptiClick.Infrastructure.Install.Components.DllPayloadInstallRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _inner.InstallAsync(
            new DllPayloadInstallRequest
            {
                TargetPath = request.TargetPath,
                DestinationRelativePath = request.DestinationRelativePath,
                SourceDllName = request.SourceDllName,
                Url = request.Url,
                CachedArchivePath = request.CachedArchivePath,
                DownloadFileName = request.DownloadFileName,
                Sha256 = request.Sha256
            },
            cancellationToken);

        return new OptiClick.Infrastructure.Install.Components.DllPayloadInstallResult
        {
            IsSuccess = result.IsSuccess,
            IsSkipped = result.IsSkipped,
            ErrorCode = result.ErrorCode,
            DestinationPath = result.DestinationPath
        };
    }
}

internal sealed class InstallFileSystemAdapter :
    OptiClick.Infrastructure.Install.Components.IInstallFileSystem,
    OptiClick.Infrastructure.Install.Components.IComponentInstallFileSystem
{
    private readonly IInstallFileSystem _inner;

    public InstallFileSystemAdapter(IInstallFileSystem inner)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    public bool FileExists(string path) => _inner.FileExists(path);

    public bool DirectoryExists(string path) => _inner.DirectoryExists(path);

    public void DeleteFile(string path) => _inner.DeleteFile(path);

    public void SetWritable(string path) => _inner.SetWritable(path);

    public bool IsWritable(string path) => _inner.IsWritable(path);

    public void CreateDirectory(string path) => _inner.CreateDirectory(path);

    public void DeleteDirectory(string path, bool recursive = true) => _inner.DeleteDirectory(path, recursive);

    public void CopyFile(string sourcePath, string destinationPath, bool overwrite) => _inner.CopyFile(sourcePath, destinationPath, overwrite);

    public IEnumerable<string> EnumerateFileSystemEntries(string directoryPath) => _inner.EnumerateFileSystemEntries(directoryPath);
}

internal sealed class FileSignatureDetectorsAdapter : OptiClick.Infrastructure.Install.Components.IFileSignatureDetectors
{
    private readonly IFileSignatureDetectors _inner;

    public FileSignatureDetectorsAdapter(IFileSignatureDetectors inner)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    public bool IsReShadeDll(string filePath) => _inner.IsReShadeDll(filePath);

    public bool IsSpecialKDll(string filePath) => _inner.IsSpecialKDll(filePath);
}

internal sealed class ComponentArchiveDownloaderAdapter : OptiClick.Infrastructure.Install.Components.IComponentArchiveDownloader
{
    private readonly IArchiveDownloader _inner;

    public ComponentArchiveDownloaderAdapter(IArchiveDownloader inner)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    public async Task<OptiClick.Infrastructure.Install.Components.ComponentArchiveDownloadResult> DownloadAsync(
        string url,
        string destinationPath,
        TimeSpan timeout,
        CancellationToken cancellationToken = default,
        string fallbackSha256 = "")
    {
        var result = await _inner.DownloadAsync(url, destinationPath, timeout, cancellationToken, fallbackSha256);
        return new OptiClick.Infrastructure.Install.Components.ComponentArchiveDownloadResult
        {
            IsSuccess = result.IsSuccess,
            DestinationPath = result.DestinationPath,
            ErrorCode = result.ErrorCode,
            ErrorMessage = result.ErrorMessage
        };
    }
}

internal sealed class ComponentArchiveExtractorAdapter : OptiClick.Infrastructure.Install.Components.IComponentArchiveExtractor
{
    private readonly IArchiveExtractor _inner;

    public ComponentArchiveExtractorAdapter(IArchiveExtractor inner)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    public async Task<OptiClick.Infrastructure.Install.Components.ComponentArchiveExtractionResult> ExtractAsync(
        string archivePath,
        string destinationPath,
        CancellationToken cancellationToken = default)
    {
        var result = await _inner.ExtractAsync(archivePath, destinationPath, cancellationToken);
        return new OptiClick.Infrastructure.Install.Components.ComponentArchiveExtractionResult
        {
            IsSuccess = result.IsSuccess,
            ErrorCode = result.ErrorCode,
            ErrorMessage = result.ErrorMessage
        };
    }
}
