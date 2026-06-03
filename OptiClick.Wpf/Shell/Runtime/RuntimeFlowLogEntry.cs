using OptiClick.Wpf.Shell.Flow;

namespace OptiClick.Wpf.Shell.Runtime;

public sealed record RuntimeFlowLogEntry : IFlowLogEntry
{
    public string Level { get; init; } = "info";
    public string Category { get; init; } = "runtime";
    public string Message { get; init; } = "";
    public Exception? Exception { get; init; }
}
