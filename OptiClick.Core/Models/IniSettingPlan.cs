namespace OptiClick.Core.Models;

public sealed record IniSettingPlan
{
    public string FileKind { get; init; } = "";
    public string Section { get; init; } = "";
    public string Key { get; init; } = "";
    public string Value { get; init; } = "";
    public string Condition { get; init; } = "";
}
