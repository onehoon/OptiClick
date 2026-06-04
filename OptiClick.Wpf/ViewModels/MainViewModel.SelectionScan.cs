using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OptiClick.Wpf.Services;
using OptiClick.Wpf.Install.UiState;
using OptiClick.Wpf.Shell.Navigation;
using OptiClick.Wpf.Shell.Scan;
using OptiClick.Wpf.Shell.Selection;
using OptiClick.Wpf.Threading;

namespace OptiClick.Wpf.ViewModels;

public sealed partial class MainViewModel : ViewModelBase
{
    private void ShowScanView() { ScanStatusText = Strings.ScanChooseAndSave; SetCurrentView(ShellViewKind.Scan); }

    private GameCardViewModel? ReplaceGameCards(
        IReadOnlyList<GameCardViewModel> cards,
        bool observeAutoSelection = true)
    {
        var previousSelectedGameId = SelectedGame?.GameEntry?.GameId?.Trim() ?? "";
        var hasPreferredCardSize = Games.Count > 0
                                   && Games[0].CardWidth > 0
                                   && Games[0].CardHeight > 0;
        if (hasPreferredCardSize)
        {
            var preferredCardWidth = Games[0].CardWidth;
            var preferredCardHeight = Games[0].CardHeight;
            foreach (var card in cards)
            {
                card.ApplyCardSize(preferredCardWidth, preferredCardHeight);
            }
        }

        var selected = _gameCardSelectionStateController.ReplaceCardsAndSelectPreferred(
            Games,
            cards,
            previousSelectedGameId);
        QueueHomeCoverPrefetchInBackground("visible_games_replaced");
        if (selected is null)
        {
            SetSelectedGame(null);
            return null;
        }

        if (observeAutoSelection)
        {
            _ = ObserveAutoSelectionAsync(
                selected,
                navigateHome: !_suppressHomeNavigationForAutoSelection);
        }

        return selected;
    }

    private async Task ObserveAutoSelectionAsync(GameCardViewModel selected, bool navigateHome)
    {
        try
        {
            await SelectGameCardAsync(selected, CancellationToken.None, navigateHome);
        }
        catch (Exception ex)
        {
            LogError(
                MainViewModelLogCategories.Selection,
                "auto selection failed while replacing game cards",
                ex);
        }
    }

    private void SetSelectedGame(GameCardViewModel? selectedGame)
    {
        _gameCardSelectionStateController.ApplySelectionState(Games, selectedGame);
        SelectedGame = selectedGame;
    }

    public void ConfirmNextPendingPopup()
    {
        _selectionState = _selectionPopupCoordinator.ConfirmNextPendingPopup(_selectionState);
        SelectedGameAction.ApplySelectionBridgeState(_selectionState);
    }

    private async Task SelectGameCardAsync(
        GameCardViewModel game,
        CancellationToken cancellationToken = default,
        bool navigateHome = true,
        bool showPendingPopups = true)
    {
        if (_isInstallExecutionInProgress || _isAppUpdateInProgress)
        {
            return;
        }

        SetSelectedGame(game);
        if (navigateHome)
        {
            SetCurrentView(ShellViewKind.Home);
        }

        if (!_gameSelectionFlowController.CanSelect)
        {
            return;
        }

        var selectedIndex = Games.IndexOf(game);
        var selectionVersion = Interlocked.Increment(ref _selectionRequestVersion);
        var previousSelectionState = _selectionState;

        SelectedGameAction.ApplyPrecheckRunningIntermediate();
        _selectionState = new ShellInstallSelectionState
        {
            SelectedIndex = selectedIndex >= 0 ? selectedIndex : null,
            SelectedGameId = (game.GameEntry.GameId ?? "").Trim(),
            PopupConfirmed = false,
            PrecheckRunning = true,
            PrecheckOk = false,
            MultiGpuBlocked = _gpuSelectionCoordinator.MultiGpuBlocked,
            GpuSelectionPending = _gpuSelectionCoordinator.GpuSelectionPending,
            InstallButtonPresentation = new InstallButtonPresentation
            {
                IsEnabled = false,
                ReasonCode = InstallButtonReasonCodes.InstallPrecheckRunning,
                Text = ""
            }
        };
        SelectedGameAction.ApplySelectionBridgeState(_selectionState);
        var result = await _gameSelectionFlowController.SelectAsync(
            _flowRequestFactory.BuildGameSelectionRequest(
                game,
                selectedIndex,
                Games,
                previousSelectionState,
                _scannedGameState.MatchByGameId,
                _scannedGameState.TargetPathByGameId,
                _runtimeShellState.ModuleDownloadLinks,
                _runtimeShellState.LatestArchiveReadiness,
                SelectedLanguage,
                _isInstallExecutionInProgress,
                _isAppUpdateInProgress,
                _gpuSelectionCoordinator.MultiGpuBlocked,
                _gpuSelectionCoordinator.GpuSelectionPending,
                _runtimeShellState.LatestRemoteCatalogErrorCode),
            cancellationToken);
        _flowLogDispatcher.Dispatch(result.Logs, MainViewModelLogCategories.Selection);

        if (selectionVersion != Volatile.Read(ref _selectionRequestVersion))
        {
            return;
        }

        if (!result.DidRun || !result.IsSuccess || result.IsStaleIgnored)
        {
            return;
        }

        _selectionState = result.SelectionState;
        SelectedGameAction.ApplySelectionBridgeState(_selectionState);
        if (showPendingPopups)
        {
            await ShowPendingSelectionPopupRequestsAsync(selectionVersion, cancellationToken);
            return;
        }

        ConfirmAllPendingSelectionPopups();
    }

    // Match result is intentionally not synthesized from card selection alone.
    // It must come from an actual scan/match pipeline.

    private IReadOnlyList<ScanFolderRowViewModel> LoadAddedScanFoldersFromManifest(
        IReadOnlyCollection<ScanFolderRowViewModel> defaultFolders,
        ScanFolderActionController scanFolderActionController)
    {
        var update = _resultApplier.CreateScanFolderActionStateUpdate(
            scanFolderActionController.LoadAddedFoldersFromManifest(
                defaultFolders,
                Strings,
                AddedFolderStatusBrush,
                MissingFolderStatusBrush));
        DispatchStateUpdateFlowLogs(update, MainViewModelLogCategories.Scan);
        return update.ScanFolderStateUpdate?.AddedFolders ?? [];
    }

    private void RelocalizeScanFolderRows()
    {
        Scan.RelocalizeScanFolderRows();
    }

    private void ApplyScanFolderStateUpdate(ScanFolderStateUpdate update)
    {
        Scan.ApplyScanFolderStateUpdate(update);
    }

    public async Task SaveAndStartScanAsync(CancellationToken cancellationToken = default)
    {
        await Scan.SaveAndStartScanAsync(cancellationToken);
    }

    private async Task RunStartupAutoScanAsync(CancellationToken cancellationToken = default)
    {
        await Scan.RunStartupAutoScanAsync(cancellationToken);
    }

    private ScanFlowRequest BuildScanRequest(IReadOnlyList<string> scanFolders)
    {
        return _flowRequestFactory.BuildScanRequest(
            scanFolders,
            _runtimeShellState.LatestRemoteCatalog,
            _runtimeShellState.LatestRuntimeContext,
            Strings,
            _scannedGameState.MatchByGameId,
            _scannedGameState.TargetPathByGameId,
            _runtimeShellState.ModuleDownloadLinks,
            _runtimeShellState.LatestRemoteCatalogErrorCode);
    }

    private GameCardViewModel? RefreshVisibleGamesFromScanMatches()
    {
        if (_gpuSelectionCoordinator.MultiGpuBlocked)
        {
            ReplaceGameCards([]);
            return null;
        }

        var cards = CreateVisibleGameCardsFromScanMatches();
        if (cards is null)
        {
            return null;
        }

        return ReplaceGameCards(cards);
    }

    private GameCardViewModel? TryRefreshVisibleGameCardsAfterInstall(string selectedGameId)
    {
        var normalizedSelectedGameId = (selectedGameId ?? "").Trim();
        if (string.IsNullOrWhiteSpace(normalizedSelectedGameId)
            || !_scannedGameState.ContainsGameId(normalizedSelectedGameId))
        {
            return null;
        }

        var refreshedCard = CreateVisibleGameCardFromScanMatch(normalizedSelectedGameId);
        if (refreshedCard is null)
        {
            return null;
        }

        var currentCard = Games.FirstOrDefault(card => string.Equals(
            card.GameEntry.GameId?.Trim(),
            normalizedSelectedGameId,
            StringComparison.OrdinalIgnoreCase));
        if (currentCard is null)
        {
            return null;
        }

        currentCard.ApplyInstallStatusPresentationFrom(refreshedCard);
        return currentCard;
    }

    private GameCardViewModel? CreateVisibleGameCardFromScanMatch(string selectedGameId)
    {
        if (_shellGameCardViewModelFactory is null)
        {
            return null;
        }

        var normalizedSelectedGameId = (selectedGameId ?? "").Trim();
        if (string.IsNullOrWhiteSpace(normalizedSelectedGameId)
            || !_scannedGameState.MatchByGameId.TryGetValue(normalizedSelectedGameId, out var match)
            || match.Status != ShellGameMatchStatus.Matched)
        {
            return null;
        }

        var game = _runtimeShellState.LatestRemoteCatalog.Games.FirstOrDefault(catalogGame => string.Equals(
            catalogGame.GameId?.Trim(),
            normalizedSelectedGameId,
            StringComparison.OrdinalIgnoreCase));
        if (game is null)
        {
            return null;
        }

        try
        {
            return _shellGameCardViewModelFactory.CreateCards(
                    [game],
                    _runtimeShellState.LatestRuntimeContext,
                    _scannedGameState.TargetPathByGameId,
                    _runtimeShellState.ModuleDownloadLinks)
                .FirstOrDefault();
        }
        catch (Exception ex)
        {
            LogError(MainViewModelLogCategories.Scan, "failed to create visible card from scan match", ex);
            return null;
        }
    }

    private IReadOnlyList<GameCardViewModel>? CreateVisibleGameCardsFromScanMatches()
    {
        if (_shellGameCardViewModelFactory is null)
        {
            return null;
        }

        var matchedGames = _scanVisibleGameResolver.ResolveMatchedCatalogGames(
            _runtimeShellState.LatestRemoteCatalog,
            _scannedGameState.MatchByGameId);
        IReadOnlyList<GameCardViewModel> cards;
        try
        {
            cards = _shellGameCardViewModelFactory.CreateCards(
                matchedGames,
                _runtimeShellState.LatestRuntimeContext,
                _scannedGameState.TargetPathByGameId,
                _runtimeShellState.ModuleDownloadLinks);
        }
        catch (Exception ex)
        {
            LogError(MainViewModelLogCategories.Scan, "failed to create visible cards from scan matches", ex);
            return null;
        }

        return cards;
    }

    private async Task RecomputeSelectionAfterScanAsync(CancellationToken cancellationToken, bool navigateHome)
    {
        if (SelectedGame is null) return;
        await SelectGameCardAsync(SelectedGame, cancellationToken, navigateHome, showPendingPopups: false);
    }

    private async Task ShowPendingSelectionPopupRequestsAsync(
        long selectionVersion,
        CancellationToken cancellationToken)
    {
        var result = await _selectionPopupCoordinator.ShowPendingAsync(
            new SelectionPopupChainRequest
            {
                SelectionState = _selectionState,
                SelectionVersion = selectionVersion,
                ReadCurrentSelectionVersion = () => Volatile.Read(ref _selectionRequestVersion),
                ApplySelectionState = ApplySelectionPopupState,
                Strings = Strings,
                SelectedGame = SelectedGame
            },
            cancellationToken);
        if (result.DidChange)
        {
            ApplySelectionPopupState(result.SelectionState);
        }
    }

    private void ConfirmAllPendingSelectionPopups()
    {
        var result = _selectionPopupCoordinator.ConfirmAll(_selectionState, SelectedGame);
        if (result.DidChange)
        {
            ApplySelectionPopupState(result.SelectionState);
        }
    }

    private void ApplySelectionPopupState(ShellInstallSelectionState selectionState)
    {
        _selectionState = selectionState;
        SelectedGameAction.ApplySelectionBridgeState(_selectionState);
    }

}
