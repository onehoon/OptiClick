using CoreComponentInstallErrorCodes = OptiClick.Core.Install.Components.ComponentInstallErrorCodes;
using CoreComponentInstallName = OptiClick.Core.Install.Components.ComponentInstallName;
using CoreComponentInstallOperation = OptiClick.Core.Install.Components.ComponentInstallOperation;
using CoreComponentInstallStatus = OptiClick.Core.Install.Components.ComponentInstallStatus;
using CoreComponentInstallStepResult = OptiClick.Core.Install.Components.ComponentInstallStepResult;
using OptiClick.Core.Install;

namespace OptiClick.Infrastructure.Install.Components;

public enum ComponentInstallStatus
{
    Success = (int)CoreComponentInstallStatus.Success,
    Skipped = (int)CoreComponentInstallStatus.Skipped,
    Failed = (int)CoreComponentInstallStatus.Failed
}

public enum ComponentInstallName
{
    // Infrastructure component adapters only handle post-core components.
    // OptiScalerCore is installed by the dedicated core installer path.
    UltimateAsiLoader = (int)CoreComponentInstallName.UltimateAsiLoader,
    SpecialK = (int)CoreComponentInstallName.SpecialK,
    ReFramework = (int)CoreComponentInstallName.ReFramework,
    ExtraBundle = (int)CoreComponentInstallName.ExtraBundle,
    OptiPatcher = (int)CoreComponentInstallName.OptiPatcher,
    Unreal5 = (int)CoreComponentInstallName.Unreal5,
    Fsr4 = (int)CoreComponentInstallName.Fsr4
}

public static class ComponentInstallErrorCodes
{
    public const string None = CoreComponentInstallErrorCodes.None;
    public const string SourceMissing = CoreComponentInstallErrorCodes.SourceMissing;
    public const string InvalidDestination = CoreComponentInstallErrorCodes.InvalidDestination;
    public const string PathTraversal = CoreComponentInstallErrorCodes.PathTraversal;
    public const string MultipleCandidates = CoreComponentInstallErrorCodes.MultipleCandidates;
    public const string PayloadMissing = CoreComponentInstallErrorCodes.PayloadMissing;
    public const string UnsupportedArchive = CoreComponentInstallErrorCodes.UnsupportedArchive;
    public const string CopyFailed = CoreComponentInstallErrorCodes.CopyFailed;
    public const string ExtractFailed = CoreComponentInstallErrorCodes.ExtractFailed;
    public const string DownloadFailed = CoreComponentInstallErrorCodes.DownloadFailed;
    public const string MissingMetadata = CoreComponentInstallErrorCodes.MissingMetadata;
    public const string InvalidSignature = CoreComponentInstallErrorCodes.InvalidSignature;
}

public sealed record ComponentInstallStepResult
{
    public ComponentInstallName Component { get; init; }
    public ComponentInstallStatus Status { get; init; }
    public string ErrorCode { get; init; } = "";
    public string Message { get; init; } = "";

    public static ComponentInstallStepResult Success(ComponentInstallName component)
        => new()
        {
            Component = component,
            Status = ComponentInstallStatus.Success
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

    public CoreComponentInstallStepResult ToCore()
    {
        return new CoreComponentInstallStepResult
        {
            Component = ComponentInstallerModelMapper.ToCore(Component),
            Status = ComponentInstallerModelMapper.ToCore(Status),
            ErrorCode = ErrorCode,
            Message = Message,
            Operations = Array.Empty<CoreComponentInstallOperation>()
        };
    }

    public static ComponentInstallStepResult FromCore(CoreComponentInstallStepResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return new ComponentInstallStepResult
        {
            Component = ComponentInstallerModelMapper.ToInfrastructure(result.Component),
            Status = ComponentInstallerModelMapper.ToInfrastructure(result.Status),
            ErrorCode = result.ErrorCode,
            Message = result.Message
        };
    }
}

internal static class ComponentInstallerModelMapper
{
    private const string PostCoreOnlyMessage =
        "Infrastructure component mapper only supports post-core components. OptiScalerCore is handled separately.";

    public static CoreComponentInstallName ToCore(ComponentInstallName component) => component switch
    {
        ComponentInstallName.UltimateAsiLoader => CoreComponentInstallName.UltimateAsiLoader,
        ComponentInstallName.SpecialK => CoreComponentInstallName.SpecialK,
        ComponentInstallName.ReFramework => CoreComponentInstallName.ReFramework,
        ComponentInstallName.ExtraBundle => CoreComponentInstallName.ExtraBundle,
        ComponentInstallName.OptiPatcher => CoreComponentInstallName.OptiPatcher,
        ComponentInstallName.Unreal5 => CoreComponentInstallName.Unreal5,
        ComponentInstallName.Fsr4 => CoreComponentInstallName.Fsr4,
        _ => throw new ArgumentOutOfRangeException(nameof(component), component, PostCoreOnlyMessage)
    };

    public static CoreComponentInstallStatus ToCore(ComponentInstallStatus status) => status switch
    {
        ComponentInstallStatus.Success => CoreComponentInstallStatus.Success,
        ComponentInstallStatus.Skipped => CoreComponentInstallStatus.Skipped,
        ComponentInstallStatus.Failed => CoreComponentInstallStatus.Failed,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, $"Unsupported infrastructure status mapping: {status}")
    };

    public static ComponentInstallName ToInfrastructure(CoreComponentInstallName component) => component switch
    {
        CoreComponentInstallName.UltimateAsiLoader => ComponentInstallName.UltimateAsiLoader,
        CoreComponentInstallName.SpecialK => ComponentInstallName.SpecialK,
        CoreComponentInstallName.ReFramework => ComponentInstallName.ReFramework,
        CoreComponentInstallName.ExtraBundle => ComponentInstallName.ExtraBundle,
        CoreComponentInstallName.OptiPatcher => ComponentInstallName.OptiPatcher,
        CoreComponentInstallName.Unreal5 => ComponentInstallName.Unreal5,
        CoreComponentInstallName.Fsr4 => ComponentInstallName.Fsr4,
        _ => throw new ArgumentOutOfRangeException(nameof(component), component, PostCoreOnlyMessage)
    };

    public static ComponentInstallStatus ToInfrastructure(CoreComponentInstallStatus status) => status switch
    {
        CoreComponentInstallStatus.Success => ComponentInstallStatus.Success,
        CoreComponentInstallStatus.Skipped => ComponentInstallStatus.Skipped,
        CoreComponentInstallStatus.Failed => ComponentInstallStatus.Failed,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, $"Unsupported core status mapping: {status}")
    };
}

public sealed record UltimateAsiLoaderInstallContext
{
    public string TargetPath { get; init; } = "";
    public bool UseUltimateAsiLoader { get; init; }
    public IReadOnlyList<string> UalDetectedNames { get; init; } = Array.Empty<string>();
    public ModuleDownloadLinkCatalog ModuleDownloadLinks { get; init; } = ModuleDownloadLinkCatalog.Empty;
    public string UalCachedArchivePath { get; init; } = "";
}

public sealed record SpecialKInstallContext
{
    public string TargetPath { get; init; } = "";
    public string FinalDllName { get; init; } = "";
    public string SpecialKValue { get; init; } = "";
    public ModuleDownloadLinkCatalog ModuleDownloadLinks { get; init; } = ModuleDownloadLinkCatalog.Empty;
    public string SpecialKCachedArchivePath { get; init; } = "";
}

public sealed record ReFrameworkInstallContext
{
    public string TargetPath { get; init; } = "";
    public string ReFrameworkDestination { get; init; } = "";
    public ModuleDownloadLinkCatalog ModuleDownloadLinks { get; init; } = ModuleDownloadLinkCatalog.Empty;
    public string ReFrameworkCachedArchivePath { get; init; } = "";
}

public sealed record ExtraBundleInstallContext
{
    public string TargetPath { get; init; } = "";
    public string ExtraBundleAlias { get; init; } = "";
    public ModuleDownloadLinkCatalog ModuleDownloadLinks { get; init; } = ModuleDownloadLinkCatalog.Empty;
}

public sealed record OptiPatcherInstallContext
{
    public string TargetPath { get; init; } = "";
    public bool UseOptiPatcher { get; init; }
    public ModuleDownloadLinkCatalog ModuleDownloadLinks { get; init; } = ModuleDownloadLinkCatalog.Empty;
    public string OptiPatcherCachedArchivePath { get; init; } = "";
}

public sealed record Unreal5InstallContext
{
    public string TargetPath { get; init; } = "";
    public bool UseUnreal5 { get; init; }
    public ModuleDownloadLinkCatalog ModuleDownloadLinks { get; init; } = ModuleDownloadLinkCatalog.Empty;
    public string Unreal5CachedArchivePath { get; init; } = "";
}

public sealed record Fsr4InstallContext
{
    public string TargetPath { get; init; } = "";
    public bool UseFsr4 { get; init; }
    public string Fsr4Variant { get; init; } = "";
    public string Fsr4SourceArchivePath { get; init; } = "";
    public string GpuVendor { get; init; } = "";
    public string GpuName { get; init; } = "";
    public string GpuBundleKey { get; init; } = "";
    public string GpuGroup { get; init; } = "";
}

public sealed record Fsr4InstallEligibilityContext
{
    public bool UseFsr4 { get; init; }
    public string Fsr4Variant { get; init; } = "";
    public string GpuVendor { get; init; } = "";
    public string GpuName { get; init; } = "";
    public string GpuBundleKey { get; init; } = "";
    public string GpuGroup { get; init; } = "";
}

public sealed record Fsr4InstallEligibility
{
    public bool CanInstall { get; init; }
    public string SkipReason { get; init; } = "";
}
