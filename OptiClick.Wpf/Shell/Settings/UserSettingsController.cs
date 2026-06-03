using OptiClick.Wpf.Services;
using OptiClick.Wpf.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace OptiClick.Wpf.Shell.Settings;

public sealed class UserSettingsController
{
    private readonly IAppUserSettingsStore _userSettingsStore;
    private readonly object _pendingSaveSync = new();
    private readonly ManualResetEventSlim _backgroundSaveIdle = new(initialState: true);
    private readonly IAppLogger _logger;
    private AppUserSettings? _pendingSettings;
    private int _isBackgroundSaveRunning;

    public UserSettingsController(IAppUserSettingsStore userSettingsStore, IAppLogger? logger = null)
    {
        _userSettingsStore = userSettingsStore;
        _logger = logger ?? NullAppLogger.Instance;
    }

    public AppUserSettings Load()
    {
        return _userSettingsStore.Load() ?? new AppUserSettings();
    }

    public void SavePreferences(bool checkUpdatesOnStartup)
    {
        var current = Load();
        SavePreferences(checkUpdatesOnStartup, current.LanguagePreference);
    }

    public void SavePreferences(bool checkUpdatesOnStartup, string languagePreference)
    {
        _userSettingsStore.Save(new AppUserSettings
        {
            Version = 1,
            CheckUpdatesOnStartup = checkUpdatesOnStartup,
            LanguagePreference = string.IsNullOrWhiteSpace(languagePreference) ? "auto" : languagePreference
        });
    }

    public void SavePreferencesNonBlocking(bool checkUpdatesOnStartup)
    {
        var current = Load();
        SavePreferencesNonBlocking(checkUpdatesOnStartup, current.LanguagePreference);
    }

    public void SavePreferencesNonBlocking(bool checkUpdatesOnStartup, string languagePreference)
    {
        lock (_pendingSaveSync)
        {
            _pendingSettings = new AppUserSettings
            {
                Version = 1,
                CheckUpdatesOnStartup = checkUpdatesOnStartup,
                LanguagePreference = string.IsNullOrWhiteSpace(languagePreference) ? "auto" : languagePreference
            };
        }

        StartBackgroundSaveWorkerIfNeeded();
    }

    public void FlushPendingSaves(TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(2));

        while (true)
        {
            while (TryTakePendingSettings(out var pending))
            {
                TrySavePendingSettings(pending!, "flush");
            }

            if (Interlocked.CompareExchange(ref _isBackgroundSaveRunning, 0, 0) == 0)
            {
                return;
            }

            var remaining = deadline - DateTime.UtcNow;
            if (remaining <= TimeSpan.Zero)
            {
                return;
            }

            _backgroundSaveIdle.Wait(remaining);
        }
    }

    private void StartBackgroundSaveWorkerIfNeeded()
    {
        if (Interlocked.CompareExchange(ref _isBackgroundSaveRunning, 1, 0) != 0)
        {
            return;
        }

        _backgroundSaveIdle.Reset();
        _ = Task.Run(DrainPendingSavesAsync);
    }

    private async Task DrainPendingSavesAsync()
    {
        try
        {
            while (true)
            {
                AppUserSettings? pending;
                lock (_pendingSaveSync)
                {
                    pending = _pendingSettings;
                    _pendingSettings = null;
                }

                if (pending is null)
                {
                    break;
                }

                TrySavePendingSettings(pending, "background");
                await Task.Yield();
            }
        }
        finally
        {
            Interlocked.Exchange(ref _isBackgroundSaveRunning, 0);
            var shouldRestart = TryHasPendingSettings();
            if (shouldRestart)
            {
                StartBackgroundSaveWorkerIfNeeded();
            }
            else
            {
                _backgroundSaveIdle.Set();
            }
        }
    }

    private bool TryTakePendingSettings(out AppUserSettings? pending)
    {
        lock (_pendingSaveSync)
        {
            pending = _pendingSettings;
            _pendingSettings = null;
            return pending is not null;
        }
    }

    private bool TryHasPendingSettings()
    {
        lock (_pendingSaveSync)
        {
            return _pendingSettings is not null;
        }
    }

    private void TrySavePendingSettings(AppUserSettings pending, string source)
    {
        try
        {
            _userSettingsStore.Save(pending);
        }
        catch (Exception ex)
        {
            _logger.Warning(
                "settings",
                $"user settings save failed source={(source ?? "unknown").Trim()} type={ex.GetType().Name}");
        }
    }
}
