namespace OptiClick.Wpf.Shell.Scan;

public enum ScanFolderAddOutcome
{
    Added,
    Cancelled,
    Missing,
    NormalizeFailed,
    Duplicate,
    BlockedBroadPath
}

public sealed record ScanFolderAddResult
{
    public ScanFolderAddOutcome Outcome { get; init; }
    public string StatusText { get; init; } = "";
    public string NormalizedPath { get; init; } = "";
}
