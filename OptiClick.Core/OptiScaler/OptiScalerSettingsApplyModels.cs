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
    public bool IsSuccess { get; init; } = true;

    public string ErrorCode { get; init; } = "";

    public string ErrorMessage { get; init; } = "";

    public string SelectedVariantPreference { get; init; } = OptiScalerVariantPreference.StableVariant;

    public OptiScalerCommonIniSettingsDocument CommonIniSettings { get; init; } = new();
}

public sealed record OptiScalerSettingsPersistenceResult
{
    public static OptiScalerSettingsPersistenceResult Success()
    {
        return new OptiScalerSettingsPersistenceResult
        {
            IsSuccess = true
        };
    }

    public static OptiScalerSettingsPersistenceResult Failed(string errorCode, string errorMessage = "")
    {
        return new OptiScalerSettingsPersistenceResult
        {
            IsSuccess = false,
            ErrorCode = string.IsNullOrWhiteSpace(errorCode) ? "save_failed" : errorCode,
            ErrorMessage = errorMessage ?? ""
        };
    }

    public bool IsSuccess { get; init; }

    public string ErrorCode { get; init; } = "";

    public string ErrorMessage { get; init; } = "";
}
