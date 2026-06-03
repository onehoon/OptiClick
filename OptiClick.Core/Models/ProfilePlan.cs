namespace OptiClick.Core.Models;

public sealed record ProfilePlan
{
    public string ProfileKind { get; init; } = "";
    public string ProfileId { get; init; } = "";
    public int Order { get; init; }
    public string Target { get; init; } = "";
}
