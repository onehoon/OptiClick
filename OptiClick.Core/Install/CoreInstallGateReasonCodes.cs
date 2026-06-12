namespace OptiClick.Core.Install;

public static class CoreInstallGateReasonCodes
{
    public const string MultiGpuBlocked = "multi_gpu_blocked";
    public const string GpuSelectionPending = "gpu_selection_pending";
    public const string SheetLoading = "sheet_loading";
    public const string SheetNotReady = "sheet_not_ready";
    public const string InstallInProgress = "install_in_progress";
    public const string AppUpdateInProgress = "app_update_in_progress";
    public const string PredownloadInProgress = "predownload_in_progress";
    public const string NoGameSelected = "no_game_selected";
    public const string InstallPrecheckRunning = "install_precheck_running";
    public const string PrecheckIncomplete = "precheck_incomplete";
    public const string OptiScalerArchiveDownloading = "optiscaler_archive_downloading";
    public const string OptiScalerArchiveNotReady = "optiscaler_archive_not_ready";
    public const string Fsr4NotReady = "fsr4_not_ready";
    public const string UnsupportedGpu = "unsupported_gpu";
    public const string ConfirmPopupRequired = "confirm_popup_required";
}
