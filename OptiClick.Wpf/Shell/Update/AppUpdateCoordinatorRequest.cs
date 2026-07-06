using OptiClick.Core.Runtime;
using OptiClick.Wpf.Shell.RuntimeData;

namespace OptiClick.Wpf.Shell.Update;

public sealed record AppUpdateCoordinatorRequest
{
    public AppUpdateTrigger Trigger { get; init; } = AppUpdateTrigger.Manual;
    public RemoteRuntimeData LatestRuntimeData { get; init; } = RemoteRuntimeData.Empty;
    public string CurrentVersion { get; init; } = "";
    public required AppUpdateCoordinatorText Text { get; init; }
    public required AppLanguage Language { get; init; }
    public bool IsAppUpdateInProgress { get; init; }
}
