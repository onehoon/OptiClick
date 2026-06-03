using OptiClick.Wpf.Shell.Flow;

namespace OptiClick.Wpf.Install.Flow;

public sealed record InstallFlowLogEntry : IFlowLogEntry
{
    public string Level { get; init; } = "info";
    public string Category { get; init; } = "install";
    public string Message { get; init; } = "";
    public Exception? Exception { get; init; }
}
