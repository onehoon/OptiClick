namespace OptiClick.Infrastructure.Install.Uninstall;

public enum UninstallCandidateKind
{
    OptiScaler,
    ReFramework,
    SpecialK
}

public enum UninstallPlanStatus
{
    Ready,
    NothingToRemove,
    InvalidTarget,
    ValidationFailed
}

public enum UninstallExecutionStatus
{
    Success,
    PartialSuccess,
    Failed,
    NothingToRemove
}

public static class UninstallErrorCodes
{
    public const string None = "";
    public const string InvalidTarget = "invalid_target";
    public const string ValidationFailed = "validation_failed";
    public const string InvalidPlanStatus = "invalid_plan_status";
    public const string LockedOrPermissionDenied = "locked_or_permission_denied";
    public const string DeleteFailed = "delete_failed";
    public const string SetWritableFailed = "set_writable_failed";
    public const string ConfigCleanupFailed = "config_cleanup_failed";
}

public static class UninstallSkipReasons
{
    public const string InvalidPath = "invalid_path";
    public const string OutsideTarget = "outside_target";
    public const string SubdirectoryNotAllowed = "subdirectory_not_allowed";
    public const string InvalidExtension = "invalid_extension";
    public const string VersionInfoUnavailable = "version_info_unavailable";
    public const string SignatureNotMatched = "signature_not_matched";
    public const string ComponentNotRequested = "component_not_requested";
    public const string ReShadeSignatureMatched = "reshade_signature_matched";
    public const string MissingAtExecution = "missing_at_execution";
    public const string DirectoryAtExecution = "directory_at_execution";
    public const string FileAtDirectoryTarget = "file_at_directory_target";
    public const string InvalidDirectoryTarget = "invalid_directory_target";
    public const string SignatureChanged = "signature_changed";
    public const string SignatureValidationRequired = "signature_validation_required";
    public const string ConfigTargetMissing = "config_target_missing";
    public const string ConfigEntryMissing = "config_entry_missing";
}

public sealed record UninstallComponentTarget
{
    public UninstallCandidateKind Kind { get; init; }
    public string RelativePath { get; init; } = "";
    public bool RequiresSignatureValidation { get; init; } = true;
}

public sealed record UninstallDirectoryTarget
{
    public UninstallCandidateKind Kind { get; init; }
    public string RelativePath { get; init; } = "";
    public bool Recursive { get; init; } = true;
}

public sealed record UninstallPlanBuildRequest
{
    public string TargetPath { get; init; } = "";
    public IReadOnlyList<UninstallComponentTarget> ComponentTargets { get; init; } =
        Array.Empty<UninstallComponentTarget>();
    public IReadOnlyList<UninstallDirectoryTarget> DirectoryTargets { get; init; } =
        Array.Empty<UninstallDirectoryTarget>();
    public IReadOnlyList<UninstallEngineIniCleanupTarget> EngineIniCleanupTargets { get; init; } =
        Array.Empty<UninstallEngineIniCleanupTarget>();
}

public sealed record UninstallEngineIniCleanupTarget
{
    public string FullPath { get; init; } = "";
    public string Section { get; init; } = "";
    public string Key { get; init; } = "";
}

public sealed record UninstallCandidate
{
    public string FullPath { get; init; } = "";
    public string RelativePath { get; init; } = "";
    public UninstallCandidateKind Kind { get; init; }
    public string MatchedText { get; init; } = "";
    public bool IsReadOnly { get; init; }
    public bool RequiresSignatureValidation { get; init; } = true;
}

public sealed record UninstallDirectoryCandidate
{
    public string FullPath { get; init; } = "";
    public string RelativePath { get; init; } = "";
    public UninstallCandidateKind Kind { get; init; }
    public bool Recursive { get; init; } = true;
}

public sealed record UninstallSkippedFile
{
    public string FullPath { get; init; } = "";
    public string Reason { get; init; } = "";
}

public sealed record UninstallPlan
{
    public UninstallPlanStatus Status { get; init; }
    public string TargetPath { get; init; } = "";
    public IReadOnlyList<UninstallCandidate> Candidates { get; init; } = Array.Empty<UninstallCandidate>();
    public IReadOnlyList<UninstallDirectoryCandidate> DirectoryCandidates { get; init; } =
        Array.Empty<UninstallDirectoryCandidate>();
    public IReadOnlyList<UninstallEngineIniCleanupTarget> EngineIniCleanupTargets { get; init; } =
        Array.Empty<UninstallEngineIniCleanupTarget>();
    public IReadOnlyList<UninstallSkippedFile> SkippedFiles { get; init; } = Array.Empty<UninstallSkippedFile>();
    public string ErrorCode { get; init; } = "";
    public string Message { get; init; } = "";
}

public sealed record UninstallDeletedFile
{
    public string FullPath { get; init; } = "";
    public string RelativePath { get; init; } = "";
    public UninstallCandidateKind Kind { get; init; }
}

public sealed record UninstallFailedFile
{
    public string FullPath { get; init; } = "";
    public string RelativePath { get; init; } = "";
    public UninstallCandidateKind Kind { get; init; }
    public string ErrorCode { get; init; } = "";
    public string Message { get; init; } = "";
}

public sealed record UninstallDeletedDirectory
{
    public string FullPath { get; init; } = "";
    public string RelativePath { get; init; } = "";
    public UninstallCandidateKind Kind { get; init; }
}

public sealed record UninstallFailedDirectory
{
    public string FullPath { get; init; } = "";
    public string RelativePath { get; init; } = "";
    public UninstallCandidateKind Kind { get; init; }
    public string ErrorCode { get; init; } = "";
    public string Message { get; init; } = "";
}

public sealed record UninstallCleanedEngineIniEntry
{
    public string FullPath { get; init; } = "";
    public string Section { get; init; } = "";
    public string Key { get; init; } = "";
}

public sealed record UninstallSkippedEngineIniEntry
{
    public string FullPath { get; init; } = "";
    public string Section { get; init; } = "";
    public string Key { get; init; } = "";
    public string Reason { get; init; } = "";
}

public sealed record UninstallFailedEngineIniEntry
{
    public string FullPath { get; init; } = "";
    public string Section { get; init; } = "";
    public string Key { get; init; } = "";
    public string ErrorCode { get; init; } = "";
    public string Message { get; init; } = "";
}

public sealed record UninstallExecutionResult
{
    public UninstallExecutionStatus Status { get; init; }
    public IReadOnlyList<UninstallDeletedFile> DeletedFiles { get; init; } = Array.Empty<UninstallDeletedFile>();
    public IReadOnlyList<UninstallFailedFile> FailedFiles { get; init; } = Array.Empty<UninstallFailedFile>();
    public IReadOnlyList<UninstallDeletedDirectory> DeletedDirectories { get; init; } =
        Array.Empty<UninstallDeletedDirectory>();
    public IReadOnlyList<UninstallFailedDirectory> FailedDirectories { get; init; } =
        Array.Empty<UninstallFailedDirectory>();
    public IReadOnlyList<UninstallSkippedFile> SkippedFiles { get; init; } = Array.Empty<UninstallSkippedFile>();
    public IReadOnlyList<UninstallCleanedEngineIniEntry> CleanedEngineIniEntries { get; init; } =
        Array.Empty<UninstallCleanedEngineIniEntry>();
    public IReadOnlyList<UninstallSkippedEngineIniEntry> SkippedEngineIniEntries { get; init; } =
        Array.Empty<UninstallSkippedEngineIniEntry>();
    public IReadOnlyList<UninstallFailedEngineIniEntry> FailedEngineIniEntries { get; init; } =
        Array.Empty<UninstallFailedEngineIniEntry>();
    public string ErrorCode { get; init; } = "";
    public string Message { get; init; } = "";
}

public sealed record UninstallSignatureDetection
{
    public bool IsMatch { get; init; }
    public bool IsVersionInfoAvailable { get; init; }
    public UninstallCandidateKind Kind { get; init; }
    public string MatchedText { get; init; } = "";
    public string Reason { get; init; } = "";

    public static UninstallSignatureDetection Matched(UninstallCandidateKind kind, string matchedText)
    {
        return new UninstallSignatureDetection
        {
            IsMatch = true,
            IsVersionInfoAvailable = true,
            Kind = kind,
            MatchedText = matchedText
        };
    }

    public static UninstallSignatureDetection NotMatched(bool hasVersionInfo, string reason)
    {
        return new UninstallSignatureDetection
        {
            IsMatch = false,
            IsVersionInfoAvailable = hasVersionInfo,
            Reason = reason
        };
    }
}

public interface IOptiClickUninstallPlanBuilder
{
    UninstallPlan BuildPlan(string targetPath);
    UninstallPlan BuildPlan(UninstallPlanBuildRequest request);
}

public interface IOptiClickUninstallExecutor
{
    Task<UninstallExecutionResult> ExecuteAsync(UninstallPlan plan, CancellationToken cancellationToken = default);
}

public interface IOptiClickUninstallFileSystem
{
    bool FileExists(string path);
    bool DirectoryExists(string path);
    bool IsWritable(string path);
    void SetWritable(string path);
    void DeleteFile(string path);
    void DeleteDirectory(string path, bool recursive = true);
    IEnumerable<string> EnumerateFiles(string directoryPath, string searchPattern, SearchOption searchOption);
}

public interface IOptiClickUninstallSignatureDetector
{
    UninstallSignatureDetection Detect(string filePath);
}

public interface IOptiClickUninstallLogger
{
    void Info(string category, string message);
    void Warning(string category, string message);
    void Error(string category, string message, Exception? exception = null);
}

public sealed class NullOptiClickUninstallLogger : IOptiClickUninstallLogger
{
    public static NullOptiClickUninstallLogger Instance { get; } = new();

    private NullOptiClickUninstallLogger()
    {
    }

    public void Info(string category, string message)
    {
        _ = category;
        _ = message;
    }

    public void Warning(string category, string message)
    {
        _ = category;
        _ = message;
    }

    public void Error(string category, string message, Exception? exception = null)
    {
        _ = category;
        _ = message;
        _ = exception;
    }
}
