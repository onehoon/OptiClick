using OptiClick.Wpf.Shell.Flow;

namespace OptiClick.Wpf.Shell.Selection;

public sealed record GameSelectionFlowLogEntry : IFlowLogEntry
{
    public string Level { get; init; } = "info";
    public string Category { get; init; } = "selection";
    public string Message { get; init; } = "";
    public Exception? Exception { get; init; }
}
