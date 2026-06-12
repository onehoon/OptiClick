using OptiClick.Wpf.Install.Planning;
using OptiClick.Wpf.Shell.Selection;

namespace OptiClick.Wpf.Install.Flow;

public static class InstallFlowSelectionSnapshotMapper
{
    public static InstallFlowSelectionSnapshot FromSelectionState(ShellInstallSelectionState? selectionState)
    {
        selectionState ??= new ShellInstallSelectionState();
        return new InstallFlowSelectionSnapshot
        {
            ActionAvailabilitySnapshot = InstallPlanSnapshotMapper.FromShellActionAvailability(selectionState.ActionAvailability),
            Precheck = selectionState.PrecheckSnapshot,
            MultiGpuBlocked = selectionState.MultiGpuBlocked,
            GpuSelectionPending = selectionState.GpuSelectionPending,
            SheetLoading = selectionState.SheetLoading,
            SheetReady = selectionState.SheetReady,
            PopupConfirmed = selectionState.PopupConfirmed,
            MatchSnapshot = InstallPlanSnapshotMapper.FromShellMatch(selectionState.SelectedMatchResult),
            ActionAvailabilityReasonCode = selectionState.ActionAvailabilityReasonCode,
            PendingPopupRequestCount = selectionState.PendingPopupRequests.Count
        };
    }
}
