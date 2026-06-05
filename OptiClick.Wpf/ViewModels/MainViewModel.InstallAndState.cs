using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OptiClick.Wpf.Install.Archives;
using OptiClick.Wpf.Install.Flow;
using OptiClick.Wpf.Install.Planning;
using OptiClick.Wpf.Install.UiState;
using OptiClick.Wpf.Models;
using OptiClick.Wpf.Shell.RuntimeData;
using OptiClick.Wpf.Shell.Games;
using OptiClick.Wpf.Shell.Localization;
using OptiClick.Wpf.Shell.Scan;
using OptiClick.Wpf.Shell.Selection;
using OptiClick.Wpf.Threading;
using OperatingSystemSupportState = OptiClick.Infrastructure.Windows.OperatingSystemSupportState;

namespace OptiClick.Wpf.ViewModels;

public sealed partial class MainViewModel : ViewModelBase
{
    private sealed record InstallExecutionPreparation(
        InstallFlowRequest Request,
        ShellInstallSelectionState SelectionStateBeforeExecution);

    private async Task ShowInstallDialogAsync(CancellationToken cancellationToken = default)
    {
        if (ShouldBlockStartupForUnsupportedOperatingSystem())
        {
            await _dialogPresenter.ShowSafelyAsync(
                _startupNoticePresenter.BuildWindows10StartupBlockDialog(Strings),
                cancellationToken);
            return;
        }

        if (SelectedGame is null)
        {
            return;
        }

        if (_isAppUpdateInProgress)
        {
            SettingsStatusText = Strings.InstallUnavailableDuringAppUpdate;
            return;
        }

        await _installExecutionLock.TryRunExclusiveAsync(
            async ct =>
        {
            if (await HandleInstalledGameActionAsync(ct))
            {
                return;
            }

            await ExecuteCurrentInstallFlowAsync(ct);
        },
            cancellationToken);
    }

    private async Task ExecuteCurrentInstallFlowAsync(CancellationToken cancellationToken)
    {
        var preparation = await PrepareInstallExecutionAsync(cancellationToken);
        if (preparation is null)
        {
            return;
        }

        await ExecuteInstallFlowAsync(preparation, cancellationToken);
    }

    private async Task<bool> HandleInstalledGameActionAsync(CancellationToken cancellationToken)
    {
        var selectedGame = SelectedGame;
        if (selectedGame is null)
        {
            return false;
        }

        var statusCode = NormalizeStatusCode(_selectionState.SelectedInstallStatusCode, InstallStatusCodes.Installable);
        if (!IsInstalledSelectionStatus(statusCode))
        {
            return false;
        }

        var request = BuildInstallManagementDialogRequest(statusCode);
        var selectedGameId = NormalizeStatusCode(_selectionState.SelectedGameId, "none");
        var action = await _installManagementDialogService.ShowDialogAsync(request, cancellationToken);
        LogInfo(
            MainViewModelLogCategories.UninstallUi,
            $"popup result source=install_management game_id={selectedGameId} status={statusCode} result={action}");

        if (action == InstallManagementDialogResult.Cancel)
        {
            return true;
        }

        if (action == InstallManagementDialogResult.ContinueInstall)
        {
            await ExecuteCurrentInstallFlowAsync(cancellationToken);
            return true;
        }

        await HandleUninstallAsync(selectedGame, cancellationToken);
        return true;
    }

    private static bool IsInstalledSelectionStatus(string statusCode)
    {
        var normalized = NormalizeStatusCode(statusCode, InstallStatusCodes.Installable);
        return !string.Equals(normalized, InstallStatusCodes.Installable, StringComparison.OrdinalIgnoreCase);
    }

    private InstallManagementDialogRequest BuildInstallManagementDialogRequest(string statusCode)
    {
        var isUpdateAvailable = string.Equals(
            NormalizeStatusCode(statusCode, InstallStatusCodes.Installable),
            InstallStatusCodes.UpdateAvailable,
            StringComparison.OrdinalIgnoreCase);
        var message = isUpdateAvailable
            ? $"{Strings.InstallManagementUpdateAvailableSummary}\n{Strings.InstallManagementChooseAction}"
            : $"{Strings.InstallManagementInstalledSummary}\n{Strings.InstallManagementChooseAction}";

        return new InstallManagementDialogRequest
        {
            Title = Strings.InstallManagementDialogTitle,
            Message = message,
            DestructiveText = Strings.InstallManagementUninstallButton,
            PrimaryText = isUpdateAvailable ? Strings.InstallButtonUpdate : Strings.InstallButtonReinstall,
            CancelText = Strings.DialogButtonCancel
        };
    }

    private async Task HandleUninstallAsync(GameCardViewModel selectedGame, CancellationToken cancellationToken)
    {
        await _uninstallFlowCoordinator.RunAsync(
            new UninstallFlowCoordinatorRequest
            {
                SelectedGame = selectedGame,
                SelectedGameId = _selectionState.SelectedGameId,
                TargetPath = ResolveSelectedGameTargetPath(selectedGame),
                Strings = Strings,
                SelectionStateBeforeExecution = _selectionState,
                ApplyInstallBusyState = ApplyInstallBusyState,
                ApplySettingsStatusText = value => SettingsStatusText = value,
                RefreshSelectionAfterUninstallAsync = RefreshSelectionAfterUninstallAsync
            },
            cancellationToken);
    }

    private string ResolveSelectedGameTargetPath(GameCardViewModel selectedGame)
    {
        var gameId = NormalizeStatusCode(
            selectedGame.SourceModel?.GameId ?? selectedGame.GameEntry.GameId,
            "");
        if (!string.IsNullOrWhiteSpace(gameId)
            && _scannedGameState.TryGetTargetPath(gameId, out var targetPath))
        {
            return InstallTargetPathNormalizer.NormalizeTargetDirectory(targetPath);
        }

        var fallbackPath = _selectionState.SelectedMatchResult?.FolderPath;
        return InstallTargetPathNormalizer.NormalizeTargetDirectory(fallbackPath);
    }

    private async Task RefreshSelectionAfterUninstallAsync(
        GameCardViewModel selectedGame,
        CancellationToken cancellationToken)
    {
        var buttonBefore = SelectedGameAction.InstallButtonText;
        try
        {
            var selectedGameId = (selectedGame.SourceModel?.GameId ?? selectedGame.GameEntry.GameId ?? "").Trim();
            var refreshedCard = TryRefreshVisibleGameCardsAfterInstall(selectedGameId);
            if (refreshedCard is not null)
            {
                selectedGame = refreshedCard;
            }

            await SelectGameCardAsync(selectedGame, cancellationToken, navigateHome: false, showPendingPopups: false);
            LogInfo(
                MainViewModelLogCategories.UninstallFlow,
                $"uninstall status refresh result=success game_id={NormalizeStatusCode(selectedGameId, "none")} card_refreshed={(refreshedCard is not null).ToString().ToLowerInvariant()} badge_after={NormalizeStatusCode(selectedGame.StatusBadge, "none")} button_before={NormalizeStatusCode(buttonBefore, "none")} button_after={NormalizeStatusCode(SelectedGameAction.InstallButtonText, "none")}");
        }
        catch (OperationCanceledException)
        {
            LogWarning(MainViewModelLogCategories.UninstallFlow, "uninstall status refresh result=canceled");
        }
        catch (Exception ex)
        {
            LogError(MainViewModelLogCategories.UninstallFlow, "uninstall status refresh result=failed", ex);
        }
    }

    private async Task<InstallExecutionPreparation?> PrepareInstallExecutionAsync(CancellationToken cancellationToken)
    {
        if (_isInstallExecutionInProgress)
        {
            return null;
        }

        var selectedGame = SelectedGame;
        if (selectedGame is null)
        {
            return null;
        }

        var selectedIndex = Games.IndexOf(selectedGame);
        if (selectedIndex < 0)
        {
            return null;
        }

        await RefreshArchiveReadinessAsync(cancellationToken);
        // This pre-install refresh reuses the current selection without showing already-reviewed popups.
        // If future precheck logic can create new blocking warnings here, those must be shown to the user
        // instead of being auto-confirmed.
        await SelectGameCardAsync(selectedGame, cancellationToken, navigateHome: false, showPendingPopups: false);

        var request = _flowRequestFactory.BuildInstallRequest(
            selectedGame,
            selectedIndex,
            _runtimeShellState.LatestRuntimeContext,
            _runtimeShellState.LatestArchiveReadiness,
            _selectionState,
            _scannedGameState.MatchByGameId,
            _scannedGameState.TargetPathByGameId,
            _runtimeShellState.ModuleDownloadLinks,
            EnsureOperatingSystemPolicyEvaluated().IsSupported,
            _isInstallExecutionInProgress,
            _isAppUpdateInProgress,
            Strings,
            _runtimeShellState.LatestRemoteCatalogErrorCode);
        // Keep the pre-install selection snapshot so busy false can restore
        // a non-running button presentation when no recompute occurs immediately.
        return new InstallExecutionPreparation(request, _selectionState);
    }

    private async Task ExecuteInstallFlowAsync(
        InstallExecutionPreparation preparation,
        CancellationToken cancellationToken)
    {
        var coordinatorResult = await _installExecutionCoordinator.RunAsync(
            new InstallExecutionCoordinatorRequest
            {
                FlowRequest = preparation.Request,
                SelectionStateBeforeExecution = preparation.SelectionStateBeforeExecution,
                Strings = Strings,
                ApplyInstallBusyState = ApplyInstallBusyState
            },
            cancellationToken);

        await ApplyInstallExecutionResultAsync(coordinatorResult.Result, cancellationToken);
    }

    private async Task ApplyInstallExecutionResultAsync(
        InstallFlowResult result,
        CancellationToken cancellationToken)
    {
        _flowLogDispatcher.Dispatch(result.Logs, MainViewModelLogCategories.Install);
        var update = _resultApplier.CreateInstallStateUpdate(result);

        var finalSuccess = result.ApplyResult?.FinalSuccess == true;
        if (!result.DidStart || result.WasBlocked || !finalSuccess)
        {
            ApplyDeferredStateUpdate(update);
            LogInfo(MainViewModelLogCategories.Install, "badge refresh result=skipped reason=install_not_completed");
            return;
        }

        var completionDialog = update.PopupRequest is null
            ? null
            : _installPopupPresenter.BuildDialogRequest(update.PopupRequest, Strings);
        ApplyStateUpdate(update with
        {
            PopupRequest = null
        });

        await RefreshSelectionAfterSuccessfulInstallAsync(cancellationToken);

        if (completionDialog is null)
        {
            return;
        }

        LogInfo(MainViewModelLogCategories.Install, "popup show source=install_post");
        var dialogResult = await _dialogPresenter.ShowSafelyAsync(
            completionDialog,
            cancellationToken);
        LogInfo(MainViewModelLogCategories.Install, $"popup result source=install_post result={dialogResult}");
        if (dialogResult == AppDialogResult.Ok)
        {
            ClearSelectedGameContext();
            LogInfo(MainViewModelLogCategories.Install, "selection reset result=success reason=install_completion_acknowledged");
        }
        else
        {
            LogInfo(MainViewModelLogCategories.Install, $"selection reset result=skipped reason=install_completion_not_acknowledged dialog_result={dialogResult}");
        }
    }

    private async Task<ArchiveReadinessFlowResult> RefreshArchiveReadinessAsync(CancellationToken cancellationToken)
    {
        return await _archiveReadinessRefreshCoordinator.RunForegroundRefreshAsync(
            RefreshArchiveReadinessCoreAsync,
            cancellationToken);
    }

    private async Task<ArchiveReadinessFlowResult> RefreshArchiveReadinessCoreAsync(CancellationToken cancellationToken)
    {
        var result = await _archiveReadinessFlowController.RefreshAsync(
            new ArchiveReadinessFlowRequest
            {
                ModuleDownloadLinks = _runtimeShellState.ModuleDownloadLinks,
                OptiScalerVariantCatalog = _runtimeShellState.LatestOptiScalerVariantCatalog,
                PreferredOptiScalerVariant = _optiScalerVariantPreference
            },
            cancellationToken);
        _flowLogDispatcher.Dispatch(result.Logs, MainViewModelLogCategories.Install);
        if (result.DidRun)
        {
            _runtimeShellState.SetArchiveReadiness(result.Readiness);
            ApplyOptiScalerVariantSyncResult(result.OptiScalerVariantSync);
        }

        return result;
    }

    private void ApplyOptiScalerVariantSyncResult(OptiScalerVariantSyncResult? result)
    {
        _runtimeShellState.ApplyOptiScalerVariantSync(result);
        Settings.ApplyOptiScalerVariantOptions(
            _runtimeShellState.LatestOptiScalerVariantSelectionOptions,
            _runtimeShellState.EffectiveOptiScalerVariant);

        if (result?.ShouldPersistEffectiveVariant == true)
        {
            _optiScalerVariantPreference = NormalizeOptiScalerVariantPreference(result.EffectiveVariant);
            SaveUserSettings();
        }
    }

    private async Task RefreshSelectionAfterSuccessfulInstallAsync(CancellationToken cancellationToken)
    {
        var selectedGame = SelectedGame;
        if (selectedGame is null)
        {
            LogWarning(MainViewModelLogCategories.Install, "badge refresh result=skipped reason=no_selected_game");
            return;
        }

        var selectedGameId = (selectedGame.SourceModel?.GameId ?? selectedGame.GameEntry.GameId ?? "").Trim();
        var buttonBefore = SelectedGameAction.InstallButtonText;
        try
        {
            var refreshedCard = TryRefreshVisibleGameCardsAfterInstall(selectedGameId);
            if (refreshedCard is not null)
            {
                selectedGame = refreshedCard;
            }

            await SelectGameCardAsync(selectedGame, cancellationToken, navigateHome: false, showPendingPopups: false);
            LogInfo(
                MainViewModelLogCategories.Install,
                $"badge refresh result=success game_id={NormalizeStatusCode(selectedGameId, "none")} card_refreshed={(refreshedCard is not null).ToString().ToLowerInvariant()} badge_after={NormalizeStatusCode(selectedGame.StatusBadge, "none")} button_before={NormalizeStatusCode(buttonBefore, "none")} button_after={NormalizeStatusCode(SelectedGameAction.InstallButtonText, "none")}");
        }
        catch (OperationCanceledException)
        {
            LogWarning(MainViewModelLogCategories.Install, "badge refresh result=canceled");
        }
        catch (Exception ex)
        {
            LogError(MainViewModelLogCategories.Install, "badge refresh result=failed", ex);
        }
    }

    private void ApplyStateUpdate(MainViewModelStateUpdate update)
    {
        ArgumentNullException.ThrowIfNull(update);
        DispatchStateUpdateFlowLogs(update);

        if (update.ScanFolderStateUpdate is { } scanFolderStateUpdate) ApplyScanFolderStateUpdate(scanFolderStateUpdate);
        if (!string.IsNullOrWhiteSpace(update.SettingsStatusText)) SettingsStatusText = update.SettingsStatusText;
        if (!string.IsNullOrWhiteSpace(update.ScanStatusText)) ScanStatusText = update.ScanStatusText;
        if (update.ShouldQueuePendingStartupNotice) _pendingAdministratorRelaunchCancelledNotice = true;
        if (update.RemoteCatalogErrorCode is not null)
        {
            _runtimeShellState.SetRemoteCatalogError(update.RemoteCatalogErrorCode, update.RemoteCatalogDetailErrorCode);
        }

        if (update.RuntimeData is not null
            || update.RemoteCatalog is not null
            || update.ModuleDownloadLinks is not null
            || update.OptiScalerVariantCatalog is not null)
        {
            _runtimeShellState.ApplyRemoteCatalog(
                update.RuntimeData,
                update.RemoteCatalog,
                update.ModuleDownloadLinks,
                update.OptiScalerVariantCatalog);
        }

        if (update.MatchByGameId is { } matchByGameId) _scannedGameState.ReplaceMatches(matchByGameId);
        if (update.TargetPathByGameId is { } targetPathByGameId) _scannedGameState.ReplaceTargetPaths(targetPathByGameId);
        if (update.VisibleGames is not null) ReplaceGameCards(update.VisibleGames);
        if (string.IsNullOrWhiteSpace(update.SupportLogMessage)) return;
        ApplyAppLog(true, update.SupportLogAsWarning, update.SupportLogCategory, update.SupportLogMessage);
    }

    private void ApplyDeferredStateUpdate(MainViewModelStateUpdate update)
    {
        ApplyStateUpdate(update);
        if (update.PopupRequest is { } popup) _dialogPresenter.ShowDeferred(_installPopupPresenter.BuildDialogRequest(popup, Strings));
        if (update.DialogRequest is { } dialog) _dialogPresenter.ShowDeferred(dialog);
    }

    private void DispatchStateUpdateFlowLogs(
        MainViewModelStateUpdate update,
        string defaultCategory = MainViewModelLogCategories.App)
    {
        if (update.FlowLogs.Count == 0) return;
        var fallbackCategory = string.IsNullOrWhiteSpace(update.FlowLogFallbackCategory)
            ? defaultCategory
            : update.FlowLogFallbackCategory;
        _flowLogDispatcher.Dispatch(update.FlowLogs, fallbackCategory);
    }

    private void ApplyAppLog(
        bool shouldWrite,
        bool asWarning,
        string? category,
        string? message)
    {
        if (!shouldWrite || string.IsNullOrWhiteSpace(message)) return;

        var normalizedCategory = NormalizeStatusCode(category, MainViewModelLogCategories.App);
        var normalizedMessage = message.Trim();
        if (asWarning) { LogWarning(normalizedCategory, normalizedMessage); return; }
        LogInfo(normalizedCategory, normalizedMessage);
    }

    private void ApplyInstallBusyState(
        bool inProgress,
        ShellInstallSelectionState? restoreSelectionState = null,
        string operationOverlayMessage = "")
    {
        ApplyBusyStateUpdate(
            _busyStateApplier.CreateInstallBusyState(
                inProgress,
                _isAppUpdateInProgress,
                _selectionState,
                restoreSelectionState,
                operationOverlayMessage));
    }

    private async Task ShowRemoteCatalogDialogOnceAsync(
        AppDialogRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalizedCode = NormalizeStatusCode(request.ErrorCode, MainViewModelStatusCodes.RuntimeDataFailed);
        if (!_remoteCatalogDialogGate.TryMarkShown(normalizedCode, MainViewModelStatusCodes.RuntimeDataFailed))
        {
            return;
        }

        await _dialogPresenter.ShowSafelyAsync(
            request with
            {
                ErrorCode = normalizedCode
            },
            cancellationToken);
    }

    private void RefreshLocalizedStrings()
    {
        Strings = _appStringsProvider.Get(SelectedLanguage);
        Scan.RefreshLocalization();
        Settings.RefreshLocalization();
        Home.RefreshLocalization();
        OnPropertyChanged(nameof(WindowTitleWithVersion));
    }

    private void ApplyLocalizationStateUpdate(LocalizationStateUpdate update)
    {
        ArgumentNullException.ThrowIfNull(update);
        if (!string.IsNullOrWhiteSpace(update.ScanStatusText)) ScanStatusText = update.ScanStatusText;
        if (!string.IsNullOrWhiteSpace(update.SettingsStatusText)) SettingsStatusText = update.SettingsStatusText;
        RuntimeHeader.ApplyTextUpdate(update.DeviceText, update.GpuText);
        if (update.ShouldRelocalizeScanFolders) RelocalizeScanFolderRows();
        if (update.ShouldRefreshRuntimeSummary) ApplyRuntimeSummaryStateUpdate(_runtimeSummaryStateController.Build(_runtimeShellState.LatestRuntimeContext, Strings));
    }

    private static string Format(string template, params object[] args) =>
        string.Format(CultureInfo.CurrentCulture, template ?? "", args ?? []);

    private void LogInfo(string category, string message) => _appLogger.Info(category, message);

    private void LogWarning(string category, string message) => _appLogger.Warning(category, message);

    private void LogError(string category, string message, Exception? exception = null)
    {
        if (exception is null)
        {
            _appLogger.Error(category, message);
            return;
        }

        _appLogger.Error(category, message, exception);
    }

    private static string NormalizeStatusCode(string? value, string fallback)
    {
        var normalized = (value ?? "").Trim();
        return string.IsNullOrWhiteSpace(normalized) ? fallback : normalized;
    }

    private OperatingSystemSupportState EnsureOperatingSystemPolicyEvaluated()
    {
        return _runtimeShellState.EnsureOperatingSystemEvaluated(
            _operatingSystemSupportPolicy,
            MainViewModelStatusCodes.Unknown);
    }

    private string GetCurrentAppVersion()
    {
        var value = _appVersionProvider.GetCurrentVersion();
        var normalized = (value ?? "").Trim();
        return string.IsNullOrWhiteSpace(normalized) ? "0.0.0" : normalized;
    }

}
