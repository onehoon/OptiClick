using OptiClick.Core.OptiScaler;

namespace OptiClick.Core.Models;

public static class AppUserSettingsUpdatePolicy
{
    public static AppUserSettings UpdatePreferences(
        AppUserSettings? current,
        string? languagePreference,
        string? optiScalerVariantPreference)
    {
        var safeCurrent = current ?? new AppUserSettings();
        return safeCurrent with
        {
            Version = safeCurrent.Version <= 0 ? 1 : safeCurrent.Version,
            LanguagePreference = AppLanguagePreference.NormalizeOrDefault(languagePreference),
            OptiScalerVariantPreference = OptiScalerVariantPreference.NormalizeOrDefault(optiScalerVariantPreference)
        };
    }
}
