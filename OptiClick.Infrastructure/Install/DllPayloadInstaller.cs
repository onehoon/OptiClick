using System.IO;

namespace OptiClick.Infrastructure.Install;

public interface IDllPayloadFileSystem
{
    bool DirectoryExists(string path);
    void CreateDirectory(string path);
    bool FileExists(string path);
    bool IsWritable(string path);
    void SetWritable(string path);
    void CopyFile(string sourcePath, string destinationPath, bool overwrite);
}

public interface IDllPayloadArchiveSourceReader
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

public static class DllPayloadInstallErrorCodes
{
    public const string SourceMissing = "source_missing";
    public const string InvalidDestination = "invalid_destination";
    public const string PathTraversal = "path_traversal";
    public const string MultipleCandidates = "multiple_candidates";
    public const string CopyFailed = "copy_failed";
}

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

public sealed class DllPayloadInstaller
{
    private readonly IDllPayloadFileSystem _fileSystem;
    private readonly IDllPayloadArchiveSourceReader _archiveSourceReader;

    public DllPayloadInstaller(
        IDllPayloadFileSystem fileSystem,
        IDllPayloadArchiveSourceReader archiveSourceReader)
    {
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        _archiveSourceReader = archiveSourceReader ?? throw new ArgumentNullException(nameof(archiveSourceReader));
    }

    public async Task<DllPayloadInstallResult> InstallAsync(
        DllPayloadInstallRequest request,
        CancellationToken cancellationToken = default)
    {
        var sourcePath = "";
        IReadOnlyList<string> candidates = Array.Empty<string>();
        if (!_fileSystem.DirectoryExists(request.TargetPath))
        {
            return new DllPayloadInstallResult
            {
                ErrorCode = DllPayloadInstallErrorCodes.InvalidDestination
            };
        }

        string destinationRelative;
        try
        {
            destinationRelative = NormalizeRelativeDllPath(request.DestinationRelativePath);
        }
        catch
        {
            return new DllPayloadInstallResult
            {
                ErrorCode = DllPayloadInstallErrorCodes.PathTraversal
            };
        }

        try
        {
            sourcePath = await _archiveSourceReader.ResolveSourcePathAsync(
                request.Url,
                request.CachedArchivePath,
                request.DownloadFileName,
                cancellationToken,
                request.Sha256);
            if (string.IsNullOrWhiteSpace(sourcePath))
            {
                return new DllPayloadInstallResult
                {
                    ErrorCode = DllPayloadInstallErrorCodes.SourceMissing
                };
            }

            candidates = await _archiveSourceReader.FindFilesAsync(
                sourcePath,
                file => string.Equals(Path.GetFileName(file), request.SourceDllName, StringComparison.OrdinalIgnoreCase),
                cancellationToken);

            if (candidates.Count == 0)
            {
                return new DllPayloadInstallResult
                {
                    ErrorCode = DllPayloadInstallErrorCodes.SourceMissing
                };
            }

            if (candidates.Count > 1)
            {
                return new DllPayloadInstallResult
                {
                    ErrorCode = DllPayloadInstallErrorCodes.MultipleCandidates
                };
            }

            var destinationPath = CombineUnderTarget(request.TargetPath, destinationRelative);
            var parent = Path.GetDirectoryName(destinationPath)!;
            _fileSystem.CreateDirectory(parent);
            if (_fileSystem.FileExists(destinationPath))
            {
                EnsureWritableIfExists(destinationPath);
            }

            try
            {
                // Component payload installs intentionally overwrite their managed target.
                _fileSystem.CopyFile(candidates[0], destinationPath, overwrite: true);
                return new DllPayloadInstallResult
                {
                    IsSuccess = true,
                    DestinationPath = destinationPath
                };
            }
            catch
            {
                return new DllPayloadInstallResult
                {
                    ErrorCode = DllPayloadInstallErrorCodes.CopyFailed,
                    DestinationPath = destinationPath
                };
            }
        }
        finally
        {
            if (candidates.Count > 0)
            {
                _archiveSourceReader.CleanupTemporaryPath(candidates[0]);
            }

            _archiveSourceReader.CleanupTemporaryPath(sourcePath);
        }
    }

    private static string NormalizeRelativeDllPath(string destinationRelPath)
    {
        var normalized = (destinationRelPath ?? "").Trim().Replace('\\', '/');
        while (normalized.StartsWith("./", StringComparison.Ordinal))
        {
            normalized = normalized[2..];
        }

        normalized = normalized.Trim('/');
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new InvalidOperationException("Invalid DLL destination path: (empty)");
        }

        if (Path.IsPathRooted(normalized))
        {
            throw new InvalidOperationException($"Invalid DLL destination path: {destinationRelPath}");
        }

        var relative = new PathString(normalized);
        if (relative.Parts.Any(static part => part == ".."))
        {
            throw new InvalidOperationException($"Invalid DLL destination path: {destinationRelPath}");
        }

        var fileName = Path.GetFileName(normalized);
        if (string.IsNullOrWhiteSpace(fileName) || !fileName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Invalid DLL destination path: {destinationRelPath}");
        }

        return normalized;
    }

    private static string CombineUnderTarget(string targetPath, string relativePath)
    {
        var target = EnsureTrailingSeparator(targetPath);
        var candidate = Path.GetFullPath(Path.Combine(target, relativePath));
        if (!candidate.StartsWith(target, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Path traversal detected.");
        }

        return candidate;
    }

    private static string EnsureTrailingSeparator(string path)
    {
        var fullPath = Path.GetFullPath(path);
        return Path.EndsInDirectorySeparator(fullPath)
            ? fullPath
            : fullPath + Path.DirectorySeparatorChar;
    }

    private void EnsureWritableIfExists(string path)
    {
        if (_fileSystem.FileExists(path) && !_fileSystem.IsWritable(path))
        {
            _fileSystem.SetWritable(path);
        }
    }

    private readonly record struct PathString(string Raw)
    {
        public IEnumerable<string> Parts =>
            Raw.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}
