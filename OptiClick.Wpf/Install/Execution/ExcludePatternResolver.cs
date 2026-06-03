using OptiClick.Wpf.Shell.Games;
using OptiClick.Wpf.Shell.RuntimeData;

namespace OptiClick.Wpf.Install.Execution;

public static class ExcludePatternResolver
{
    private static readonly string[] ModuleExcludeKeys =
    [
        RuntimeDataGameProfileKeys.ExcludeList,
        "__exclude_list__"
    ];

    public static IReadOnlyList<string> Resolve(
        ShellGameCardModel? game,
        IReadOnlyDictionary<string, object?>? moduleDownloadLinks)
    {
        var gamePatterns = ResolveFromGame(game);
        if (gamePatterns.Count > 0)
        {
            return gamePatterns;
        }

        return ResolveFromModuleDownloadLinks(moduleDownloadLinks);
    }

    private static IReadOnlyList<string> ResolveFromGame(ShellGameCardModel? game)
    {
        if (game?.ExcludeListPatterns?.Count > 0)
        {
            return ExcludeListPatternParser.Normalize(game.ExcludeListPatterns);
        }

        var raw = (game?.ExcludeListRaw ?? "").Trim();
        if (string.IsNullOrWhiteSpace(raw))
        {
            return Array.Empty<string>();
        }

        return ExcludeListPatternParser.Parse(raw);
    }

    private static IReadOnlyList<string> ResolveFromModuleDownloadLinks(
        IReadOnlyDictionary<string, object?>? moduleDownloadLinks)
    {
        if (moduleDownloadLinks is null || moduleDownloadLinks.Count == 0)
        {
            return Array.Empty<string>();
        }

        foreach (var key in ModuleExcludeKeys)
        {
            if (!moduleDownloadLinks.TryGetValue(key, out var entry))
            {
                continue;
            }

            var patterns = ParseEntry(entry);
            if (patterns.Count > 0)
            {
                return patterns;
            }
        }

        foreach (var pair in moduleDownloadLinks)
        {
            if (pair.Value is not IReadOnlyDictionary<string, object?> row)
            {
                continue;
            }

            var resourceId = ReadString(row, "resource_id");
            var resourceGroup = ReadString(row, "resource_group");
            if (!string.Equals(resourceId, RuntimeDataGameProfileKeys.ExcludeList, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(resourceGroup, RuntimeDataGameProfileKeys.ExcludeList, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var patterns = ParseEntry(row);
            if (patterns.Count > 0)
            {
                return patterns;
            }
        }

        return Array.Empty<string>();
    }

    private static IReadOnlyList<string> ParseEntry(object? entry)
    {
        return entry switch
        {
            null => Array.Empty<string>(),
            string raw => ExcludeListPatternParser.Parse(raw),
            IReadOnlyDictionary<string, object?> row => ParseDictionaryEntry(row),
            IDictionary<string, object?> mutableRow => ParseDictionaryEntry(ToReadOnlyDictionary(mutableRow)),
            IEnumerable<string> tokens => ExcludeListPatternParser.Normalize(tokens),
            IEnumerable<object?> tokens => ExcludeListPatternParser.Normalize(tokens.Select(ToTokenString)),
            _ => ExcludeListPatternParser.Parse(entry.ToString())
        };
    }

    private static IReadOnlyList<string> ParseDictionaryEntry(IReadOnlyDictionary<string, object?> row)
    {
        var excludeRaw = ReadString(row, RuntimeDataGameProfileKeys.ExcludeList);
        if (!string.IsNullOrWhiteSpace(excludeRaw))
        {
            return ExcludeListPatternParser.Parse(excludeRaw);
        }

        if (row.TryGetValue("patterns", out var patternsValue))
        {
            var parsed = ParseEntry(patternsValue);
            if (parsed.Count > 0)
            {
                return parsed;
            }
        }

        var filenameRaw = ReadString(row, "filename");
        if (!string.IsNullOrWhiteSpace(filenameRaw))
        {
            return ExcludeListPatternParser.Parse(filenameRaw);
        }

        var valueRaw = ReadString(row, "value");
        if (!string.IsNullOrWhiteSpace(valueRaw))
        {
            return ExcludeListPatternParser.Parse(valueRaw);
        }

        return Array.Empty<string>();
    }

    private static string ReadString(IReadOnlyDictionary<string, object?> row, string key)
    {
        if (!row.TryGetValue(key, out var value) || value is null)
        {
            return "";
        }

        return value.ToString()?.Trim() ?? "";
    }

    private static string ToTokenString(object? value)
    {
        return value?.ToString() ?? "";
    }

    private static IReadOnlyDictionary<string, object?> ToReadOnlyDictionary(IDictionary<string, object?> source)
    {
        var copy = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in source)
        {
            copy[pair.Key] = pair.Value;
        }

        return copy;
    }
}
