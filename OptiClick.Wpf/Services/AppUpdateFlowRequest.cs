using OptiClick.Wpf.Localization;
using OptiClick.Wpf.Shell.RuntimeData;

namespace OptiClick.Wpf.Services;

public sealed record AppUpdateFlowRequest
{
    public required RemoteRuntimeData LatestRuntimeData { get; init; }
    public required string CurrentVersion { get; init; }
    public required AppStrings Strings { get; init; }
}

public sealed record AppUpdateConfirmedRequest
{
    public required AppUpdateInfo UpdateInfo { get; init; }
    public required AppStrings Strings { get; init; }
}
