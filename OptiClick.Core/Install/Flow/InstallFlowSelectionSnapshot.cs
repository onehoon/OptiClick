using OptiClick.Core.Install.Planning;

namespace OptiClick.Core.Install.Flow;

public sealed record InstallFlowSelectionSnapshot
{
    public InstallActionAvailabilitySnapshot ActionAvailabilitySnapshot { get; init; } = new();
    public InstallPrecheckSnapshot Precheck { get; init; } = InstallPrecheckSnapshot.NotStarted;
    public bool MultiGpuBlocked { get; init; }
    public bool GpuSelectionPending { get; init; }
    public bool SheetLoading { get; init; }
    public bool SheetReady { get; init; }
    public bool PopupConfirmed { get; init; }
    public InstallGameMatchSnapshot? MatchSnapshot { get; init; }
    public string ActionAvailabilityReasonCode { get; init; } = "";
    public int PendingPopupRequestCount { get; init; }
    public bool HasValidMatch => MatchSnapshot?.State == InstallGameMatchState.Matched;
    public bool HasPendingPopupRequests => PendingPopupRequestCount > 0;
}
