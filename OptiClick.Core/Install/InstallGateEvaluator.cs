using OptiClick.Core.Models;

namespace OptiClick.Core.Install;

public sealed class InstallGateEvaluator
{
    public InstallGateResult Evaluate(InstallGateState state)
    {
        var uiReasons = new List<string>();
        var workerReasons = new List<string>();

        AddIf(uiReasons, state.AppUpdateAvailable, InstallGateReason.AppUpdate);
        AddIf(uiReasons, state.SheetLoading, InstallGateReason.SheetLoading);
        AddIf(uiReasons, state.GpuSelectionPending, InstallGateReason.GpuSelectionPending);
        AddIf(uiReasons, !state.GpuSupported, InstallGateReason.UnsupportedGpu);

        AddIf(workerReasons, state.MultiGpuBlocked, InstallGateReason.MultiGpuBlocked);
        AddIf(workerReasons, state.InstallInProgress, InstallGateReason.InstallInProgress);
        AddIf(workerReasons, state.PredownloadInProgress, InstallGateReason.PredownloadInProgress);
        AddIf(workerReasons, !state.HasSelectedGame, InstallGateReason.NoGameSelected);
        AddIf(workerReasons, state.OptiScalerDownloading, InstallGateReason.OptiScalerDownloading);
        AddIf(workerReasons, state.PrecheckRunning, InstallGateReason.PrecheckRunning);
        AddIf(workerReasons, state.PrecheckIncomplete, InstallGateReason.PrecheckIncomplete);
        AddIf(workerReasons, !state.OptiScalerReady, InstallGateReason.OptiScalerNotReady);
        AddIf(workerReasons, state.InvalidSelection, InstallGateReason.InvalidSelection);
        AddIf(workerReasons, state.PopupRequired, InstallGateReason.PopupRequired);

        var blockingReason = workerReasons.FirstOrDefault() ?? uiReasons.FirstOrDefault() ?? "";
        return new InstallGateResult
        {
            UiBlocked = uiReasons.Count > 0 || workerReasons.Count > 0,
            WorkerBlocked = workerReasons.Count > 0,
            BlockingReason = blockingReason,
            RequiresPopupConfirmation = state.PopupRequired,
            UiReasons = uiReasons,
            WorkerReasons = workerReasons
        };
    }

    private static void AddIf(ICollection<string> reasons, bool condition, string reason)
    {
        if (condition)
        {
            reasons.Add(reason);
        }
    }
}
