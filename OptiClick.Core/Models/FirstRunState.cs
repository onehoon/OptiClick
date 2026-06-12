namespace OptiClick.Core.Models;

public sealed record FirstRunState
{
    public bool FirstStartupCompleted { get; init; }
    public bool ArchivePreparedOnce { get; init; }
    public bool CoverCacheBootstrapAttempted { get; init; }
    public string CoverCacheBootstrapState { get; init; } = "";
    public DateTimeOffset? CreatedAt { get; init; }
}
