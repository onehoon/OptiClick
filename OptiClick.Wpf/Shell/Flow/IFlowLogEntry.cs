namespace OptiClick.Wpf.Shell.Flow;

public interface IFlowLogEntry
{
    string Level { get; }
    string Category { get; }
    string Message { get; }
    Exception? Exception { get; }
}
