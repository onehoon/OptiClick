using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using OptiClick.Core.Runtime;
using OptiClick.Wpf.Models;
using OptiClick.Wpf.Services;
using OptiClick.Wpf.Shell.Actions;
using OptiClick.Wpf.Shell.Startup;
using OptiClick.Wpf.Shell.Update;

namespace OptiClick.Wpf.ViewModels;

public sealed partial class MainViewModel : ViewModelBase
{
    private void OpenSupportRequest() =>
        ApplyShellCommandActionResult(_shellCommandActionController.OpenSupportRequest(
            GetCurrentAppVersion(),
            _runtimeShellState.LatestRuntimeContext,
            SelectedLanguage,
            Strings));

    private void OpenGameSupportRequest() =>
        ApplyShellCommandActionResult(_shellCommandActionController.OpenGameSupportRequest(
            GetCurrentAppVersion(),
            _runtimeShellState.LatestRuntimeContext,
            SelectedLanguage,
            Strings));

    public void NotifyAdministratorRelaunchCancelled()
    {
        ApplyShellCommandActionResult(_shellCommandActionController.BuildAdministratorRelaunchCancelledNotice(Strings));
    }

    private async Task ShowPendingStartupNoticesAsync(CancellationToken cancellationToken = default)
    {
        if (!_pendingAdministratorRelaunchCancelledNotice)
        {
            return;
        }

        _pendingAdministratorRelaunchCancelledNotice = false;
        await _dialogPresenter.ShowSafelyAsync(
            _shellCommandActionController.BuildAdministratorRelaunchCancelledDialog(Strings),
            cancellationToken);
    }

    private async Task ShowStartupAnnouncementIfNeededAsync(CancellationToken cancellationToken = default)
    {
        var result = _startupAnnouncementFlowController.Build(
            new StartupAnnouncementFlowRequest
            {
                RuntimeData = _runtimeShellState.LatestRuntimeData,
                Language = SelectedLanguage,
                SelectedGpuVendor = _runtimeShellState.LatestRuntimeContext.SelectedGpu?.Vendor ?? ""
            });
        _flowLogDispatcher.Dispatch(result.Logs, MainViewModelLogCategories.Startup);

        if (!result.ShouldShowDialog || result.DialogRequest is null)
        {
            return;
        }

        await _dialogPresenter.ShowSafelyAsync(result.DialogRequest, cancellationToken);
    }

    private void ApplyUserSettings(AppUserSettings settings)
    {
        var safeSettings = settings ?? new AppUserSettings();

        _languagePreference = NormalizeLanguagePreference(safeSettings.LanguagePreference);
        _optiScalerVariantPreference = NormalizeOptiScalerVariantPreference(safeSettings.OptiScalerVariantPreference);
        var preferredLanguage = _languagePreference switch
        {
            LanguagePreferenceKorean => AppLanguage.Korean,
            LanguagePreferenceEnglish => AppLanguage.English,
            _ => ResolveAutoLanguage()
        };
        if (_selectedLanguage != preferredLanguage)
        {
            _selectedLanguage = preferredLanguage;
            _languageProvider.SetLanguage(preferredLanguage);
            OnPropertyChanged(nameof(SelectedLanguage));
            RefreshLocalizedStrings();
            SelectedGameAction.ApplyLocalization(Strings);
            SupportedGames.RefreshAfterLanguageChange();
            ApplyLocalizationStateUpdate(_localizationStateController.BuildRefreshState(preferredLanguage, Strings));
        }

        Settings.ApplyLoadedSettings(ResolveLanguageOptionFromState(_languagePreference));
    }

    private void SaveUserSettings() => _userSettingsController.SavePreferencesNonBlocking(
        _languagePreference,
        _optiScalerVariantPreference);

    public void FlushPendingUserSettingsSave() => _userSettingsController.FlushPendingSaves();

    private void OpenLogFolder() => ApplyShellCommandActionResult(_shellCommandActionController.OpenLogFolder(_appLogger.LogDirectory, Strings));

    private void ApplyShellCommandActionResult(ShellCommandActionResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        var update = _resultApplier.CreateShellCommandStateUpdate(result);
        ApplyStateUpdate(update);

        if (result.SupportActionResult is not null)
        {
            ApplyDeferredStateUpdate(_resultApplier.CreateSupportActionStateUpdate(result.SupportActionResult));
        }

        if (update.DialogRequest is not null)
        {
            _dialogPresenter.ShowDeferred(update.DialogRequest);
        }

        ApplyAppLog(result.ShouldWriteLog, result.LogAsWarning, result.LogCategory, result.LogMessage);
    }

    private Task ShowStartupUpdateCheckDialogAsync(CancellationToken cancellationToken = default)
    {
        return ShowUpdateCheckDialogAsync(AppUpdateTrigger.Startup, cancellationToken);
    }

    private async Task ShowUpdateCheckDialogAsync(
        AppUpdateTrigger trigger,
        CancellationToken cancellationToken = default)
    {
        var coordinatorResult = _appUpdateCoordinator.BeginCheck(
            new AppUpdateCoordinatorRequest
            {
                Trigger = trigger,
                LatestRuntimeData = _runtimeShellState.LatestRuntimeData,
                CurrentVersion = GetCurrentAppVersion(),
                Strings = Strings,
                IsAppUpdateInProgress = _isAppUpdateInProgress
            });

        _flowLogDispatcher.Dispatch(coordinatorResult.Logs, MainViewModelLogCategories.AppUpdate);

        if (!string.IsNullOrWhiteSpace(coordinatorResult.StatusText))
        {
            SettingsStatusText = coordinatorResult.StatusText;
        }

        if (!coordinatorResult.ShouldContinue
            || !coordinatorResult.ShouldShowDialog
            || coordinatorResult.DialogRequest is null)
        {
            return;
        }

        var confirm = await _dialogPresenter.ShowSafelyAsync(coordinatorResult.DialogRequest, cancellationToken);
        if (!coordinatorResult.IsUpdateAvailable)
        {
            return;
        }

        if (!AppUpdateCoordinator.IsDialogConfirmed(confirm))
        {
            SettingsStatusText = Strings.UpdateCanceled;
            return;
        }

        if (!coordinatorResult.TryGetUpdateInfo(out var updateInfo))
        {
            SettingsStatusText = Strings.UpdateFailed;
            LogError(MainViewModelLogCategories.AppUpdate, coordinatorResult.MissingUpdateInfoLogMessage);
            return;
        }

        await ExecuteConfirmedAppUpdateAsync(updateInfo, cancellationToken);
    }

    private async Task ExecuteConfirmedAppUpdateAsync(AppUpdateInfo updateInfo, CancellationToken cancellationToken)
    {
        ApplyBusyStateUpdate(
            _busyStateApplier.CreateAppUpdateBusyState(
                true,
                _isInstallExecutionInProgress,
                _selectionState,
                Strings.OperationOverlayUpdating));
        try
        {
            SettingsStatusText = Strings.UpdatePreparing;
            var executeResult = await _appUpdateFlowController.ExecuteConfirmedUpdateAsync(
                new AppUpdateConfirmedRequest
                {
                    UpdateInfo = updateInfo,
                    Strings = Strings
                },
                cancellationToken);
            await ApplyAppUpdateFlowResultAsync(executeResult, cancellationToken);
        }
        finally
        {
            ApplyBusyStateUpdate(
                _busyStateApplier.CreateAppUpdateBusyState(
                    false,
                    _isInstallExecutionInProgress,
                    _selectionState));
        }
    }

    private void ApplyBusyStateUpdate(MainViewModelBusyStateUpdate update)
    {
        ArgumentNullException.ThrowIfNull(update);
        _isAppUpdateInProgress = update.IsAppUpdateInProgress;
        _isInstallExecutionInProgress = update.IsInstallExecutionInProgress;
        ShellBusyState.Apply(update.IsOperationOverlayVisible, update.OperationOverlayMessage);
        if (!string.IsNullOrWhiteSpace(update.SettingsStatusText)) SettingsStatusText = update.SettingsStatusText;
        if (update.ShouldRefreshInstallCommand)
        {
            Home.RefreshCommandStates();
        }
        _selectionState = update.SelectionState;
        if (update.ShouldApplySelectedGameActionRunningState) SelectedGameAction.ApplySelectionBridgeState(_selectionState);
    }

    private async Task ApplyAppUpdateFlowResultAsync(
        AppUpdateExecutionFlowResult result,
        CancellationToken cancellationToken = default)
    {
        _flowLogDispatcher.Dispatch(result.Logs, MainViewModelLogCategories.AppUpdate);
        var update = _resultApplier.CreateAppUpdateStateUpdate(result);
        ApplyStateUpdate(update);

        if (update.DialogRequest is not null)
        {
            await _dialogPresenter.ShowSafelyAsync(update.DialogRequest, cancellationToken);
        }

        if (update.ShouldShutdown)
        {
            Application.Current?.Shutdown();
        }
    }

    private void ShowDetailsDialog()
    {
        if (SelectedGame is null) return;
        _dialogPresenter.ShowDeferred(_gameDetailsDialogPresenter.BuildDetailsDialog(SelectedGame, Strings));
    }
}
