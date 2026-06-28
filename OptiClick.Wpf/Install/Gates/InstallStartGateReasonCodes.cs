using OptiClick.Core.Install;

namespace OptiClick.Wpf.Install.Gates;

public static class InstallStartGateReasonCodes
{
    public const string UnsupportedOs = CoreInstallStartGateReasonCodes.UnsupportedOs;
    public const string MultiGpuBlocked = CoreInstallGateReasonCodes.MultiGpuBlocked;
    public const string GpuSelectionPending = CoreInstallGateReasonCodes.GpuSelectionPending;
    public const string SheetLoading = CoreInstallGateReasonCodes.SheetLoading;
    public const string SheetNotReady = CoreInstallGateReasonCodes.SheetNotReady;
    public const string InstallInProgress = CoreInstallGateReasonCodes.InstallInProgress;
    public const string AppUpdateInProgress = CoreInstallGateReasonCodes.AppUpdateInProgress;
    public const string PredownloadInProgress = CoreInstallGateReasonCodes.PredownloadInProgress;
    public const string NoGameSelected = CoreInstallGateReasonCodes.NoGameSelected;
    public const string InvalidMatch = CoreInstallStartGateReasonCodes.InvalidMatch;
    public const string InvalidTargetFolder = CoreInstallStartGateReasonCodes.InvalidTargetFolder;
    public const string InstallPrecheckRunning = CoreInstallGateReasonCodes.InstallPrecheckRunning;
    public const string PrecheckIncomplete = CoreInstallGateReasonCodes.PrecheckIncomplete;
    public const string OptiScalerArchiveDownloading = CoreInstallGateReasonCodes.OptiScalerArchiveDownloading;
    public const string OptiScalerArchiveNotReady = CoreInstallGateReasonCodes.OptiScalerArchiveNotReady;
    public const string ComponentArchiveNotReady = CoreInstallStartGateReasonCodes.ComponentArchiveNotReady;
    public const string UnsupportedGpu = CoreInstallGateReasonCodes.UnsupportedGpu;
    public const string DisabledGame = CoreInstallStartGateReasonCodes.DisabledGame;
    public const string FinalProxyMissing = CoreInstallStartGateReasonCodes.FinalProxyMissing;
    public const string ProxyChainUnresolved = CoreInstallStartGateReasonCodes.ProxyChainUnresolved;
    public const string InvalidInstallPlan = CoreInstallStartGateReasonCodes.InvalidInstallPlan;
    public const string ConfirmPopupRequired = CoreInstallGateReasonCodes.ConfirmPopupRequired;
    public const string WritePermissionDenied = "write_permission_denied";
    public const string Ready = CoreInstallStartGateReasonCodes.Ready;
}

