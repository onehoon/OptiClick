using OptiClick.Wpf.Shell.Games;
using CoreComponentInstallErrorCodes = OptiClick.Core.Install.Components.ComponentInstallErrorCodes;
using CoreComponentInstallName = OptiClick.Core.Install.Components.ComponentInstallName;
using CoreComponentInstallOperation = OptiClick.Core.Install.Components.ComponentInstallOperation;
using CoreComponentInstallStatus = OptiClick.Core.Install.Components.ComponentInstallStatus;
using CoreComponentInstallStepResult = OptiClick.Core.Install.Components.ComponentInstallStepResult;

namespace OptiClick.Wpf.Install.Execution;

public enum ComponentInstallStatus
{
    Success = (int)CoreComponentInstallStatus.Success,
    Skipped = (int)CoreComponentInstallStatus.Skipped,
    Failed = (int)CoreComponentInstallStatus.Failed
}

public enum ComponentInstallName
{
    OptiScalerCore = (int)CoreComponentInstallName.OptiScalerCore,
    ExtraBundle = (int)CoreComponentInstallName.ExtraBundle,
    UltimateAsiLoader = (int)CoreComponentInstallName.UltimateAsiLoader,
    SpecialK = (int)CoreComponentInstallName.SpecialK,
    ReFramework = (int)CoreComponentInstallName.ReFramework,
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
    public const string ProtectedExistingFile = CoreComponentInstallErrorCodes.ProtectedExistingFile;
    public const string CopyFailed = CoreComponentInstallErrorCodes.CopyFailed;
    public const string ExtractFailed = CoreComponentInstallErrorCodes.ExtractFailed;
    public const string DownloadFailed = CoreComponentInstallErrorCodes.DownloadFailed;
    public const string MissingMetadata = CoreComponentInstallErrorCodes.MissingMetadata;
    public const string InvalidSignature = CoreComponentInstallErrorCodes.InvalidSignature;
    public const string LegacyCleanupFailed = CoreComponentInstallErrorCodes.LegacyCleanupFailed;
    public const string LegacyCleanupDeleteFailed = CoreComponentInstallErrorCodes.LegacyCleanupDeleteFailed;
    public const string LegacyCleanupWritableFailed = CoreComponentInstallErrorCodes.LegacyCleanupWritableFailed;
    public const string LegacyCleanupInvalidTarget = CoreComponentInstallErrorCodes.LegacyCleanupInvalidTarget;
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

    public CoreComponentInstallStepResult ToCore()
    {
        return new CoreComponentInstallStepResult
        {
            Component = ConvertEnum<ComponentInstallName, CoreComponentInstallName>(Component),
            Status = ConvertEnum<ComponentInstallStatus, CoreComponentInstallStatus>(Status),
            ErrorCode = ErrorCode,
            Message = Message,
            Operations = Operations.Select(static operation => new CoreComponentInstallOperation
            {
                Kind = operation.Kind,
                Source = operation.Source,
                Destination = operation.Destination
            }).ToArray()
        };
    }

    public static ComponentInstallStepResult FromCore(CoreComponentInstallStepResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return new ComponentInstallStepResult
        {
            Component = ConvertEnum<CoreComponentInstallName, ComponentInstallName>(result.Component),
            Status = ConvertEnum<CoreComponentInstallStatus, ComponentInstallStatus>(result.Status),
            ErrorCode = result.ErrorCode,
            Message = result.Message,
            Operations = result.Operations.Select(static operation => new ComponentInstallOperation
            {
                Kind = operation.Kind,
                Source = operation.Source,
                Destination = operation.Destination
            }).ToArray()
        };
    }

    private static TTarget ConvertEnum<TSource, TTarget>(TSource value)
        where TSource : struct, Enum
        where TTarget : struct, Enum
    {
        var name = value.ToString();
        if (Enum.TryParse<TTarget>(name, ignoreCase: false, out var converted))
        {
            return converted;
        }

        throw new ArgumentOutOfRangeException(nameof(value), value, $"Unsupported enum mapping from {typeof(TSource).Name} to {typeof(TTarget).Name}.");
    }
}

public sealed record ComponentInstallResult
{
    public bool IsSuccess { get; init; }
    public IReadOnlyList<ComponentInstallStepResult> Steps { get; init; } = Array.Empty<ComponentInstallStepResult>();
    public ComponentInstallStepResult? FailedStep { get; init; }
}

public sealed record ComponentInstallContext
{
    public ShellGameCardModel? Game { get; init; }
    public string TargetPath { get; init; } = "";
    public string FinalDllName { get; init; } = "";
    public string OptiScalerPayloadDirectory { get; init; } = "";
    public string OptiScalerVariant { get; init; } = "";
    public string OptiScalerVersion { get; init; } = "";
    public string OptiScalerDisplayVersion { get; init; } = "";
    public string GpuVendor { get; init; } = "";
    public string GpuName { get; init; } = "";
    public string GpuBundleKey { get; init; } = "";
    public string GpuGroup { get; init; } = "";
    public string Fsr4SourceArchive { get; init; } = "";
    public bool Fsr4Required { get; init; }
    public bool UseUltimateAsiLoader { get; init; }
    public IReadOnlyList<string> UalDetectedNames { get; init; } = Array.Empty<string>();
    public IReadOnlyDictionary<string, object?> ModuleDownloadLinks { get; init; } = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
    public string UalCachedArchivePath { get; init; } = "";
    public string OptiPatcherCachedArchivePath { get; init; } = "";
    public string SpecialKCachedArchivePath { get; init; } = "";
    public string ReFrameworkCachedArchivePath { get; init; } = "";
    public string Unreal5CachedArchivePath { get; init; } = "";
}
