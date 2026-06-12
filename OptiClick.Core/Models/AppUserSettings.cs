namespace OptiClick.Core.Models;

public sealed record AppUserSettings
{
    public int Version { get; init; } = 1;
    public string LanguagePreference { get; init; } = AppLanguagePreference.Auto;
    public string OptiScalerVariantPreference { get; init; } = "stable";
}
