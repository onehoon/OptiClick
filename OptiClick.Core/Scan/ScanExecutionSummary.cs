namespace OptiClick.Core.Scan;

public sealed class ScanExecutionSummary
{
    public int ExecutableCount { get; init; }
    public int CandidateExecutableCount { get; init; }
    public int MatchedCount { get; init; }
    public int MultipleCandidateCount { get; init; }
    public int DisabledCount { get; init; }
    public int UnsupportedCount { get; init; }
    public int UnmatchedCount { get; init; }
    public int DuplicateMatchCount { get; init; }
    public int VisibleGameCount { get; init; }
    public int SkippedDirectoryCount { get; init; }
}
