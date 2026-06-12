using OptiClick.Wpf.Install.Planning;

namespace OptiClick.Wpf.Install.UiState;

public sealed record InstallButtonStateInputs
{
    public bool MultiGpuBlocked { get; init; }
    public bool GpuSelectionPending { get; init; }
    public bool SheetReady { get; init; }
    public bool SheetLoading { get; init; }
    public bool InstallInProgress { get; init; }
    public bool AppUpdateInProgress { get; init; }
    public bool HasValidGame { get; init; }
    public bool HasSupportedGpu { get; init; } = true;
    public bool InstallPrecheckRunning { get; init; }
    public bool InstallPrecheckOk { get; init; }
    public bool OptiScalerArchiveReady { get; init; }
    public bool OptiScalerArchiveDownloading { get; init; }
    public bool Fsr4Ready { get; init; } = true;
    public bool AllArchivesReady { get; init; }
    public bool GamePopupConfirmed { get; init; }
}

public sealed record InstallButtonState
{
    public bool Enabled { get; init; }
    public bool ShowInstalling { get; init; }
    public string ReasonCode { get; init; } = "";
}

public sealed record InstallButtonPresentation
{
    public bool IsEnabled { get; init; }
    public bool ShowInstalling { get; init; }
    public bool IsLoadingBlinkReason { get; init; }
    public string ReasonCode { get; init; } = "";
    public string Text { get; init; } = "";
}

public sealed record InstallEntryGateInputs
{
    public bool MultiGpuBlocked { get; init; }
    public bool InstallInProgress { get; init; }
    public bool PredownloadInProgress { get; init; }
    public int? SelectedGameIndex { get; init; }
    public int FoundGamesCount { get; init; }
    public bool OptiScalerArchiveDownloading { get; init; }
    public bool InstallPrecheckRunning { get; init; }
    public bool InstallPrecheckOk { get; init; }
    public string InstallPrecheckError { get; init; } = "";
    public string InstallPrecheckDllName { get; init; } = "";
    public bool OptiScalerArchiveReady { get; init; }
    public string OptiSourceArchive { get; init; } = "";
    public string OptiScalerArchiveError { get; init; } = "";
    public bool ShouldInstallFsr4 { get; init; }
    public bool Fsr4ArchiveDownloading { get; init; }
    public bool Fsr4ArchiveReady { get; init; }
    public string Fsr4SourceArchive { get; init; } = "";
    public string Fsr4ArchiveError { get; init; } = "";
    public bool GamePopupConfirmed { get; init; }
}

public sealed record InstallEntryGateDecision
{
    public bool Ok { get; init; }
    public string Code { get; init; } = "";
    public string Detail { get; init; } = "";
}

public enum PopupPresentationKind
{
    None,
    Info,
    Warning,
    Error
}

public sealed record PopupPresentationRequest
{
    public PopupPresentationKind Kind { get; init; }
    public string TitleKey { get; init; } = "";
    public string BodyKey { get; init; } = "";
    public string BodyDetail { get; init; } = "";
    public string ReasonCode { get; init; } = "";
}

public sealed record InstallResultPresentationInput
{
    public bool Success { get; init; }
    public string Message { get; init; } = "";
}

public sealed record InstallResultPresentation
{
    public bool ClearInstallInProgress { get; init; }
    public bool ShouldUpdateButtonState { get; init; }
    public bool ShouldUpdateCardInstallStatus { get; init; }
    public bool ShouldUpdateInstallSummary { get; init; }
    public PopupPresentationRequest PopupRequest { get; init; } = new();
}

public sealed record InstallUiStateBuildInput
{
    public bool MultiGpuBlocked { get; init; }
    public bool GpuSelectionPending { get; init; }
    public bool SheetReady { get; init; }
    public bool SheetLoading { get; init; }
    public bool InstallInProgress { get; init; }
    public bool AppUpdateInProgress { get; init; }
    public bool PopupConfirmed { get; init; }
    public bool ShouldInstallFsr4 { get; init; }
    public ArchiveReadinessSnapshot ArchiveReadiness { get; init; } = ArchiveReadinessSnapshot.NotReady;
    public InstallPrecheckSnapshot Precheck { get; init; } = InstallPrecheckSnapshot.NotStarted;
    public string ActionAvailabilityReasonCode { get; init; } = "";
    public bool HasSelectedGame { get; init; }
}
