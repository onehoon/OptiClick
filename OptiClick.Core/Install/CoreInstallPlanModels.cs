using OptiClick.Core.Install;

namespace OptiClick.Core.Install.Planning;

public enum CoreInstallPlanStepType
{
    ValidateGate,
    PrepareArchives,
    BuildComponents,
    BuildFileOperations,
    BuildConfigEdits,
    FinalizeSummary
}

public enum CoreInstallPlanComponentType
{
    OptiScalerCore,
    OptiPatcher,
    REFramework,
    SpecialK,
    UltimateAsiLoader,
    Unreal5,
    ExtraBundle,
    Fsr4,
    RtssProfile
}

public enum CoreInstallPlanFileOperationType
{
    BackupManagedOptiScalerDll,
    RemoveLegacyOptiScalerFile,
    CopyPayloadTree,
    RenameOptiScalerDll,
    CopyComponentFile,
    ExtractArchive,
    RemoveLegacyComponentFile,
    EnsureDirectory,
    SetWritable
}

public enum CoreInstallPlanConfigEditType
{
    GameIniProfile,
    GameUnrealIniProfile,
    GameXmlProfile,
    GameJsonProfile,
    EngineIniProfile,
    RegistryProfile
}

public enum ArchiveReadinessState
{
    NotReady,
    Downloading,
    Ready,
    MissingSource,
    Failed
}

public enum InstallPrecheckState
{
    NotStarted,
    Running,
    Passed,
    Failed
}

public sealed record CoreInstallPlanStep
{
    public CoreInstallPlanStepType Type { get; init; }
    public bool Completed { get; init; }
    public string Note { get; init; } = "";
}

public sealed record CoreInstallPlanBlockReason
{
    public string Code { get; init; } = "";
    public string Detail { get; init; } = "";
}

public sealed record CoreInstallPlanWarning
{
    public string Code { get; init; } = "";
    public string Detail { get; init; } = "";
    public bool IsBlockingCandidate { get; init; }
}

public sealed record CoreInstallPlanComponent
{
    public CoreInstallPlanComponentType Type { get; init; }
    public bool Enabled { get; init; }
    public string SkipReason { get; init; } = "";
    public string SourceKind { get; init; } = "";
    public string DestinationHint { get; init; } = "";
    public string RequiredArchiveAlias { get; init; } = "";
}

public sealed record CoreInstallPlanFileOperation
{
    public CoreInstallPlanFileOperationType Type { get; init; }
    public string SourcePathHint { get; init; } = "";
    public string DestinationPathHint { get; init; } = "";
    public CoreInstallPlanComponentType Component { get; init; } = CoreInstallPlanComponentType.OptiScalerCore;
    public bool IsDestructive { get; init; }
    public bool RequiresExistingFileSnapshot { get; init; }
    public string Notes { get; init; } = "";
}

public sealed record CoreInstallPlanConfigEdit
{
    public CoreInstallPlanConfigEditType Type { get; init; }
    public string TargetPathHint { get; init; } = "";
    public string KeyHint { get; init; } = "";
    public string ValuePathHint { get; init; } = "";
    public bool CreatesFileAllowed { get; init; }
    public bool CreatesMissingPathAllowed { get; init; }
    public bool AllowsAddMissingKey { get; init; }
    public bool AllowsAddMissingSection { get; init; }
    public bool BestEffort { get; init; }
    public string Notes { get; init; } = "";
}

public sealed record CoreInstallPlanSummary
{
    public static readonly CoreInstallPlanSummary Empty = new();

    public string OptiScalerTargetDll { get; init; } = "";
    public IReadOnlyList<string> SelectedComponents { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> WarningCodes { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Notes { get; init; } = Array.Empty<string>();
}

public sealed record CoreInstallPlan
{
    public bool IsAllowed { get; init; }
    public IReadOnlyList<CoreInstallPlanBlockReason> BlockReasons { get; init; } = Array.Empty<CoreInstallPlanBlockReason>();
    public IReadOnlyList<CoreInstallPlanWarning> Warnings { get; init; } = Array.Empty<CoreInstallPlanWarning>();
    public string GameId { get; init; } = "";
    public string GameDisplayName { get; init; } = "";
    public string TargetFolder { get; init; } = "";
    public string MatchedExe { get; init; } = "";
    public string FinalProxyDllName { get; init; } = "";
    public IReadOnlyList<string> ExcludeListPatterns { get; init; } = Array.Empty<string>();
    public IReadOnlyList<CoreInstallPlanComponent> Components { get; init; } = Array.Empty<CoreInstallPlanComponent>();
    public IReadOnlyList<CoreInstallPlanFileOperation> FileOperations { get; init; } = Array.Empty<CoreInstallPlanFileOperation>();
    public IReadOnlyList<CoreInstallPlanConfigEdit> ConfigEdits { get; init; } = Array.Empty<CoreInstallPlanConfigEdit>();
    public IReadOnlyList<CoreInstallPlanStep> Steps { get; init; } = Array.Empty<CoreInstallPlanStep>();
    public CoreInstallPlanSummary Summary { get; init; } = CoreInstallPlanSummary.Empty;
}

public sealed record CoreInstallPlanTargets
{
    public static readonly CoreInstallPlanTargets Empty = new();

    public string GameDisplayName { get; init; } = "";
    public string TargetFolder { get; init; } = "";
    public string MatchedExe { get; init; } = "";
    public string FinalProxyDllName { get; init; } = "";
    public IReadOnlyList<string> ExcludeListPatterns { get; init; } = Array.Empty<string>();
}

public sealed record CoreInstallPlanBuildResult
{
    public static CoreInstallPlanBuildResult Success(CoreInstallPlan plan)
    {
        return new CoreInstallPlanBuildResult
        {
            IsSuccess = true,
            Plan = plan
        };
    }

    public static CoreInstallPlanBuildResult Failure(CoreInstallPlan plan, string errorCode, string errorDetail = "")
    {
        return new CoreInstallPlanBuildResult
        {
            IsSuccess = false,
            Plan = plan,
            ErrorCode = errorCode,
            ErrorDetail = errorDetail
        };
    }

    public bool IsSuccess { get; init; }
    public CoreInstallPlan Plan { get; init; } = new();
    public string ErrorCode { get; init; } = "";
    public string ErrorDetail { get; init; } = "";
}

public sealed record InstallPrecheckFinding
{
    public string Kind { get; init; } = "";
    public string Message { get; init; } = "";
    public IReadOnlyList<string> Evidence { get; init; } = Array.Empty<string>();
    public bool IsBlocking { get; init; }
}

public sealed record InstallPrecheckSnapshot
{
    public static readonly InstallPrecheckSnapshot NotStarted = new()
    {
        State = InstallPrecheckState.NotStarted
    };

    public InstallPrecheckState State { get; init; } = InstallPrecheckState.NotStarted;
    public string ErrorText { get; init; } = "";
    public string ResolvedDllName { get; init; } = "";
    public IReadOnlyList<InstallPrecheckFinding> Findings { get; init; } = Array.Empty<InstallPrecheckFinding>();
}

public sealed record ArchiveReadinessSnapshot
{
    public static readonly ArchiveReadinessSnapshot NotReady = new()
    {
        OptiScalerState = ArchiveReadinessState.NotReady,
        Fsr4State = ArchiveReadinessState.NotReady,
        UalState = ArchiveReadinessState.NotReady,
        OptiPatcherState = ArchiveReadinessState.NotReady,
        SpecialKState = ArchiveReadinessState.NotReady,
        ReframeworkState = ArchiveReadinessState.NotReady,
        Unreal5State = ArchiveReadinessState.NotReady
    };

    public ArchiveReadinessState OptiScalerState { get; init; } = ArchiveReadinessState.NotReady;
    public string OptiScalerSourceArchive { get; init; } = "";
    public string OptiScalerVariant { get; init; } = "";
    public string OptiScalerVersion { get; init; } = "";
    public string OptiScalerDisplayVersion { get; init; } = "";
    public ArchiveReadinessState Fsr4State { get; init; } = ArchiveReadinessState.NotReady;
    public string Fsr4SourceArchive { get; init; } = "";
    public IReadOnlyDictionary<string, Fsr4VariantReadiness> Fsr4Variants { get; init; } =
        new Dictionary<string, Fsr4VariantReadiness>(StringComparer.OrdinalIgnoreCase);
    public ArchiveReadinessState UalState { get; init; } = ArchiveReadinessState.NotReady;
    public string UalSourceArchive { get; init; } = "";
    public ArchiveReadinessState OptiPatcherState { get; init; } = ArchiveReadinessState.NotReady;
    public string OptiPatcherSourceArchive { get; init; } = "";
    public ArchiveReadinessState SpecialKState { get; init; } = ArchiveReadinessState.NotReady;
    public string SpecialKSourceArchive { get; init; } = "";
    public ArchiveReadinessState ReframeworkState { get; init; } = ArchiveReadinessState.NotReady;
    public string ReframeworkSourceArchive { get; init; } = "";
    public ArchiveReadinessState Unreal5State { get; init; } = ArchiveReadinessState.NotReady;
    public string Unreal5SourceArchive { get; init; } = "";

    public bool AreAllStartupArchivesReady()
    {
        return OptiScalerState == ArchiveReadinessState.Ready
               && Fsr4State == ArchiveReadinessState.Ready
               && OptiPatcherState == ArchiveReadinessState.Ready
               && SpecialKState == ArchiveReadinessState.Ready
               && ReframeworkState == ArchiveReadinessState.Ready
               && UalState == ArchiveReadinessState.Ready
               && Unreal5State == ArchiveReadinessState.Ready;
    }

    public ArchiveReadinessState ResolveFsr4VariantState(string variant)
    {
        var normalized = NormalizeFsr4Variant(variant);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return Fsr4State;
        }

        return Fsr4Variants.TryGetValue(normalized, out var readiness)
            ? readiness.State
            : ArchiveReadinessState.MissingSource;
    }

    public string ResolveFsr4VariantSourceArchive(string variant)
    {
        var normalized = NormalizeFsr4Variant(variant);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return Fsr4SourceArchive;
        }

        return Fsr4Variants.TryGetValue(normalized, out var readiness)
            ? readiness.SourceArchive
            : "";
    }

    private static string NormalizeFsr4Variant(string value)
    {
        return (value ?? "").Trim().ToLowerInvariant();
    }
}

public sealed record Fsr4VariantReadiness
{
    public string Variant { get; init; } = "";
    public ArchiveReadinessState State { get; init; } = ArchiveReadinessState.NotReady;
    public string SourceArchive { get; init; } = "";
}

public sealed record CoreInstallConfigProfileHint
{
    public CoreInstallPlanConfigEditType Type { get; init; }
    public string TargetPathHint { get; init; } = "";
    public string KeyHint { get; init; } = "";
    public string ValuePathHint { get; init; } = "";
}

public sealed record CoreInstallPlanBuildInput
{
    public InstallGameDescriptor? GameDescriptor { get; init; }
    public InstallGameMatchSnapshot? MatchSnapshot { get; init; }
    public InstallActionAvailabilitySnapshot ActionAvailability { get; init; } = new();
    public ArchiveReadinessSnapshot ArchiveReadiness { get; init; } = ArchiveReadinessSnapshot.NotReady;
    public InstallPrecheckSnapshot Precheck { get; init; } = InstallPrecheckSnapshot.NotStarted;
    public IReadOnlyList<CoreInstallConfigProfileHint> ConfigProfiles { get; init; } = Array.Empty<CoreInstallConfigProfileHint>();
    public IReadOnlyList<string> ExistingTargetFilesSnapshot { get; init; } = Array.Empty<string>();
    public string TargetFolderHint { get; init; } = "";
    public string MatchedExeHint { get; init; } = "";
    public bool IsInstallInProgress { get; init; }
    public bool IsPredownloadInProgress { get; init; }
    public bool IsMultiGpuBlocked { get; init; }
    public bool IsAppUpdateInProgress { get; init; }
    public bool IsSelectionPopupConfirmed { get; init; }
    public bool IsGpuSelectionPending { get; init; }
    public bool IsSheetLoading { get; init; }
    public bool IsSheetReady { get; init; } = true;
    public bool ShouldInstallFsr4 { get; init; }
    public string Fsr4Variant { get; init; } = "";
}
