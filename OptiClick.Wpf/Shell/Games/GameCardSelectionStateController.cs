using System.Collections.ObjectModel;
using System.Linq;
using OptiClick.Wpf.Collections;
using OptiClick.Wpf.ViewModels;

namespace OptiClick.Wpf.Shell.Games;

public sealed class GameCardSelectionStateController
{
    public GameCardViewModel? ReplaceCardsAndSelectPreferred(
        ObservableCollection<GameCardViewModel> games,
        IReadOnlyList<GameCardViewModel> newCards,
        string previousSelectedGameId)
    {
        ArgumentNullException.ThrowIfNull(games);
        ArgumentNullException.ThrowIfNull(newCards);

        foreach (var game in newCards)
        {
            game.IsSelected = false;
            game.IsDimmed = false;
        }

        ReplaceCards(games, newCards);

        if (games.Count == 0)
        {
            return null;
        }

        var normalizedPreviousId = (previousSelectedGameId ?? "").Trim();
        if (!string.IsNullOrWhiteSpace(normalizedPreviousId))
        {
            var sameId = games.FirstOrDefault(game =>
                string.Equals(
                    game.GameEntry.GameId?.Trim(),
                    normalizedPreviousId,
                    StringComparison.OrdinalIgnoreCase));
            if (sameId is not null)
            {
                return sameId;
            }
        }

        return null;
    }

    private static void ReplaceCards(
        ObservableCollection<GameCardViewModel> games,
        IReadOnlyList<GameCardViewModel> newCards)
    {
        if (games is BatchedObservableCollection<GameCardViewModel> batchedGames)
        {
            batchedGames.ReplaceAll(newCards);
            return;
        }

        games.Clear();
        foreach (var game in newCards)
        {
            games.Add(game);
        }
    }

    public void ApplySelectionState(
        ObservableCollection<GameCardViewModel> games,
        GameCardViewModel? selectedGame)
    {
        ArgumentNullException.ThrowIfNull(games);

        foreach (var item in games)
        {
            var isSelected = selectedGame is not null && ReferenceEquals(item, selectedGame);
            item.IsSelected = isSelected;
            item.IsDimmed = selectedGame is not null && !isSelected;
        }
    }
}
