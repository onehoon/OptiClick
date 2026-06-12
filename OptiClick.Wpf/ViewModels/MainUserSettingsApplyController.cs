using OptiClick.Core.Models;
using OptiClick.Core.OptiScaler;
using OptiClick.Core.Runtime;

namespace OptiClick.Wpf.ViewModels;

internal sealed class MainUserSettingsApplyController
{
    public void Apply(
        MainUserSettingsApplyContext context,
        AppUserSettings settings)
    {
        ArgumentNullException.ThrowIfNull(context);

        var safeSettings = settings ?? new AppUserSettings();
        var languagePreference = context.NormalizeLanguagePreference(safeSettings.LanguagePreference);
        var optiScalerVariantPreference =
            context.NormalizeOptiScalerVariantPreference(safeSettings.OptiScalerVariantPreference);

        context.SetLanguagePreference(languagePreference);
        context.SetOptiScalerVariantPreference(optiScalerVariantPreference);

        var preferredLanguage = context.ResolvePreferredLanguage(languagePreference);
        if (context.ReadSelectedLanguage() != preferredLanguage)
        {
            context.ApplyChangedLanguage(preferredLanguage);
        }

        context.ApplyLoadedSettings(context.ResolveLanguageOptionFromState(languagePreference));
        context.ApplySavedOptiScalerSettings(
            optiScalerVariantPreference,
            context.LoadCommonIniSettings());
    }
}

internal sealed class MainUserSettingsApplyContext
{
    public required Func<string?, string> NormalizeLanguagePreference { get; init; }
    public required Func<string?, string> NormalizeOptiScalerVariantPreference { get; init; }
    public required Func<string, AppLanguage> ResolvePreferredLanguage { get; init; }
    public required Func<string, string> ResolveLanguageOptionFromState { get; init; }
    public required Action<string> SetLanguagePreference { get; init; }
    public required Action<string> SetOptiScalerVariantPreference { get; init; }
    public required Func<AppLanguage> ReadSelectedLanguage { get; init; }
    public required Action<AppLanguage> ApplyChangedLanguage { get; init; }
    public required Action<string> ApplyLoadedSettings { get; init; }
    public required Action<string, OptiScalerCommonIniSettingsDocument?> ApplySavedOptiScalerSettings { get; init; }
    public required Func<OptiScalerCommonIniSettingsDocument?> LoadCommonIniSettings { get; init; }
}
