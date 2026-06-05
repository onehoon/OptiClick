using OptiClick.Core.Runtime;
using OptiClick.Wpf.Shell.Games;
using OptiClick.Wpf.Shell.Games.Actions;
using OptiClick.Wpf.Shell.Scan;
using OptiClick.Wpf.Shell.RuntimeData;

namespace OptiClick.Wpf.Install.Planning;

public enum InstallPlanStepType
{
    ValidateGate,
    PrepareArchives,
    BuildComponents,
    BuildFileOperations,
    BuildConfigEdits,
    FinalizeSummary
}

public enum InstallPlanComponentType
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

public enum InstallPlanFileOperationType
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

public enum InstallPlanConfigEditType
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

public sealed record InstallPlanStep
{
    public InstallPlanStepType Type { get; init; }
    public bool Completed { get; init; }
    public string Note { get; init; } = "";
}

public sealed record InstallPlanBlockReason
{
    public string Code { get; init; } = "";
    public string Detail { get; init; } = "";
}

public sealed record InstallPlanWarning
{
    public string Code { get; init; } = "";
    public string Detail { get; init; } = "";
    public bool IsBlockingCandidate { get; init; }
}

public sealed record InstallPlanComponent
{
    public InstallPlanComponentType Type { get; init; }
    public bool Enabled { get; init; }
    public string SkipReason { get; init; } = "";
    public string SourceKind { get; init; } = "";
    public string DestinationHint { get; init; } = "";
    public string RequiredArchiveAlias { get; init; } = "";
}

public sealed record InstallPlanFileOperation
{
    public InstallPlanFileOperationType Type { get; init; }
    public string SourcePathHint { get; init; } = "";
    public string DestinationPathHint { get; init; } = "";
    public InstallPlanComponentType Component { get; init; } = InstallPlanComponentType.OptiScalerCore;
    public bool IsDestructive { get; init; }
    public bool RequiresExistingFileSnapshot { get; init; }
    public string Notes { get; init; } = "";
}

public sealed record InstallPlanConfigEdit
{
    public InstallPlanConfigEditType Type { get; init; }
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

public sealed record InstallPlanSummary
{
    public static readonly InstallPlanSummary Empty = new();

    public string OptiScalerTargetDll { get; init; } = "";
    public IReadOnlyList<string> SelectedComponents { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> WarningCodes { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Notes { get; init; } = Array.Empty<string>();
}

public sealed record InstallPlan
{
    public bool IsAllowed { get; init; }
    public IReadOnlyList<InstallPlanBlockReason> BlockReasons { get; init; } = Array.Empty<InstallPlanBlockReason>();
    public IReadOnlyList<InstallPlanWarning> Warnings { get; init; } = Array.Empty<InstallPlanWarning>();
    public string GameId { get; init; } = "";
    public string GameDisplayName { get; init; } = "";
    public string TargetFolder { get; init; } = "";
    public string MatchedExe { get; init; } = "";
    public string FinalProxyDllName { get; init; } = "";
    public IReadOnlyList<string> ExcludeListPatterns { get; init; } = Array.Empty<string>();
    public IReadOnlyList<InstallPlanComponent> Components { get; init; } = Array.Empty<InstallPlanComponent>();
    public IReadOnlyList<InstallPlanFileOperation> FileOperations { get; init; } = Array.Empty<InstallPlanFileOperation>();
    public IReadOnlyList<InstallPlanConfigEdit> ConfigEdits { get; init; } = Array.Empty<InstallPlanConfigEdit>();
    public IReadOnlyList<InstallPlanStep> Steps { get; init; } = Array.Empty<InstallPlanStep>();
    public InstallPlanSummary Summary { get; init; } = InstallPlanSummary.Empty;
    public AttachedRuntimeProfileRows ProfileRows { get; init; } = AttachedRuntimeProfileRows.Empty;
}

public sealed record InstallPlanBuildResult
{
    public static InstallPlanBuildResult Success(InstallPlan plan)
    {
        return new InstallPlanBuildResult
        {
            IsSuccess = true,
            Plan = plan
        };
    }

    public static InstallPlanBuildResult Failure(InstallPlan plan, string errorCode, string errorDetail = "")
    {
        return new InstallPlanBuildResult
        {
            IsSuccess = false,
            Plan = plan,
            ErrorCode = errorCode,
            ErrorDetail = errorDetail
        };
    }

    public bool IsSuccess { get; init; }
    public InstallPlan Plan { get; init; } = new();
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
}

public sealed record InstallConfigProfileHint
{
    public InstallPlanConfigEditType Type { get; init; }
    public string TargetPathHint { get; init; } = "";
    public string KeyHint { get; init; } = "";
    public string ValuePathHint { get; init; } = "";
}

public sealed record InstallPlanBuildInput
{
    public ShellGameCardModel? SelectedGame { get; init; }
    public ShellGameMatchResult? MatchResult { get; init; }
    public RuntimeContext RuntimeContext { get; init; } = new();
    public ShellGameActionAvailability ActionAvailability { get; init; } = new();
    public ArchiveReadinessSnapshot ArchiveReadiness { get; init; } = ArchiveReadinessSnapshot.NotReady;
    public InstallPrecheckSnapshot Precheck { get; init; } = InstallPrecheckSnapshot.NotStarted;
    public IReadOnlyList<InstallConfigProfileHint> ConfigProfiles { get; init; } = Array.Empty<InstallConfigProfileHint>();
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
    public bool IsWriteProbeFailed { get; init; }
    public bool IsFsr4Required { get; init; }
}
