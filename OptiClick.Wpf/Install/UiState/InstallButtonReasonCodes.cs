using OptiClick.Wpf.Install.Gates;

namespace OptiClick.Wpf.Install.UiState;

public static class InstallButtonReasonCodes
{
    public const string UnsupportedOs = InstallStartGateReasonCodes.UnsupportedOs;
    public const string MultiGpuBlocked = InstallStartGateReasonCodes.MultiGpuBlocked;
    public const string GpuSelectionPending = InstallStartGateReasonCodes.GpuSelectionPending;
    public const string SheetLoading = InstallStartGateReasonCodes.SheetLoading;
    public const string SheetNotReady = InstallStartGateReasonCodes.SheetNotReady;
    public const string InstallInProgress = InstallStartGateReasonCodes.InstallInProgress;
    public const string AppUpdateInProgress = InstallStartGateReasonCodes.AppUpdateInProgress;
    public const string NoGameSelected = InstallStartGateReasonCodes.NoGameSelected;
    public const string InstallPrecheckRunning = InstallStartGateReasonCodes.InstallPrecheckRunning;
    public const string PrecheckIncomplete = InstallStartGateReasonCodes.PrecheckIncomplete;
    public const string OptiScalerArchiveDownloading = InstallStartGateReasonCodes.OptiScalerArchiveDownloading;
    public const string OptiScalerArchiveNotReady = InstallStartGateReasonCodes.OptiScalerArchiveNotReady;
    public const string AllArchivesNotReady = "all_archives_not_ready";
    public const string UnsupportedGpu = InstallStartGateReasonCodes.UnsupportedGpu;
    public const string DisabledGame = InstallStartGateReasonCodes.DisabledGame;
    public const string FinalProxyMissing = InstallStartGateReasonCodes.FinalProxyMissing;
    public const string ProxyChainUnresolved = InstallStartGateReasonCodes.ProxyChainUnresolved;
    public const string InvalidInstallPlan = InstallStartGateReasonCodes.InvalidInstallPlan;
    public const string WritePermissionDenied = "write_permission_denied";
    public const string ConfirmPopupRequired = InstallStartGateReasonCodes.ConfirmPopupRequired;
}

public static class InstallEntryRejectionCodes
{
    public const string UnsupportedOs = InstallStartGateReasonCodes.UnsupportedOs;
    public const string MultiGpuBlocked = InstallStartGateReasonCodes.MultiGpuBlocked;
    public const string InstallInProgress = InstallStartGateReasonCodes.InstallInProgress;
    public const string PredownloadInProgress = InstallStartGateReasonCodes.PredownloadInProgress;
    public const string NoGameSelected = InstallStartGateReasonCodes.NoGameSelected;
    public const string InvalidMatch = InstallStartGateReasonCodes.InvalidMatch;
    public const string InvalidTargetFolder = InstallStartGateReasonCodes.InvalidTargetFolder;
    public const string OptiScalerArchiveDownloading = InstallStartGateReasonCodes.OptiScalerArchiveDownloading;
    public const string InstallPrecheckRunning = InstallStartGateReasonCodes.InstallPrecheckRunning;
    public const string PrecheckIncomplete = InstallStartGateReasonCodes.PrecheckIncomplete;
    public const string OptiScalerArchiveNotReady = InstallStartGateReasonCodes.OptiScalerArchiveNotReady;
    public const string InvalidGameSelection = "invalid_game_selection";
    public const string DisabledGame = InstallStartGateReasonCodes.DisabledGame;
    public const string FinalProxyMissing = InstallStartGateReasonCodes.FinalProxyMissing;
    public const string ProxyChainUnresolved = InstallStartGateReasonCodes.ProxyChainUnresolved;
    public const string InvalidInstallPlan = InstallStartGateReasonCodes.InvalidInstallPlan;
    public const string WritePermissionDenied = "write_permission_denied";
    public const string ConfirmPopupRequired = InstallStartGateReasonCodes.ConfirmPopupRequired;
    public const string InstallExecutionUnavailable = "install_execution_unavailable";
}

public static class InstallStatusCodes
{
    public const string Installable = "installable";
    public const string UpdateAvailable = "update_available";
    public const string Latest = "latest";
    public const string PreRelease = "pre_release";
    public const string NeedsReview = "needs_review";
}

public static class InstallStatusBadgeCodes
{
    public const string Installable = InstallStatusCodes.Installable;
    public const string UpdateAvailable = InstallStatusCodes.UpdateAvailable;
    public const string Latest = InstallStatusCodes.Latest;
    public const string StableInstalled = "stable_installed";
    public const string PreviewInstalled = "preview_installed";
    public const string InstalledVersion = "installed_version";
    public const string NeedsReview = InstallStatusCodes.NeedsReview;
}
