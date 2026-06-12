using System.Threading;
using System.Threading.Tasks;
using OptiClick.Core.Abstractions;
using OptiClick.Core.Models;
using OptiClick.Wpf.Logging;

namespace OptiClick.Wpf.Shell.Settings;

internal sealed class UserSettingsPendingSaveQueue : IDisposable
{
    private readonly IAppUserSettingsStore _userSettingsStore;
    private readonly IAppLogger _logger;
    private readonly object _pendingSaveSync = new();
    private readonly ManualResetEventSlim _backgroundSaveIdle = new(initialState: true);
    private AppUserSettings? _pendingSettings;
    private Task? _backgroundSaveTask;
    private int _isBackgroundSaveRunning;
    private bool _disposed;

    public UserSettingsPendingSaveQueue(IAppUserSettingsStore userSettingsStore, IAppLogger logger)
    {
        _userSettingsStore = userSettingsStore ?? throw new ArgumentNullException(nameof(userSettingsStore));
        _logger = logger ?? NullAppLogger.Instance;
    }

    public void Enqueue(AppUserSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        lock (_pendingSaveSync)
        {
            ThrowIfDisposed();
            _pendingSettings = settings;
            StartBackgroundSaveWorkerIfNeeded();
        }
    }

    public void Flush(TimeSpan? timeout = null)
    {
        ThrowIfDisposed();
        FlushCore(timeout ?? TimeSpan.FromSeconds(2));
    }

    public void Dispose()
    {
        lock (_pendingSaveSync)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
        }

        FlushCore(Timeout.InfiniteTimeSpan);
        var backgroundSaveTask = GetBackgroundSaveTask();
        backgroundSaveTask?.GetAwaiter().GetResult();
        _backgroundSaveIdle.Dispose();
    }

    private void FlushCore(TimeSpan timeout)
    {
        var waitIndefinitely = timeout == Timeout.InfiniteTimeSpan;
        var deadline = waitIndefinitely
            ? DateTime.MaxValue
            : DateTime.UtcNow + timeout;

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

            if (waitIndefinitely)
            {
                _backgroundSaveIdle.Wait();
                continue;
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
        lock (_pendingSaveSync)
        {
            if (_disposed)
            {
                return;
            }
        }

        if (Interlocked.CompareExchange(ref _isBackgroundSaveRunning, 1, 0) != 0)
        {
            return;
        }

        _backgroundSaveIdle.Reset();
        var backgroundSaveTask = Task.Run(DrainPendingSavesAsync);
        lock (_pendingSaveSync)
        {
            _backgroundSaveTask = backgroundSaveTask;
        }
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
                if (IsDisposed())
                {
                    _backgroundSaveIdle.Set();
                }
            }
            else
            {
                _backgroundSaveIdle.Set();
            }
        }
    }

    private Task? GetBackgroundSaveTask()
    {
        lock (_pendingSaveSync)
        {
            return _backgroundSaveTask;
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

    private bool IsDisposed()
    {
        lock (_pendingSaveSync)
        {
            return _disposed;
        }
    }

    private void ThrowIfDisposed()
    {
        lock (_pendingSaveSync)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
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
