using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using OptiClick.Wpf.Install.Archives;
using OptiClick.Wpf.Install.UiState;
using OptiClick.Wpf.Models;
using OptiClick.Wpf.Services;
using OptiClick.Wpf.Shell.Navigation;
using OptiClick.Wpf.Shell.Runtime;
using OptiClick.Wpf.Shell.RuntimeData;
using OptiClick.Wpf.Shell.Games;
using OptiClick.Wpf.Shell.Selection;
using OptiClick.Wpf.Shell.Startup;

namespace OptiClick.Wpf.ViewModels;

public sealed partial class MainViewModel : ViewModelBase
{
    public Task<bool> ShowStartupOperatingSystemBlockIfNeededAsync(CancellationToken cancellationToken = default)
    {
        return _features.Startup.ShowStartupOperatingSystemBlockIfNeededAsync(cancellationToken);
    }

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        return _features.Startup.InitializeAsync(cancellationToken);
    }

    private void UpdateStartupPreparationState(Func<StartupPreparationState, StartupPreparationState> update)
    {
        ArgumentNullException.ThrowIfNull(update);

        var didChange = false;
        lock (_operationLocks.StartupPreparationStateGate)
        {
            var next = update(_startupPreparationState);
            if (next == _startupPreparationState)
            {
                return;
            }

            _startupPreparationState = next;
            didChange = true;
        }

        if (didChange)
        {
            StartupOverlay.ApplyPreparationState(StartupPreparationState);
            OnPropertyChanged(nameof(StartupPreparationState));
        }
    }

    private static string ClearLastErrorCode(string lastErrorCode, string errorCode)
    {
        return string.Equals(lastErrorCode, errorCode, StringComparison.OrdinalIgnoreCase)
            ? ""
            : lastErrorCode;
    }

    private Task RefreshRuntimeDataCatalogForStartupAsync(CancellationToken cancellationToken = default)
    {
        return _features.Runtime.RefreshRuntimeDataCatalogForStartupAsync(cancellationToken);
    }

    private Task StartStartupPreparationAsync(CancellationToken cancellationToken = default)
    {
        return _features.Startup.StartStartupPreparationAsync(cancellationToken);
    }

    private void StartStartupDialogsInBackground()
    {
        _features.Startup.StartStartupDialogsInBackground();
    }

    private void StartGameMasterCoverPrefetchInBackground()
    {
        _features.Startup.StartGameMasterCoverPrefetchInBackground();
    }

    public void CancelBackgroundWork()
    {
        _features.Startup.CancelBackgroundWork();
    }

    private void QueueHomeCoverPrefetchInBackground(string reason)
    {
        _features.Startup.QueueHomeCoverPrefetchInBackground(reason);
    }

    private async Task RefreshHomeCoversOnDispatcherAsync()
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.HasShutdownStarted)
        {
            return;
        }

        await dispatcher.InvokeAsync(() =>
        {
            Home.RequestVisibleCoverLoad();
        }, DispatcherPriority.Background);
    }

    private bool ShouldBlockStartupForUnsupportedOperatingSystem()
    {
        return IsUnsupportedOperatingSystem();
    }

    private void SetCurrentView(ShellViewKind view)
    {
        if (view != ShellViewKind.Home)
        {
            ClearSelectedGameContext();
        }

        if (!_navigationState.SetCurrentView(view))
        {
            return;
        }
        Navigation.Refresh();
        RefreshNavigationAndScanCommandStates();
        if (view == ShellViewKind.SupportedGamesWiki)
        {
            SupportedGames.EnsureLoadedForView();
            SupportedGames.QueueVisibleCoverLoad(0, 0);
        }
    }

    private void ClearSelectedGameContext()
    {
        if (SelectedGame is null && _selectionState.PendingPopupRequests.Count == 0)
        {
            return;
        }

        Interlocked.Increment(ref _selectionRequestVersion);
        _selectionState = new ShellInstallSelectionState
        {
            MultiGpuBlocked = _features.Runtime.MultiGpuBlocked,
            GpuSelectionPending = _features.Runtime.GpuSelectionPending
        };
        SetSelectedGame(null);
        SelectedGameAction.ApplySelectionBridgeState(_selectionState);
    }

    public async Task RefreshRuntimeContextAsync(CancellationToken cancellationToken = default)
    {
        await _features.Runtime.RefreshRuntimeContextAsync(cancellationToken);
    }

    public async Task RefreshDeviceIdentityRulesAsync(CancellationToken cancellationToken = default)
    {
        await _features.Runtime.RefreshDeviceIdentityRulesAsync(cancellationToken);
    }

    // Apply cached device identity rules during startup so UI can immediately reflect normalized device labels.
    public async Task ApplyDeviceIdentityRulesFromCacheAsync(CancellationToken cancellationToken = default)
    {
        await _features.Runtime.ApplyLocalDeviceIdentityRulesAsync(
            RuntimeSummaryStateText.FromAppStrings(Strings),
            ApplyRuntimeSummaryStateUpdate,
            cancellationToken);
    }

    public void StartDeviceIdentityRulesRefreshInBackground()
    {
        // Remote rules are not needed for startup readiness, so always run as best-effort background.
        _features.Startup.StartBackgroundTask(RefreshDeviceIdentityRulesInBackgroundAsync);
    }

    private async Task RefreshDeviceIdentityRulesInBackgroundAsync(CancellationTokenSource cancellationTokenSource)
    {
        var cancellationToken = cancellationTokenSource.Token;
        try
        {
            await RefreshDeviceIdentityRulesAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Ignore background shutdown during app shutdown.
        }
        catch (Exception ex)
        {
            LogWarning(MainViewModelLogCategories.Runtime, $"device identity rules background refresh failed type={ex.GetType().Name}");
        }
        finally
        {
            _features.Startup.RemoveBackgroundTask(cancellationTokenSource);
        }
    }

    public async Task RefreshRuntimeDataCatalogAsync(CancellationToken cancellationToken = default)
    {
        await _features.Runtime.RefreshRuntimeDataCatalogAsync(
            RuntimeCatalogRefreshMode.Inline,
            cancellationToken);
    }

    private Task RefreshRuntimeDataCatalogByModeAsync(
        RuntimeCatalogRefreshMode refreshMode,
        CancellationToken cancellationToken = default)
    {
        return _features.Runtime.RefreshRuntimeDataCatalogAsync(
            refreshMode,
            cancellationToken);
    }

    private void ApplyRuntimeCatalogSelectionState(ShellInstallSelectionState selectionState)
    {
        _selectionState = selectionState;
        SelectedGameAction.ApplySelectionBridgeState(_selectionState);
    }

    private void ApplyRuntimeSummaryStateUpdate(RuntimeSummaryStateUpdate update)
    {
        _runtimeShellState.ApplyRuntimeSummary(update);
        RuntimeHeader.Apply(update);

        if (update.HasSelectedGpu)
        {
            LogInfo(
                MainViewModelLogCategories.Runtime,
                $"selected_gpu vendor={NormalizeStatusCode(update.SelectedGpuVendor, MainViewModelStatusCodes.Unknown)} name=\"{NormalizeStatusCode(update.SelectedGpuName, MainViewModelStatusCodes.Unknown)}\" source={NormalizeStatusCode(_features.Runtime.SelectedGpuLogSource, "runtime_context_selected")}");
        }
        else
        {
            LogWarning(MainViewModelLogCategories.Runtime, "selected_gpu missing reason=no_supported_gpu");
        }
    }

}
