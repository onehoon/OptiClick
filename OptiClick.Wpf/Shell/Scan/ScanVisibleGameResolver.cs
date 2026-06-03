using OptiClick.Wpf.Shell.Games;

namespace OptiClick.Wpf.Shell.Scan;

public sealed class ScanVisibleGameResolver
{
    public IReadOnlyList<ShellGameCardModel> ResolveMatchedCatalogGames(
        ShellGameCatalog catalog,
        IReadOnlyDictionary<string, ShellGameMatchResult> matchByGameId)
    {
        var safeCatalog = catalog ?? ShellGameCatalog.Empty;
        var safeMatchByGameId = matchByGameId
                                ?? new Dictionary<string, ShellGameMatchResult>(StringComparer.OrdinalIgnoreCase);
        if (safeCatalog.Games.Count == 0 || safeMatchByGameId.Count == 0)
        {
            return [];
        }

        var matchedGameIds = safeMatchByGameId
            .Where(static pair => pair.Value.Status == ShellGameMatchStatus.Matched)
            .Select(static pair => pair.Key)
            .Where(static key => !string.IsNullOrWhiteSpace(key))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (matchedGameIds.Count == 0)
        {
            return [];
        }

        return safeCatalog.Games
            .Where(game =>
            {
                var gameId = (game.GameId ?? "").Trim();
                return !string.IsNullOrWhiteSpace(gameId)
                       && matchedGameIds.Contains(gameId);
            })
            .ToArray();
    }
}
