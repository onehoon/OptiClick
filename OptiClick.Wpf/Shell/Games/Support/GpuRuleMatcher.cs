using System.Text.RegularExpressions;

namespace OptiClick.Wpf.Shell.Games.Support;

public static class GpuRuleMatcher
{
    public static bool IsMatch(string ruleText, string gpuText)
    {
        var patterns = SplitPatterns(ruleText);
        if (patterns.Count == 0)
        {
            return false;
        }

        if (patterns.Any(static pattern => pattern is "all" or "true" or "yes" or "1"))
        {
            return true;
        }

        var normalizedGpu = NormalizeGpuText(gpuText);
        if (normalizedGpu is "" or "checking gpu..." or "unknown")
        {
            return false;
        }

        foreach (var pattern in patterns)
        {
            if (pattern is "null" or "none")
            {
                continue;
            }

            if (ContainsWildcard(pattern))
            {
                if (WildcardRegex(pattern).IsMatch(normalizedGpu))
                {
                    return true;
                }

                continue;
            }

            if (normalizedGpu.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    public static string StripTrademarkMarkers(string value)
    {
        var text = NormalizeSpace(value);
        if (text.Length == 0)
        {
            return "";
        }

        text = Regex.Replace(text, @"\((?:tm|r)\)", "", RegexOptions.IgnoreCase);
        text = text.Replace("\u2122", "").Replace("\u00AE", "");
        text = text.Replace("\u1d40", "").Replace("\u1d39", "");
        return NormalizeSpace(text);
    }

    private static List<string> SplitPatterns(string ruleText)
    {
        var stripped = StripTrademarkMarkersPreserveDelimiters(ruleText);
        if (string.IsNullOrWhiteSpace(stripped))
        {
            return [];
        }

        var normalized = stripped
            .Replace("\r", "\n", StringComparison.Ordinal)
            .Replace("\n", "|", StringComparison.Ordinal)
            .Replace(";", "|", StringComparison.Ordinal)
            .Replace(",", "|", StringComparison.Ordinal);

        return normalized
            .Split('|')
            .Select(static token => NormalizeSpace(token).ToLowerInvariant())
            .Where(static token => token.Length > 0)
            .ToList();
    }

    private static string NormalizeGpuText(string gpuText)
    {
        return StripTrademarkMarkers(gpuText).ToLowerInvariant();
    }

    private static string NormalizeSpace(string value)
    {
        return string.Join(" ", (value ?? "").Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)).Trim();
    }

    private static string StripTrademarkMarkersPreserveDelimiters(string value)
    {
        var text = value ?? "";
        if (text.Length == 0)
        {
            return "";
        }

        text = Regex.Replace(text, @"\((?:tm|r)\)", "", RegexOptions.IgnoreCase);
        text = text.Replace("\u2122", "").Replace("\u00AE", "");
        text = text.Replace("\u1d40", "").Replace("\u1d39", "");
        return text;
    }

    private static bool ContainsWildcard(string pattern)
    {
        return pattern.IndexOfAny(['*', '?', '[', ']']) >= 0;
    }

    private static Regex WildcardRegex(string pattern)
    {
        var regexText = "^" + ConvertWildcardToRegex(pattern) + "$";
        return new Regex(regexText, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    private static string ConvertWildcardToRegex(string pattern)
    {
        var builder = new System.Text.StringBuilder();
        for (var i = 0; i < pattern.Length; i++)
        {
            var ch = pattern[i];
            switch (ch)
            {
                case '*':
                    builder.Append(".*");
                    break;
                case '?':
                    builder.Append('.');
                    break;
                case '[':
                    {
                        var closingIndex = pattern.IndexOf(']', i + 1);
                        if (closingIndex > i + 1)
                        {
                            var classText = pattern.Substring(i, closingIndex - i + 1);
                            builder.Append(classText);
                            i = closingIndex;
                        }
                        else
                        {
                            builder.Append(@"\[");
                        }

                        break;
                    }
                default:
                    builder.Append(Regex.Escape(ch.ToString()));
                    break;
            }
        }

        return builder.ToString();
    }
}
