using OptiClick.Wpf.Models;
using OptiClick.Wpf.Shell.Flow;
using OptiClick.Wpf.ViewModels;

namespace OptiClick.Wpf.Shell.Scan;

public sealed record ScanFlowLogEntry : IFlowLogEntry
{
    public string Level { get; init; } = "info";
    public string Category { get; init; } = "scan";
    public string Message { get; init; } = "";
    public Exception? Exception { get; init; }
}

public sealed record ScanFlowResult
{
    public bool DidRun { get; init; }
    public bool ShouldNavigateHome { get; init; }
    public bool ShouldRecomputeSelection { get; init; }
    public string StatusText { get; init; } = "";
    public IReadOnlyList<GameCardViewModel> VisibleGames { get; init; } = [];
    public IReadOnlyDictionary<string, ShellGameMatchResult> MatchByGameId { get; init; }
        = new Dictionary<string, ShellGameMatchResult>(StringComparer.OrdinalIgnoreCase);
    public IReadOnlyDictionary<string, string> TargetPathByGameId { get; init; }
        = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    public AppDialogRequest? DialogRequest { get; init; }
    public ScanExecutionSummary Summary { get; init; } = new();
    public IReadOnlyList<ScanFlowLogEntry> Logs { get; init; } = [];
}
