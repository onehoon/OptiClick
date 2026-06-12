using OptiClick.Core.Models;
using OptiClick.Wpf.Shell.Settings;

namespace OptiClick.Wpf.ViewModels.Features.Shell.UserSettings;

internal sealed class MainUserSettingsInteractionFeature
{
    private readonly UserSettingsController _userSettingsController;
    private readonly MainUserSettingsApplyController _applyController;
    private readonly MainShellInteractionContextFactory _contextFactory;

    public MainUserSettingsInteractionFeature(
        UserSettingsController userSettingsController,
        MainUserSettingsApplyController applyController,
        MainShellInteractionContextFactory contextFactory)
    {
        _userSettingsController = userSettingsController ?? throw new ArgumentNullException(nameof(userSettingsController));
        _applyController = applyController ?? throw new ArgumentNullException(nameof(applyController));
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
    }

    public AppUserSettings LoadUserSettings()
    {
        return _userSettingsController.Load();
    }

    public void SavePreferencesNonBlocking(string languagePreference, string optiScalerVariantPreference)
    {
        _userSettingsController.SavePreferencesNonBlocking(languagePreference, optiScalerVariantPreference);
    }

    public void DisposeUserSettings()
    {
        _userSettingsController.Dispose();
    }

    public void ApplyUserSettings(AppUserSettings settings)
    {
        _applyController.Apply(
            _contextFactory.CreateUserSettingsApplyContext(),
            settings);
    }
}
