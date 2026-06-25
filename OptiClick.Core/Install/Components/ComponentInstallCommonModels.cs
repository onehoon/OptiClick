using OptiClick.Core.Install;

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
    SpecialK,
    ReFramework,
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

public sealed record ComponentInstallResult
{
    public bool IsSuccess { get; init; }
    public IReadOnlyList<ComponentInstallStepResult> Steps { get; init; } = Array.Empty<ComponentInstallStepResult>();
    public ComponentInstallStepResult? FailedStep { get; init; }
}

public sealed record ComponentInstallContext
{
    public InstallExecutionDescriptor ExecutionDescriptor { get; init; } = InstallExecutionDescriptor.Empty;
    public string TargetPath { get; init; } = "";
    public string FinalDllName { get; init; } = "";
    public string OptiScalerPayloadDirectory { get; init; } = "";
    public string OptiScalerVariant { get; init; } = "";
    public string OptiScalerVersion { get; init; } = "";
    public string OptiScalerDisplayVersion { get; init; } = "";
    public string GpuVendor { get; init; } = "";
    public string GpuName { get; init; } = "";
    public string Fsr4SourceArchive { get; init; } = "";
    public string Fsr4Variant { get; init; } = "";
    public bool HasPlannedComponentInstallers { get; init; }
    public IReadOnlyList<ComponentInstallName> PlannedComponentInstallers { get; init; } = Array.Empty<ComponentInstallName>();
    public ModuleDownloadLinkCatalog ModuleDownloadLinks { get; init; } = ModuleDownloadLinkCatalog.Empty;
    public bool ShouldInstallUnreal5 { get; init; }
    public bool ShouldInstallFsr4 { get; init; }
    public string GpuBundleKey { get; init; } = "";
    public string GpuGroup { get; init; } = "";
    public string ReFrameworkDestination { get; init; } = "";
    public string SpecialKValue { get; init; } = "";
    public string ExtraBundleAlias { get; init; } = "";
    public string SpecialKCachedArchivePath { get; init; } = "";
    public string ReFrameworkCachedArchivePath { get; init; } = "";
    public string Unreal5CachedArchivePath { get; init; } = "";
}
