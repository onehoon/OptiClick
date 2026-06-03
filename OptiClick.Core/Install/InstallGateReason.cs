namespace OptiClick.Core.Install;

public static class InstallGateReason
{
    public const string AppUpdate = "app_update";
    public const string SheetLoading = "sheet_loading";
    public const string GpuSelectionPending = "gpu_selection_pending";
    public const string UnsupportedGpu = "unsupported_gpu";
    public const string MultiGpuBlocked = "multi_gpu_blocked";
    public const string InstallInProgress = "install_in_progress";
    public const string PredownloadInProgress = "predownload_in_progress";
    public const string NoGameSelected = "no_game_selected";
    public const string OptiScalerDownloading = "optiscaler_downloading";
    public const string PrecheckRunning = "precheck_running";
    public const string PrecheckIncomplete = "precheck_incomplete";
    public const string OptiScalerNotReady = "optiscaler_not_ready";
    public const string InvalidSelection = "invalid_selection";
    public const string Fsr4Downloading = "fsr4_downloading";
    public const string Fsr4NotReady = "fsr4_not_ready";
    public const string PopupRequired = "popup_required";
}
