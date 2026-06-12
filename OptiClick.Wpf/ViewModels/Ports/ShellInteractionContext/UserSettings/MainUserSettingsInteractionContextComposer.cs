using OptiClick.Wpf.ViewModels.Ports.ShellInteractionContext.Language;
using OptiClick.Wpf.ViewModels.Ports.ShellInteractionContext.OptiScaler;

namespace OptiClick.Wpf.ViewModels.Ports.ShellInteractionContext.UserSettings;

internal sealed record MainUserSettingsInteractionContextCompositionInput
{
    public required IMainUserSettingsInteractionAccess UserSettingsAccess { get; init; }
    public required IMainLanguagePreferenceInteractionAccess LanguageAccess { get; init; }
    public required IMainOptiScalerSettingsInteractionAccess OptiScalerAccess { get; init; }
    public required MainOptiScalerSettingsController OptiScalerSettingsController { get; init; }
}

internal static class MainUserSettingsInteractionContextComposer
{
    public static MainUserSettingsInteractionContextInput Compose(
        MainUserSettingsInteractionContextCompositionInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new MainUserSettingsInteractionContextInput
        {
            NormalizeLanguagePreference = input.LanguageAccess.NormalizeLanguagePreference,
            NormalizeOptiScalerVariantPreference = input.OptiScalerAccess.NormalizeOptiScalerVariantPreference,
            ResolvePreferredLanguage = input.LanguageAccess.ResolvePreferredLanguage,
            ResolveLanguageOptionFromState = input.LanguageAccess.ResolveLanguageOptionFromState,
            SetLanguagePreference = value => input.UserSettingsAccess.LanguagePreference = value,
            SetOptiScalerVariantPreference = value => input.UserSettingsAccess.PreferredOptiScalerVariant = value,
            ReadSelectedLanguage = () => input.LanguageAccess.SelectedLanguage,
            ApplyChangedLanguage = input.LanguageAccess.ApplyChangedLanguage,
            ApplyLoadedSettings = input.LanguageAccess.ApplyLoadedSettings,
            ApplySavedOptiScalerSettings = input.OptiScalerAccess.ApplySavedOptiScalerSettings,
            LoadCommonIniSettings = input.OptiScalerSettingsController.LoadCommonIniSettings
        };
    }
}
