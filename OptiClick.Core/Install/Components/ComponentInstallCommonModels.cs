namespace OptiClick.Core.Install.Components;

public enum ComponentInstallStatus
{
    Success,
    Skipped,
    Failed
}

public enum ComponentInstallName
{
    OptiScalerCore,
    ExtraBundle,
    UltimateAsiLoader,
    SpecialK,
    ReFramework,
    OptiPatcher,
    Unreal5,
    Fsr4
}

public static class ComponentInstallErrorCodes
{
    public const string None = "";
    public const string SourceMissing = "source_missing";
    public const string InvalidDestination = "invalid_destination";
    public const string PathTraversal = "path_traversal";
    public const string MultipleCandidates = "multiple_candidates";
    public const string PayloadMissing = "payload_missing";
    public const string UnsupportedArchive = "unsupported_archive";
    public const string ProtectedExistingFile = "protected_existing_file";
    public const string CopyFailed = "copy_failed";
    public const string ExtractFailed = "extract_failed";
    public const string DownloadFailed = "download_failed";
    public const string MissingMetadata = "missing_metadata";
    public const string InvalidSignature = "invalid_signature";
    public const string LegacyCleanupFailed = "legacy_cleanup_failed";
    public const string LegacyCleanupDeleteFailed = "legacy_cleanup_delete_failed";
    public const string LegacyCleanupWritableFailed = "legacy_cleanup_writable_failed";
    public const string LegacyCleanupInvalidTarget = "legacy_cleanup_invalid_target";
}

public sealed record ComponentInstallOperation
{
    public string Kind { get; init; } = "";
    public string Source { get; init; } = "";
    public string Destination { get; init; } = "";
}

public sealed record ComponentInstallStepResult
{
    public ComponentInstallName Component { get; init; }
    public ComponentInstallStatus Status { get; init; }
    public string ErrorCode { get; init; } = "";
    public string Message { get; init; } = "";
    public IReadOnlyList<ComponentInstallOperation> Operations { get; init; } = Array.Empty<ComponentInstallOperation>();

    public static ComponentInstallStepResult Success(ComponentInstallName component, IReadOnlyList<ComponentInstallOperation>? operations = null)
        => new()
        {
            Component = component,
            Status = ComponentInstallStatus.Success,
            Operations = operations ?? Array.Empty<ComponentInstallOperation>()
        };

    public static ComponentInstallStepResult Skipped(ComponentInstallName component, string message = "")
        => new()
        {
            Component = component,
            Status = ComponentInstallStatus.Skipped,
            Message = message
        };

    public static ComponentInstallStepResult Failed(ComponentInstallName component, string errorCode, string message = "")
        => new()
        {
            Component = component,
            Status = ComponentInstallStatus.Failed,
            ErrorCode = errorCode,
            Message = message
        };
}
