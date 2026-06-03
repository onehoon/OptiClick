namespace OptiClick.Wpf.Install.UiState;

public static class InstallButtonReasonCodes
{
    public const string UnsupportedOs = "unsupported_os";
    public const string MultiGpuBlocked = "multi_gpu_blocked";
    public const string GpuSelectionPending = "gpu_selection_pending";
    public const string SheetLoading = "sheet_loading";
    public const string SheetNotReady = "sheet_not_ready";
    public const string InstallInProgress = "install_in_progress";
    public const string AppUpdateInProgress = "app_update_in_progress";
    public const string NoGameSelected = "no_game_selected";
    public const string InstallPrecheckRunning = "install_precheck_running";
    public const string PrecheckIncomplete = "precheck_incomplete";
    public const string OptiScalerArchiveDownloading = "optiscaler_archive_downloading";
    public const string OptiScalerArchiveNotReady = "optiscaler_archive_not_ready";
    public const string Fsr4NotReady = "fsr4_not_ready";
    public const string AllArchivesNotReady = "all_archives_not_ready";
    public const string UnsupportedGpu = "unsupported_gpu";
    public const string DisabledGame = "disabled_game";
    public const string FinalProxyMissing = "final_proxy_missing";
    public const string ProxyChainUnresolved = "proxy_chain_unresolved";
    public const string InvalidInstallPlan = "invalid_install_plan";
    public const string WritePermissionDenied = "write_permission_denied";
    public const string ConfirmPopupRequired = "confirm_popup_required";
}

public static class InstallEntryRejectionCodes
{
    public const string UnsupportedOs = "unsupported_os";
    public const string MultiGpuBlocked = "multi_gpu_blocked";
    public const string InstallInProgress = "install_in_progress";
    public const string PredownloadInProgress = "predownload_in_progress";
    public const string NoGameSelected = "no_game_selected";
    public const string InvalidMatch = "invalid_match";
    public const string InvalidTargetFolder = "invalid_target_folder";
    public const string OptiScalerArchiveDownloading = "optiscaler_archive_downloading";
    public const string InstallPrecheckRunning = "install_precheck_running";
    public const string PrecheckIncomplete = "precheck_incomplete";
    public const string OptiScalerArchiveNotReady = "optiscaler_archive_not_ready";
    public const string InvalidGameSelection = "invalid_game_selection";
    public const string Fsr4ArchiveDownloading = "fsr4_archive_downloading";
    public const string Fsr4NotReady = "fsr4_not_ready";
    public const string DisabledGame = "disabled_game";
    public const string FinalProxyMissing = "final_proxy_missing";
    public const string ProxyChainUnresolved = "proxy_chain_unresolved";
    public const string InvalidInstallPlan = "invalid_install_plan";
    public const string WritePermissionDenied = "write_permission_denied";
    public const string ConfirmPopupRequired = "confirm_popup_required";
    public const string WriteProbeFailed = "write_probe_failed";
}

public static class InstallStatusCodes
{
    public const string Installable = "installable";
    public const string UpdateAvailable = "update_available";
    public const string Latest = "latest";
    public const string PreRelease = "pre_release";
    public const string NeedsReview = "needs_review";
}
