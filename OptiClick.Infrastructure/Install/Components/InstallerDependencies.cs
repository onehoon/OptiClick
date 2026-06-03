namespace OptiClick.Infrastructure.Install.Components;

public sealed record DllPayloadInstallRequest
{
    public string TargetPath { get; init; } = "";
    public string DestinationRelativePath { get; init; } = "";
    public string SourceDllName { get; init; } = "";
    public string Url { get; init; } = "";
    public string CachedArchivePath { get; init; } = "";
    public string DownloadFileName { get; init; } = "";
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

public interface IInstallFileSystem
{
    bool FileExists(string path);
    bool DirectoryExists(string path);
    void DeleteFile(string path);
    void SetWritable(string path);
    bool IsWritable(string path);
}

public interface IFileSignatureDetectors
{
    bool IsReShadeDll(string filePath);
    bool IsSpecialKDll(string filePath);
    bool IsUltimateAsiLoaderDll(string filePath);
}

public sealed record ComponentArchiveDownloadResult
{
    public bool IsSuccess { get; init; }
    public string DestinationPath { get; init; } = "";
    public string ErrorCode { get; init; } = "";
    public string ErrorMessage { get; init; } = "";
}

public sealed record ComponentArchiveExtractionResult
{
    public bool IsSuccess { get; init; }
    public string ErrorCode { get; init; } = "";
    public string ErrorMessage { get; init; } = "";
}

public interface IComponentArchiveDownloader
{
    Task<ComponentArchiveDownloadResult> DownloadAsync(
        string url,
        string destinationPath,
        TimeSpan timeout,
        CancellationToken cancellationToken = default);
}

public interface IComponentArchiveExtractor
{
    Task<ComponentArchiveExtractionResult> ExtractAsync(
        string archivePath,
        string destinationPath,
        CancellationToken cancellationToken = default);
}

public interface IComponentInstallFileSystem : IInstallFileSystem
{
    void CreateDirectory(string path);
    void DeleteDirectory(string path, bool recursive = true);
    void CopyFile(string sourcePath, string destinationPath, bool overwrite);
    IEnumerable<string> EnumerateFileSystemEntries(string directoryPath);
}

public interface IComponentArchiveSourceReader
{
    Task<string> ResolveSourcePathAsync(
        string url,
        string cachedArchivePath,
        string downloadFilename,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> FindFilesAsync(
        string sourcePath,
        Func<string, bool> predicate,
        CancellationToken cancellationToken = default);

    bool CleanupTemporaryPath(string path);
}
