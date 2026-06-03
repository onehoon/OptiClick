namespace OptiClick.Core.Models;

public sealed record RtssActionPlan
{
    public string Action { get; init; } = "";
    public string Target { get; init; } = "";
    public bool Required { get; init; }
    public string Reason { get; init; } = "";
}
