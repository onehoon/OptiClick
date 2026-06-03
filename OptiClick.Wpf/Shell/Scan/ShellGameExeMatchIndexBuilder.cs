using OptiClick.Wpf.Shell.Games;

namespace OptiClick.Wpf.Shell.Scan;

public sealed class ShellGameExeMatchIndexBuilder : IShellGameExeMatchIndexBuilder
{
    public ShellGameExeMatchIndex Build(ShellGameCatalog catalog)
    {
        if (catalog is null)
        {
            return ShellGameExeMatchIndex.Empty;
        }

        if (catalog.Games.Count == 0 && catalog.MatchRules.Count == 0)
        {
            return ShellGameExeMatchIndex.Empty;
        }

        var resolvedRules = ResolveMatchRules(catalog);
        if (resolvedRules.Count == 0)
        {
            return ShellGameExeMatchIndex.Empty;
        }

        var rulesByExecutableName = new Dictionary<string, List<ShellGameMatchRule>>(StringComparer.OrdinalIgnoreCase);
        foreach (var rule in resolvedRules)
        {
            foreach (var executableCandidate in rule.ExecutableCandidates)
            {
                var executableName = MatchExePatternParser.NormalizeExecutableName(executableCandidate);
                if (string.IsNullOrWhiteSpace(executableName))
                {
                    continue;
                }

                if (!rulesByExecutableName.TryGetValue(executableName, out var rules))
                {
                    rules = [];
                    rulesByExecutableName[executableName] = rules;
                }

                rules.Add(rule);
            }
        }

        var readonlyRuleIndex = rulesByExecutableName.ToDictionary(
            static pair => pair.Key,
            static pair => (IReadOnlyList<ShellGameMatchRule>)pair.Value,
            StringComparer.OrdinalIgnoreCase);
        var gamesByExeName = readonlyRuleIndex.ToDictionary(
            static pair => pair.Key,
            static pair => (IReadOnlyList<ShellGameCardModel>)pair.Value
                .Select(static rule => rule.Game)
                .GroupBy(static game => (game.GameId ?? "").Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(static group => group.First())
                .ToArray(),
            StringComparer.OrdinalIgnoreCase);
        var allowedExeNames = new HashSet<string>(readonlyRuleIndex.Keys, StringComparer.OrdinalIgnoreCase);

        return new ShellGameExeMatchIndex(gamesByExeName, readonlyRuleIndex, allowedExeNames);
    }

    private static IReadOnlyList<ShellGameMatchRule> ResolveMatchRules(ShellGameCatalog catalog)
    {
        if (catalog.MatchRules.Count > 0)
        {
            return catalog.MatchRules;
        }

        var fallbackRules = new List<ShellGameMatchRule>();
        foreach (var game in catalog.Games)
        {
            var requiredFiles = MatchExePatternParser.ParseRequiredFiles(game.MatchExe);
            if (requiredFiles.Count == 0)
            {
                continue;
            }

            var executableCandidates = MatchExePatternParser.ExtractExecutableCandidates(requiredFiles);
            if (executableCandidates.Count == 0)
            {
                continue;
            }

            fallbackRules.Add(new ShellGameMatchRule
            {
                GameId = (game.GameId ?? "").Trim(),
                MatchRuleKey = MatchExePatternParser.BuildMatchRuleKey(requiredFiles),
                RequiredFiles = requiredFiles,
                ExecutableCandidates = executableCandidates,
                Game = game
            });
        }

        return fallbackRules;
    }
}
