using OptiClick.Wpf.Models;
using OptiClick.Wpf.Services;

namespace OptiClick.Wpf.Shell.Update;

public sealed record AppUpdateCoordinatorResult
{
    public bool ShouldContinue { get; init; }
    public bool ShouldShowDialog { get; init; }
    public bool IsUpdateAvailable { get; init; }
    public string StatusText { get; init; } = "";
    public string MissingUpdateInfoLogMessage { get; init; } = "";
    public AppDialogRequest? DialogRequest { get; init; }
    public AppUpdateInfo? UpdateInfo { get; init; }
    public IReadOnlyList<AppUpdateFlowLogEntry> Logs { get; init; } = [];

    public bool TryGetUpdateInfo(out AppUpdateInfo updateInfo)
    {
        if (IsUpdateAvailable && UpdateInfo is not null)
        {
            updateInfo = UpdateInfo;
            return true;
        }

        updateInfo = null!;
        return false;
    }
}
