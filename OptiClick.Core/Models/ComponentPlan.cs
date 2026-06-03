namespace OptiClick.Core.Models;

public enum ComponentKind
{
    ExtraBundle,
    UltimateAsiLoader,
    SpecialK,
    Reframework,
    OptiPatcher,
    Unreal5,
    Fsr4
}

public sealed record ComponentPlan
{
    public ComponentKind Kind { get; init; }
    public string Source { get; init; } = "";
    public string Destination { get; init; } = "";
    public bool Skipped { get; init; }
    public string Reason { get; init; } = "";
    public IReadOnlyDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();
}
