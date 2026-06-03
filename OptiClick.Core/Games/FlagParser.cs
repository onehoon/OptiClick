namespace OptiClick.Core.Games;

public static class FlagParser
{
    private static readonly HashSet<string> TrueTokens = new(StringComparer.OrdinalIgnoreCase)
    {
        "1", "true", "yes", "y", "on", "enabled", "enable", "o"
    };

    private static readonly HashSet<string> FalseTokens = new(StringComparer.OrdinalIgnoreCase)
    {
        "0", "false", "no", "n", "off", "disabled", "disable", "x"
    };

    public static bool? ParseNullableBoolean(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (TrueTokens.Contains(normalized))
        {
            return true;
        }

        if (FalseTokens.Contains(normalized))
        {
            return false;
        }

        return null;
    }

    public static bool ParseBoolean(string? value, bool defaultValue = false)
    {
        return ParseNullableBoolean(value) ?? defaultValue;
    }
}
