using OptiClick.Wpf.ViewModels.Sections.OptiScaler;

namespace OptiClick.Wpf.ViewModels;

internal sealed class MainOptiScalerSectionSaveHandler : IOptiScalerSectionSaveHandler
{
    private readonly MainOptiScalerSettingsController _settingsController;
    private readonly Func<string> _languagePreferenceAccessor;
    private readonly Action<string> _applyVariantPreference;
    private readonly Action _refreshVisibleGamesAfterPreferenceChange;

    public MainOptiScalerSectionSaveHandler(
        MainOptiScalerSettingsController settingsController,
        Func<string> languagePreferenceAccessor,
        Action<string> applyVariantPreference,
        Action? refreshVisibleGamesAfterPreferenceChange = null)
    {
        _settingsController = settingsController ?? throw new ArgumentNullException(nameof(settingsController));
        _languagePreferenceAccessor = languagePreferenceAccessor
                                      ?? throw new ArgumentNullException(nameof(languagePreferenceAccessor));
        _applyVariantPreference = applyVariantPreference
                                  ?? throw new ArgumentNullException(nameof(applyVariantPreference));
        _refreshVisibleGamesAfterPreferenceChange = refreshVisibleGamesAfterPreferenceChange ?? (() => { });
    }

    public OptiScalerSectionSaveResult Save(OptiScalerSectionSaveRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var applyResult = _settingsController.ApplySettings(new MainOptiScalerSettingsContext
        {
            SelectedOptiScalerVariantOption = request.SelectedVariant,
            LanguagePreference = _languagePreferenceAccessor(),
            CommonIniSettings = request.CommonIniSettings
        });
        _applyVariantPreference(applyResult.SelectedVariantPreference);
        _refreshVisibleGamesAfterPreferenceChange();

        return new OptiScalerSectionSaveResult(
            applyResult.SelectedVariantPreference,
            applyResult.CommonIniSettings);
    }
}
