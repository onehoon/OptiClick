namespace OptiClick.Core.Install;

public static class InstallExcludeListPatternParser
{
    private static readonly char[] Delimiters = ['|', ',', ';', '\r', '\n'];

    public static IReadOnlyList<string> Parse(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return Array.Empty<string>();
        }

        return SplitTokens(raw);
    }

    public static IReadOnlyList<string> Normalize(IEnumerable<string>? rawValues)
    {
        if (rawValues is null)
        {
            return Array.Empty<string>();
        }

        var tokens = new List<string>();
        foreach (var raw in rawValues)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                continue;
            }

            tokens.AddRange(SplitTokens(raw));
        }

        return tokens
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IReadOnlyList<string> SplitTokens(string raw)
    {
        return raw
            .Split(Delimiters, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(static token => !string.IsNullOrWhiteSpace(token))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
