using System.Collections.ObjectModel;
using System.Linq;

using OptiClick.Core.Runtime;
using OptiClick.Wpf.Install.Archives;
using OptiClick.Wpf.Install.Flow;
using OptiClick.Wpf.Install.Planning;
using OptiClick.Wpf.Install.UiState;
using OptiClick.Wpf.Localization;
using OptiClick.Wpf.Shell.Flow;
using OptiClick.Wpf.Shell.Games;
using OptiClick.Wpf.Shell.Gpu;
using OptiClick.Wpf.Shell.Navigation;
using OptiClick.Wpf.Shell.Runtime;
using OptiClick.Wpf.Shell.Scan;
using OptiClick.Wpf.Shell.Selection;

namespace OptiClick.Wpf.ViewModels;

internal sealed class MainSelectionScanContextFactory
{
    private readonly MainSelectionScanContextFactoryInput _input;

    public MainSelectionScanContextFactory(MainSelectionScanContextFactoryInput input)
    {
        _input = input ?? throw new ArgumentNullException(nameof(input));
    }

    public ScanFlowRequest BuildScanRequest(IReadOnlyList<string> scanFolders)
    {
        return _input.FlowRequestFactory.BuildScanRequest(
            scanFolders,
            _input.Runtime.ReadRemoteCatalog(),
            _input.Runtime.ReadRuntimeContext(),
            ScanFlowText.FromAppStrings(_input.ReadStrings()),
            _input.ScannedGames.ReadMatchesByGameId(),
            _input.ScannedGames.ReadTargetPathsByGameId(),
            _input.Runtime.ReadModuleDownloadLinks(),
            _input.Runtime.ReadRemoteCatalogErrorCode());
    }

    public MainVisibleGameCardRefreshContext CreateVisibleGameCardRefreshContext()
    {
        return new MainVisibleGameCardRefreshContext
        {
            State = new MainVisibleGameCardRefreshState
            {
                ReadVisibleCards = _input.ReadVisibleCards,
                ReadSelectedGameId = () => _input.ReadSelectedGame()?.ResolvedGameId ?? "",
                SetSelectedGame = _input.SetSelectedGame,
                IsHomeNavigationSuppressed = _input.ReadSuppressHomeNavigationForAutoSelection,
                HasScannedMatches = () => _input.ScannedGames.ReadMatchesByGameId().Count > 0,
                ContainsScannedGameId = gameId => _input.ScannedGames.ContainsGameId(gameId),
                FindCurrentCardById = gameId => _input.ReadVisibleCards().FirstOrDefault(card => string.Equals(
                    card.ResolvedGameId,
                    gameId,
                    StringComparison.OrdinalIgnoreCase))
            },
            Services = new MainVisibleGameCardRefreshServices
            {
                ReplaceCardsAndSelectPreferred = (cards, previousSelectedGameId) =>
                    _input.Dependencies.GameCardSelectionStateController.ReplaceCardsAndSelectPreferred(
                        _input.ReadVisibleCards(),
                        cards,
                        previousSelectedGameId),
                QueueHomeCoverPrefetchInBackground = _input.QueueHomeCoverPrefetchInBackground,
                SelectGameAsync = (game, ct, shouldNavigateHome) =>
                    _input.SelectGameAsync(game, ct, shouldNavigateHome, true),
                IsMultiGpuBlocked = () => _input.Dependencies.GpuSelectionCoordinator.MultiGpuBlocked,
                CreateVisibleCardsFromScanMatches = CreateVisibleGameCardsFromScanMatches
            },
            Callbacks = new MainVisibleGameCardRefreshCallbacks
            {
                CreateVisibleCardFromScanMatch = CreateVisibleGameCardFromScanMatch,
                OnAutoSelectionError = _input.LogAutoSelectionError
            }
        };
    }

    public MainSelectionInteractionContext CreateSelectionInteractionContext()
    {
        return new MainSelectionInteractionContext
        {
            State = new MainSelectionInteractionState
            {
                IsInteractionBlocked = () => _input.IsInstallExecutionInProgress()
                                             || _input.IsAppUpdateInProgress(),
                SetSelectedGame = _input.SetSelectedGame,
                ResolveSelectedIndex = game => _input.ReadVisibleCards().IndexOf(game),
                IncrementSelectionVersion = _input.IncrementSelectionVersion,
                ReadSelectionState = _input.ReadSelectionState,
                ReadSelectionVersion = _input.ReadSelectionVersion,
                IsMultiGpuBlocked = () => _input.Dependencies.GpuSelectionCoordinator.MultiGpuBlocked,
                IsGpuSelectionPending = () => _input.Dependencies.GpuSelectionCoordinator.GpuSelectionPending,
                ReadSelectedGame = _input.ReadSelectedGame
            },
            Services = new MainSelectionInteractionServices
            {
                CanSelect = () => _input.Dependencies.GameSelectionFlowController.CanSelect,
                ApplyPrecheckRunningIntermediate = _input.ApplyPrecheckRunningIntermediate,
                ApplySelectionState = _input.ApplySelectionState,
                ApplySelectionBridgeState = _input.ApplySelectionBridgeState,
                BuildSelectionRequest = BuildSelectionRequest,
                SelectAsync = (request, cancellationToken) =>
                    _input.Dependencies.GameSelectionFlowController.SelectAsync(request, cancellationToken),
                DispatchFlowLogs = result => _input.DispatchSelectionLogs(result.Logs),
                NavigateHome = () => _input.SetCurrentView(ShellViewKind.Home)
            },
            Popup = new MainSelectionPopupCallbacks
            {
                ShowPendingSelectionPopupRequestsAsync = ShowPendingSelectionPopupRequestsAsync,
                ConfirmAllPendingSelectionPopups =
                    (selectionState, selectedGame) =>
                        _input.Dependencies.SelectionPopupCoordinator.ConfirmAll(selectionState, selectedGame),
                ConfirmNextPendingPopup = _input.Dependencies.SelectionPopupCoordinator.ConfirmNextPendingPopup
            }
        };
    }

    public MainSelectionRecomputeContext CreateSelectionRecomputeContext()
    {
        return new MainSelectionRecomputeContext
        {
            State = new MainSelectionRecomputeState
            {
                ReadSelectedGame = _input.ReadSelectedGame,
                ShouldShowPopupAfterSelection = false
            },
            Services = new MainSelectionRecomputeServices
            {
                RecomputeSelectionAsync = (game, ct, ctNavigateHome, _) =>
                    _input.SelectGameAsync(game, ct, ctNavigateHome, false)
            }
        };
    }

    private GameCardViewModel? CreateVisibleGameCardFromScanMatch(string selectedGameId)
    {
        if (_input.Dependencies.ShellGameCardViewModelFactory is null)
        {
            return null;
        }

        var normalizedSelectedGameId = (selectedGameId ?? "").Trim();
        if (string.IsNullOrWhiteSpace(normalizedSelectedGameId)
            || !_input.ScannedGames.ReadMatchesByGameId().TryGetValue(normalizedSelectedGameId, out var match)
            || match.Status != ShellGameMatchStatus.Matched)
        {
            return null;
        }

        var game = _input.Runtime.ReadRemoteCatalog().Games.FirstOrDefault(catalogGame => string.Equals(
            catalogGame.GameId?.Trim(),
            normalizedSelectedGameId,
            StringComparison.OrdinalIgnoreCase));
        if (game is null)
        {
            return null;
        }

        try
        {
            return _input.Dependencies.ShellGameCardViewModelFactory.CreateCards(
                    [game],
                    _input.Runtime.ReadRuntimeContext(),
                    _input.ScannedGames.ReadTargetPathsByGameId(),
                    _input.Runtime.ReadModuleDownloadLinks())
                .FirstOrDefault();
        }
        catch (Exception ex)
        {
            _input.LogCreateVisibleCardError(ex);
            return null;
        }
    }

    private IReadOnlyList<GameCardViewModel>? CreateVisibleGameCardsFromScanMatches()
    {
        if (_input.Dependencies.ShellGameCardViewModelFactory is null)
        {
            return null;
        }

        var matchedGames = _input.Dependencies.ScanVisibleGameResolver.ResolveMatchedCatalogGames(
            _input.Runtime.ReadRemoteCatalog(),
            _input.ScannedGames.ReadMatchesByGameId());
        try
        {
            return _input.Dependencies.ShellGameCardViewModelFactory.CreateCards(
                matchedGames,
                _input.Runtime.ReadRuntimeContext(),
                _input.ScannedGames.ReadTargetPathsByGameId(),
                _input.Runtime.ReadModuleDownloadLinks());
        }
        catch (Exception ex)
        {
            _input.LogCreateVisibleCardsError(ex);
            return null;
        }
    }

    private GameSelectionFlowRequest BuildSelectionRequest(
        GameCardViewModel game,
        int selectedIndex,
        ShellInstallSelectionState previousSelectionState)
    {
        return _input.FlowRequestFactory.BuildGameSelectionRequest(
            ShellGameCardMapper.Map(game),
            selectedIndex,
            _input.ReadVisibleCards().OfType<GameCardViewModel>().Select(ShellGameCardMapper.Map).ToArray(),
            previousSelectionState,
            _input.ScannedGames.ReadMatchesByGameId(),
            _input.ScannedGames.ReadTargetPathsByGameId(),
            _input.Runtime.ReadModuleDownloadLinks(),
            _input.Runtime.ReadArchiveReadiness(),
            _input.ReadSelectedLanguage(),
            _input.IsInstallExecutionInProgress(),
            _input.IsAppUpdateInProgress(),
            _input.Dependencies.GpuSelectionCoordinator.MultiGpuBlocked,
            _input.Dependencies.GpuSelectionCoordinator.GpuSelectionPending,
            _input.Runtime.ReadRemoteCatalogErrorCode());
    }

    private Task<SelectionPopupChainResult> ShowPendingSelectionPopupRequestsAsync(
        ShellInstallSelectionState selectionState,
        long selectionRequestVersion,
        CancellationToken cancellationToken)
    {
        return _input.Dependencies.SelectionPopupCoordinator.ShowPendingAsync(
            new SelectionPopupChainRequest
            {
                SelectionState = selectionState,
                SelectionVersion = selectionRequestVersion,
                ReadCurrentSelectionVersion = _input.ReadSelectionVersion,
                ApplySelectionState = ApplySelectionStateAndBridge,
                Text = SelectionPopupText.FromAppStrings(_input.ReadStrings()),
                SelectedGame = _input.ReadSelectedGame()
            },
            cancellationToken);
    }

    private void ApplySelectionStateAndBridge(ShellInstallSelectionState selectionState)
    {
        _input.ApplySelectionState(selectionState);
        _input.ApplySelectionBridgeState(selectionState);
    }
}

internal sealed record MainSelectionScanContextFactoryInput
{
    public required MainSelectionScanResolvedDependencies Dependencies { get; init; }
    public required MainViewModelFlowRequestFactory FlowRequestFactory { get; init; }
    public required MainSelectionScanRuntimePort Runtime { get; init; }
    public required MainSelectionScanScannedGamePort ScannedGames { get; init; }
    public required Func<AppStrings> ReadStrings { get; init; }
    public required Func<AppLanguage> ReadSelectedLanguage { get; init; }
    public required Func<ObservableCollection<GameCardViewModel>> ReadVisibleCards { get; init; }
    public required Func<GameCardViewModel?> ReadSelectedGame { get; init; }
    public required Action<GameCardViewModel?> SetSelectedGame { get; init; }
    public required Func<bool> IsInstallExecutionInProgress { get; init; }
    public required Func<bool> IsAppUpdateInProgress { get; init; }
    public required Func<bool> ReadSuppressHomeNavigationForAutoSelection { get; init; }
    public required Func<long> IncrementSelectionVersion { get; init; }
    public required Func<ShellInstallSelectionState> ReadSelectionState { get; init; }
    public required Func<long> ReadSelectionVersion { get; init; }
    public required Action<ShellInstallSelectionState> ApplySelectionState { get; init; }
    public required Action<ShellInstallSelectionState> ApplySelectionBridgeState { get; init; }
    public required Action ApplyPrecheckRunningIntermediate { get; init; }
    public required Action<ShellViewKind> SetCurrentView { get; init; }
    public required Action<string> QueueHomeCoverPrefetchInBackground { get; init; }
    public required Func<GameCardViewModel?, CancellationToken, bool, bool, Task> SelectGameAsync { get; init; }
    public required Action<IReadOnlyList<IFlowLogEntry>> DispatchSelectionLogs { get; init; }
    public required Action<Exception> LogAutoSelectionError { get; init; }
    public required Action<Exception> LogCreateVisibleCardError { get; init; }
    public required Action<Exception> LogCreateVisibleCardsError { get; init; }
}

internal sealed record MainSelectionScanRuntimePort
{
    public required Func<ShellGameCatalog> ReadRemoteCatalog { get; init; }
    public required Func<RuntimeContext> ReadRuntimeContext { get; init; }
    public required Func<ModuleDownloadLinkContext> ReadModuleDownloadLinks { get; init; }
    public required Func<ArchiveReadinessSnapshot> ReadArchiveReadiness { get; init; }
    public required Func<string> ReadRemoteCatalogErrorCode { get; init; }
}

internal sealed record MainSelectionScanScannedGamePort
{
    public required Func<IReadOnlyDictionary<string, ShellGameMatchResult>> ReadMatchesByGameId { get; init; }
    public required Func<IReadOnlyDictionary<string, string>> ReadTargetPathsByGameId { get; init; }
    public required Func<string, bool> ContainsGameId { get; init; }
}
