using OptiClick.Core.Runtime;

namespace OptiClick.Wpf.Services;

public sealed record AppUpdateFlowRequest
{
    public required AppUpdateFlowText Text { get; init; }
    public required AppLanguage Language { get; init; }
}

public sealed record AppUpdateConfirmedRequest
{
    public required AppUpdateInfo UpdateInfo { get; init; }
    public required AppUpdateFlowText Text { get; init; }
}
