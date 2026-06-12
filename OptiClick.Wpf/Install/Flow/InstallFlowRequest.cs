using OptiClick.Core.Install.Flow;

namespace OptiClick.Wpf.Install.Flow;

public sealed record InstallFlowRequest
{
    public required InstallExecutionContext ExecutionContext { get; init; }
    public required InstallFlowText Text { get; init; }
    public string InstallPostPopupMessage { get; init; } = "";
}
