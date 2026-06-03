namespace OptiClick.Wpf.Shell.Selection;

public sealed record GameSelectionFlowResult
{
    public bool DidRun { get; init; }
    public bool IsSuccess { get; init; }
    public bool IsStaleIgnored { get; init; }
    public ShellInstallSelectionState SelectionState { get; init; } = new();
    public IReadOnlyList<GameSelectionFlowLogEntry> Logs { get; init; } = [];
}
