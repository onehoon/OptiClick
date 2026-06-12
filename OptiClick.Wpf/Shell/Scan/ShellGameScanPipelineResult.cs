using OptiClick.Core.Scan;

namespace OptiClick.Wpf.Shell.Scan;

public sealed class ShellGameScanPipelineResult
{
    public IReadOnlyDictionary<string, ShellGameMatchResult> MatchByGameId { get; init; }
        = new Dictionary<string, ShellGameMatchResult>(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, string> TargetPathByGameId { get; init; }
        = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    public ScanExecutionSummary Summary { get; init; } = new();
    public IReadOnlyList<ShellGameMatchResult> RawMatches { get; init; } = [];
}
