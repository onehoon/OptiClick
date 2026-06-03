namespace OptiClick.Wpf.Shell.Runtime;

public sealed record DeviceIdentityRulesFlowResult
{
    public bool DidRun { get; init; }
    public bool IsSuccess { get; init; }
    public string ErrorCode { get; init; } = "";
    public IReadOnlyList<RuntimeFlowLogEntry> Logs { get; init; } = [];
}
