namespace OptiClick.Core.OptiScaler;

public sealed record OptiScalerCommonIniSettingsDocument
{
    public int Version { get; init; } = 1;
    public string Fsr411Mode { get; init; } = OptiScalerFsr411Policy.ModeAuto;
    public IReadOnlyList<OptiScalerCommonIniEntry> Entries { get; init; } = Array.Empty<OptiScalerCommonIniEntry>();
}

public sealed record OptiScalerCommonIniEntry
{
    public string Section { get; init; } = "";
    public string Key { get; init; } = "";
    public string Value { get; init; } = "";
}
