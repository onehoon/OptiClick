using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OptiClick.Wpf.ViewModels;

internal sealed class MainVisibleGameCardRefreshController
{
    public GameCardViewModel? ReplaceGameCards(
        MainVisibleGameCardRefreshContext context,
        IReadOnlyList<GameCardViewModel> cards,
        bool observeAutoSelection = true)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(cards);

        var currentCards = context.State.ReadVisibleCards();
        var previousSelectedGameId = context.State.ReadSelectedGameId();
        if (currentCards.Count > 0
            && currentCards[0].CardWidth > 0
            && currentCards[0].CardHeight > 0)
        {
            var preferredCardWidth = currentCards[0].CardWidth;
            var preferredCardHeight = currentCards[0].CardHeight;
            foreach (var card in cards)
            {
                card.ApplyCardSize(preferredCardWidth, preferredCardHeight);
            }
        }

        var selected = context.Services.ReplaceCardsAndSelectPreferred(cards, previousSelectedGameId);
        context.Services.QueueHomeCoverPrefetchInBackground("visible_games_replaced");
        if (selected is null)
        {
            context.State.SetSelectedGame(null);
            return null;
        }

        if (observeAutoSelection)
        {
            _ = ObserveAutoSelectionAsync(context, selected, !context.State.IsHomeNavigationSuppressed());
        }

        return selected;
    }

    public async Task ObserveAutoSelectionAsync(
        MainVisibleGameCardRefreshContext context,
        GameCardViewModel selected,
        bool navigateHome,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        try
        {
            await context.Services.SelectGameAsync(selected, cancellationToken, navigateHome);
        }
        catch (System.Exception ex)
        {
            context.Callbacks.OnAutoSelectionError(ex);
        }
    }

    public GameCardViewModel? RefreshVisibleGamesFromScanMatches(MainVisibleGameCardRefreshContext context)
    {
        if (context.Services.IsMultiGpuBlocked())
        {
            return ReplaceGameCards(context, []);
        }

        var cards = context.Services.CreateVisibleCardsFromScanMatches();
        if (cards is null)
        {
            return null;
        }

        return ReplaceGameCards(context, cards);
    }

    public GameCardViewModel? TryRefreshVisibleGameCardsAfterInstall(
        MainVisibleGameCardRefreshContext context,
        string selectedGameId)
    {
        var normalizedSelectedGameId = (selectedGameId ?? "").Trim();
        if (string.IsNullOrWhiteSpace(normalizedSelectedGameId)
            || !context.State.ContainsScannedGameId(normalizedSelectedGameId))
        {
            return null;
        }

        var refreshedCard = context.Callbacks.CreateVisibleCardFromScanMatch(normalizedSelectedGameId);
        if (refreshedCard is null)
        {
            return null;
        }

        var currentCard = context.State.FindCurrentCardById(normalizedSelectedGameId);
        if (currentCard is null)
        {
            return null;
        }

        currentCard.ApplyInstallStatusPresentationFrom(refreshedCard);
        return currentCard;
    }

    public GameCardViewModel? CreateVisibleGameCardFromScanMatch(
        MainVisibleGameCardRefreshContext context,
        string selectedGameId)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.Callbacks.CreateVisibleCardFromScanMatch(selectedGameId);
    }

    public IReadOnlyList<GameCardViewModel>? CreateVisibleGameCardsFromScanMatches(
        MainVisibleGameCardRefreshContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.Services.CreateVisibleCardsFromScanMatches();
    }
}

internal sealed class MainVisibleGameCardRefreshContext
{
    public required MainVisibleGameCardRefreshState State { get; init; }
    public required MainVisibleGameCardRefreshServices Services { get; init; }
    public required MainVisibleGameCardRefreshCallbacks Callbacks { get; init; }
}

internal sealed class MainVisibleGameCardRefreshState
{
    public required System.Func<IReadOnlyList<GameCardViewModel>> ReadVisibleCards { get; init; }
    public required System.Func<string> ReadSelectedGameId { get; init; }
    public required System.Action<GameCardViewModel?> SetSelectedGame { get; init; }
    public required System.Func<bool> IsHomeNavigationSuppressed { get; init; }
    public required System.Func<string, bool> ContainsScannedGameId { get; init; }
    public required System.Func<string, GameCardViewModel?> FindCurrentCardById { get; init; }
}

internal sealed class MainVisibleGameCardRefreshServices
{
    public required Func<IReadOnlyList<GameCardViewModel>, string, GameCardViewModel?> ReplaceCardsAndSelectPreferred { get; init; }
    public required Action<string> QueueHomeCoverPrefetchInBackground { get; init; }
    public required Func<GameCardViewModel, CancellationToken, bool, Task> SelectGameAsync { get; init; }
    public required Func<bool> IsMultiGpuBlocked { get; init; }
    public required Func<IReadOnlyList<GameCardViewModel>?> CreateVisibleCardsFromScanMatches { get; init; }
}

internal sealed class MainVisibleGameCardRefreshCallbacks
{
    public required Func<string, GameCardViewModel?> CreateVisibleCardFromScanMatch { get; init; }
    public required Action<System.Exception> OnAutoSelectionError { get; init; }
}
