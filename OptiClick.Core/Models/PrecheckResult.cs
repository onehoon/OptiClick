namespace OptiClick.Core.Models;

public sealed record PrecheckResult
{
    public bool Ok { get; init; } = true;
    public string ResolvedDllName { get; init; } = "";
    public IReadOnlyList<PrecheckFinding> Findings { get; init; } = Array.Empty<PrecheckFinding>();
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
}
