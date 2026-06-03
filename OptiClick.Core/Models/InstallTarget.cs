namespace OptiClick.Core.Models;

public sealed record InstallTarget
{
    public string GameId { get; init; } = "";
    public string GameName { get; init; } = "";
    public string TargetPath { get; init; } = "";
}
