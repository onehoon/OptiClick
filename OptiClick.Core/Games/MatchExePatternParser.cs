namespace OptiClick.Core.Games;

public static class MatchExePatternParser
{
    public static IReadOnlyList<string> Parse(string matchExe)
    {
        return ParseRequiredFiles(matchExe);
    }

    public static IReadOnlyList<string> ParseRequiredFiles(string matchExe)
    {
        var text = (matchExe ?? "").Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        return text
            .Split(['|', ';', ',', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Select(static item => NormalizeExecutableName(item))
            .Where(static item => item.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public static IReadOnlyList<string> ExtractExecutableCandidates(IReadOnlyList<string> requiredFiles)
    {
        if (requiredFiles is null || requiredFiles.Count == 0)
        {
            return [];
        }

        return requiredFiles
            .Where(static token => token.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public static string BuildMatchRuleKey(IReadOnlyList<string> requiredFiles)
    {
        if (requiredFiles is null || requiredFiles.Count == 0)
        {
            return "";
        }

        return string.Join("|", requiredFiles.Select(static token => token.ToLowerInvariant()));
    }

    public static string NormalizeExecutableName(string value)
    {
        var text = (value ?? "").Trim().Trim('"', '\'');
        if (string.IsNullOrWhiteSpace(text))
        {
            return "";
        }

        var fileName = System.IO.Path.GetFileName(text);
        return string.IsNullOrWhiteSpace(fileName) ? text : fileName.Trim();
    }
}
