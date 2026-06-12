using OptiClick.Core.Models;

namespace OptiClick.Core.OptiScaler;

public sealed record OptiScalerSettingsApplyRequest
{
    public string SelectedVariantPreference { get; init; } = OptiScalerVariantPreference.StableVariant;

    public string LanguagePreference { get; init; } = AppLanguagePreference.Auto;

    public OptiScalerCommonIniSettingsDocument CommonIniSettings { get; init; } = new();
}

public sealed record OptiScalerSettingsApplyResult
{
    public string SelectedVariantPreference { get; init; } = OptiScalerVariantPreference.StableVariant;

    public OptiScalerCommonIniSettingsDocument CommonIniSettings { get; init; } = new();
}
