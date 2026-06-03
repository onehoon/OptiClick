using OptiClick.Wpf.Install.Flow;

namespace OptiClick.Wpf.Install.Config;

public sealed record ConfigApplyFlowResult
{
    public bool IsSuccess { get; init; } = true;
    public string FailureMessage { get; init; } = "";
    public string FailureCode { get; init; } = "";
    public IReadOnlyList<InstallFlowLogEntry> Logs { get; init; } = [];
}
