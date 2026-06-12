using OptiClick.Core.Install;
using OptiClick.Wpf.Install.Execution;
using OptiClick.Wpf.Shell.RuntimeData;

namespace OptiClick.Wpf.Install.Planning;

public sealed record InstallPlanInputBuildContext
{
    public InstallExecutionDescriptor ExecutionDescriptor { get; init; } = InstallExecutionDescriptor.Empty;
    public AttachedRuntimeProfileRows ProfileRows { get; init; } = AttachedRuntimeProfileRows.Empty;
    public InstallActionAvailabilitySnapshot ActionAvailabilitySnapshot { get; init; } = new();
    public InstallPrecheckSnapshot Precheck { get; init; } = InstallPrecheckSnapshot.NotStarted;
    public bool IsMultiGpuBlocked { get; init; }
    public bool IsSelectionPopupConfirmed { get; init; }
    public bool IsGpuSelectionPending { get; init; }
    public ArchiveReadinessSnapshot LatestArchiveReadiness { get; init; } = ArchiveReadinessSnapshot.NotReady;
    public InstallGameMatchSnapshot? MatchSnapshot { get; init; }
    public string TargetFolderHint { get; init; } = "";
    public string MatchedExeHint { get; init; } = "";
    public bool IsSheetLoading { get; init; }
    public bool IsSheetReady { get; init; } = true;
    public bool IsInstallExecutionInProgress { get; init; }
    public bool IsAppUpdateInProgress { get; init; }
}
