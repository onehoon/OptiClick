using OptiClick.Core.Models;

namespace OptiClick.Core.Games;

public sealed record RuntimeDataParseResult
{
    public int SchemaVersion { get; init; }
    public IReadOnlyList<GameEntry> Games { get; init; } = Array.Empty<GameEntry>();
    public IReadOnlyList<ResourceEntry> Resources { get; init; } = Array.Empty<ResourceEntry>();
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
}
