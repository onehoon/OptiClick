namespace OptiClick.Wpf.Shell.Games.Support;

public static class SupportFlagParser
{
    private static readonly HashSet<string> TrueTokens = new(StringComparer.OrdinalIgnoreCase)
    {
        "1", "true", "yes", "y", "on"
    };

    private static readonly HashSet<string> FalseTokens = new(StringComparer.OrdinalIgnoreCase)
    {
        "0", "false", "no", "n", "off"
    };

    public static bool Parse(
        object? value,
        bool emptyDefault = false,
        bool unknownDefault = true,
        bool nativeXefgMeansFalse = true)
    {
        var parsed = ParseNullableCore(
            value,
            nativeXefgMeansFalse: nativeXefgMeansFalse,
            emptyDefault: emptyDefault,
            unknownDefault: unknownDefault);
        return parsed ?? unknownDefault;
    }

    public static bool? ParseNullable(
        object? value,
        bool nativeXefgMeansFalse = true)
    {
        return ParseNullableCore(
            value,
            nativeXefgMeansFalse: nativeXefgMeansFalse,
            emptyDefault: false,
            unknownDefault: false,
            returnNullForMissing: true);
    }

    private static bool? ParseNullableCore(
        object? value,
        bool nativeXefgMeansFalse,
        bool emptyDefault,
        bool unknownDefault,
        bool returnNullForMissing = false)
    {
        if (value is null)
        {
            return returnNullForMissing ? null : emptyDefault;
        }

        if (value is bool booleanValue)
        {
            return booleanValue;
        }

        if (value is byte or short or int or long or sbyte or ushort or uint or ulong)
        {
            try
            {
                return Convert.ToInt64(value) != 0;
            }
            catch
            {
                return unknownDefault;
            }
        }

        var text = value.ToString()?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(text))
        {
            return returnNullForMissing ? null : emptyDefault;
        }

        if (nativeXefgMeansFalse && text.Equals("native xefg", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (TrueTokens.Contains(text))
        {
            return true;
        }

        if (FalseTokens.Contains(text))
        {
            return false;
        }

        return unknownDefault;
    }
}
