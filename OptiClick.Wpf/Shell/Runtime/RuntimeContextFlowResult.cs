using OptiClick.Core.Runtime;

namespace OptiClick.Wpf.Shell.Runtime;

public sealed record RuntimeContextFlowResult
{
    public bool IsSuccess { get; init; }
    public RuntimeContext Context { get; init; } = new();
    public IReadOnlyList<RuntimeFlowLogEntry> Logs { get; init; } = [];
}
