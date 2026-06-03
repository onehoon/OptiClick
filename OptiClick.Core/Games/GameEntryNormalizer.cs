using OptiClick.Core.Models;

namespace OptiClick.Core.Games;

public static class GameEntryNormalizer
{
    public static IReadOnlyList<string> NormalizeMatchFiles(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return Array.Empty<string>();
        }

        return raw.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(static token => token.ToLowerInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public static string ResolveMatchAnchor(IReadOnlyList<string> matchFiles)
    {
        return matchFiles.FirstOrDefault(static token => token.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            ?? matchFiles.FirstOrDefault()
            ?? "";
    }

    public static GameEntry Normalize(GameEntry game)
    {
        ArgumentNullException.ThrowIfNull(game);

        var normalizedMatchFiles = game.MatchFiles
            .Select(static value => (value ?? "").Trim().ToLowerInvariant())
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return game with
        {
            GameId = (game.GameId ?? "").Trim(),
            GameNameKr = (game.GameNameKr ?? "").Trim(),
            GameNameEn = (game.GameNameEn ?? "").Trim(),
            MatchFiles = normalizedMatchFiles,
            MatchAnchor = ResolveMatchAnchor(normalizedMatchFiles),
            SupportedGpu = (game.SupportedGpu ?? "").Trim(),
            OptiScalerDllName = (game.OptiScalerDllName ?? "").Trim(),
            ReframeworkUrl = (game.ReframeworkUrl ?? "").Trim(),
            SpecialK = (game.SpecialK ?? "").Trim(),
            ExtraBundle = (game.ExtraBundle ?? "").Trim()
        };
    }
}
