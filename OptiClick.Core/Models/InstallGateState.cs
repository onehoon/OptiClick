namespace OptiClick.Core.Models;

public sealed record InstallGateState
{
    public bool AppUpdateAvailable { get; init; }
    public bool SheetLoading { get; init; }
    public bool GpuSelectionPending { get; init; }
    public bool GpuSupported { get; init; } = true;
    public bool MultiGpuBlocked { get; init; }
    public bool InstallInProgress { get; init; }
    public bool PredownloadInProgress { get; init; }
    public bool HasSelectedGame { get; init; } = true;
    public bool OptiScalerDownloading { get; init; }
    public bool PrecheckRunning { get; init; }
    public bool PrecheckIncomplete { get; init; }
    public bool OptiScalerReady { get; init; } = true;
    public bool InvalidSelection { get; init; }
    public bool PopupRequired { get; init; }
}
