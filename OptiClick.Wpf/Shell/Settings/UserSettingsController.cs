using OptiClick.Core.Abstractions;
using OptiClick.Core.Models;
using OptiClick.Wpf.Logging;

namespace OptiClick.Wpf.Shell.Settings;

public sealed class UserSettingsController : IDisposable
{
    private readonly IAppUserSettingsStore _userSettingsStore;
    private readonly UserSettingsPendingSaveQueue _pendingSaveQueue;

    public UserSettingsController(IAppUserSettingsStore userSettingsStore, IAppLogger? logger = null)
    {
        _userSettingsStore = userSettingsStore;
        _pendingSaveQueue = new UserSettingsPendingSaveQueue(
            userSettingsStore,
            logger ?? NullAppLogger.Instance);
    }

    public AppUserSettings Load()
    {
        return _userSettingsStore.Load() ?? new AppUserSettings();
    }

    public void SavePreferences()
    {
        var current = Load();
        SavePreferences(current.LanguagePreference, current.OptiScalerVariantPreference);
    }

    public void SavePreferences(string languagePreference)
    {
        var current = Load();
        SavePreferences(languagePreference, current.OptiScalerVariantPreference);
    }

    public void SavePreferences(string languagePreference, string optiScalerVariantPreference)
    {
        _userSettingsStore.Save(AppUserSettingsUpdatePolicy.UpdatePreferences(
            Load(),
            languagePreference,
            optiScalerVariantPreference));
    }

    public void SavePreferencesNonBlocking()
    {
        var current = Load();
        SavePreferencesNonBlocking(current.LanguagePreference, current.OptiScalerVariantPreference);
    }

    public void SavePreferencesNonBlocking(string languagePreference)
    {
        var current = Load();
        SavePreferencesNonBlocking(languagePreference, current.OptiScalerVariantPreference);
    }

    public void SavePreferencesNonBlocking(string languagePreference, string optiScalerVariantPreference)
    {
        var current = Load();
        _pendingSaveQueue.Enqueue(AppUserSettingsUpdatePolicy.UpdatePreferences(
            current,
            languagePreference,
            optiScalerVariantPreference));
    }

    public void FlushPendingSaves(TimeSpan? timeout = null)
    {
        _pendingSaveQueue.Flush(timeout);
    }

    public void Dispose()
    {
        _pendingSaveQueue.Dispose();
    }
}
