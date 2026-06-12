using System.Collections.ObjectModel;
using OptiClick.Wpf.ViewModels;
using OptiClick.Core.Models;
using OptiClick.Core.Runtime;
using OptiClick.Wpf.Install.Archives;
using OptiClick.Wpf.Install.Flow;
using OptiClick.Wpf.Install.Presentation;
using OptiClick.Wpf.Install.UiState;
using OptiClick.Wpf.Localization;
using OptiClick.Wpf.Logging;
using OptiClick.Wpf.Models;
using OptiClick.Wpf.Services;
using OptiClick.Wpf.Shell.Actions;
using OptiClick.Wpf.Shell.Flow;
using OptiClick.Wpf.Shell.Games;
using OptiClick.Wpf.Shell.Gpu;
using OptiClick.Wpf.Shell.Localization;
using OptiClick.Wpf.Shell.Navigation;
using OptiClick.Wpf.Shell.Runtime;
using OptiClick.Wpf.Shell.RuntimeData;
using OptiClick.Wpf.Shell.Scan;
using OptiClick.Wpf.Shell.Selection;
using OptiClick.Wpf.Shell.Settings;
using OptiClick.Wpf.Shell.Startup;
using OptiClick.Wpf.Shell.Update;
using OptiClick.Wpf.Threading;
using OptiClick.Wpf.ViewModels.Sections;
using OptiClick.Wpf.ViewModels.Sections.Scan;

namespace OptiClick.Wpf.ViewModels.Features.Selection;
internal sealed class MainSelectionFeatureFacade
{
    private readonly MainLanguageChangeController _languageChangeController;
    private readonly MainLanguageChangeContextFactory _languageChangeContextFactory;
    private readonly MainSelectionInteractionController _interactionController;
    private readonly MainSelectionRecomputeController _recomputeController;
    private readonly MainSelectionScanContextFactory _contextFactory;
    private readonly MainVisibleGameCardRefreshController _visibleGameCardRefreshController;
    private readonly GameCardSelectionStateController _gameCardSelectionStateController;

    public MainSelectionFeatureFacade(
        MainLanguageChangeController languageChangeController,
        MainLanguageChangeContextFactory languageChangeContextFactory,
        MainSelectionInteractionController interactionController,
        MainSelectionRecomputeController recomputeController,
        MainSelectionScanContextFactory contextFactory,
        MainVisibleGameCardRefreshController visibleGameCardRefreshController,
        GameCardSelectionStateController gameCardSelectionStateController)
    {
        _languageChangeController = languageChangeController;
        _languageChangeContextFactory = languageChangeContextFactory;
        _interactionController = interactionController;
        _recomputeController = recomputeController;
        _contextFactory = contextFactory;
        _visibleGameCardRefreshController = visibleGameCardRefreshController;
        _gameCardSelectionStateController = gameCardSelectionStateController;
    }

    public Task ApplyLanguageChangeAsync(AppLanguage language, CancellationToken cancellationToken)
    {
        return _languageChangeController.ApplyAsync(
            _languageChangeContextFactory.Create(language),
            cancellationToken);
    }

    public GameCardViewModel? ReplaceGameCards(
        IReadOnlyList<GameCardViewModel> cards,
        bool observeAutoSelection = true)
    {
        return _visibleGameCardRefreshController.ReplaceGameCards(
            _contextFactory.CreateVisibleGameCardRefreshContext(),
            cards,
            observeAutoSelection);
    }

    public Task ObserveAutoSelectionAsync(
        GameCardViewModel selected,
        bool navigateHome,
        CancellationToken cancellationToken = default)
    {
        return _visibleGameCardRefreshController.ObserveAutoSelectionAsync(
            _contextFactory.CreateVisibleGameCardRefreshContext(),
            selected,
            navigateHome,
            cancellationToken);
    }

    public void ApplySelectionState(
        ObservableCollection<GameCardViewModel> games,
        GameCardViewModel? selectedGame)
    {
        _gameCardSelectionStateController.ApplySelectionState(games, selectedGame);
    }

    public void ConfirmNextPendingPopup()
    {
        _interactionController.ConfirmNextPendingPopup(
            _contextFactory.CreateSelectionInteractionContext());
    }

    public Task SelectGameCardAsync(
        GameCardViewModel? game,
        CancellationToken cancellationToken = default,
        bool navigateHome = true,
        bool showPendingPopups = true)
    {
        return _interactionController.SelectGameCardAsync(
            _contextFactory.CreateSelectionInteractionContext(),
            game,
            cancellationToken,
            navigateHome,
            showPendingPopups);
    }

    public Task RecomputeSelectionAfterScanAsync(CancellationToken cancellationToken, bool navigateHome)
    {
        return _recomputeController.RecomputeAsync(
            _contextFactory.CreateSelectionRecomputeContext(),
            cancellationToken,
            navigateHome);
    }

    public GameCardViewModel? RefreshVisibleGamesFromScanMatches()
    {
        return _visibleGameCardRefreshController.RefreshVisibleGamesFromScanMatches(
            _contextFactory.CreateVisibleGameCardRefreshContext());
    }

    public Task<bool> RefreshVisibleGamesAfterLanguageChangeAsync(CancellationToken cancellationToken)
    {
        return _visibleGameCardRefreshController.RefreshVisibleGamesAfterLanguageChangeAsync(
            _contextFactory.CreateVisibleGameCardRefreshContext(),
            cancellationToken);
    }

    public GameCardViewModel? TryRefreshVisibleGameCardsAfterInstall(string selectedGameId)
    {
        return _visibleGameCardRefreshController.TryRefreshVisibleGameCardsAfterInstall(
            _contextFactory.CreateVisibleGameCardRefreshContext(),
            selectedGameId);
    }

    public ScanFlowRequest BuildScanRequest(IReadOnlyList<string> scanFolders)
    {
        return _contextFactory.BuildScanRequest(scanFolders);
    }
}
