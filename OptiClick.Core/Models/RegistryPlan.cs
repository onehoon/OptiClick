namespace OptiClick.Core.Models;

public sealed record RegistryPlan
{
    public string Hive { get; init; } = "";
    public string Key { get; init; } = "";
    public string ValueName { get; init; } = "";
    public string ValueType { get; init; } = "";
    public string Value { get; init; } = "";
}
