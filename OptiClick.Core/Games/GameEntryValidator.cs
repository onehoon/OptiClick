using OptiClick.Core.Models;

namespace OptiClick.Core.Games;

public static class GameEntryValidator
{
    public static bool ShouldInclude(string gameId, IReadOnlyList<string> matchFiles, bool enabled)
    {
        if (!enabled)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(gameId))
        {
            return false;
        }

        return matchFiles.Count > 0;
    }

    public static bool IsValid(GameEntry game)
    {
        ArgumentNullException.ThrowIfNull(game);

        if (string.IsNullOrWhiteSpace(game.GameId))
        {
            return false;
        }

        if (game.MatchFiles.Count == 0)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(game.MatchAnchor))
        {
            return false;
        }

        return true;
    }
}
