using OptiClick.Core.Models;

namespace OptiClick.Core.Install;

public sealed record ProxyDllResolutionResult
{
    public bool Success { get; init; } = true;
    public string FinalDllName { get; init; } = "";
    public IReadOnlyList<string> BackupCandidates { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> SkippedCandidates { get; init; } = Array.Empty<string>();
    public string FailureReason { get; init; } = "";
}
