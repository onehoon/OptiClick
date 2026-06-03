using System.Text.RegularExpressions;

namespace OptiClick.Core.Games.Support;

public sealed class GpuRuleMatcher
{
    public bool IsMatch(string rawGpuName, string rules)
    {
        var normalizedGpu = Normalize(rawGpuName);
        if (string.IsNullOrWhiteSpace(normalizedGpu) || string.IsNullOrWhiteSpace(rules))
        {
            return false;
        }

        foreach (var rawToken in SplitRules(rules))
        {
            var token = Normalize(rawToken);
            if (string.IsNullOrWhiteSpace(token) || token is "null" or "none")
            {
                continue;
            }

            if (token is "all" or "true" or "yes" or "1")
            {
                return true;
            }

            if (token.Contains('*'))
            {
                var pattern = "^" + Regex.Escape(token).Replace("\\*", ".*") + "$";
                if (Regex.IsMatch(normalizedGpu, pattern, RegexOptions.IgnoreCase))
                {
                    return true;
                }
                continue;
            }

            if (normalizedGpu.Contains(token, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    public static string Normalize(string value)
    {
        var normalized = value.ToLowerInvariant()
            .Replace("(tm)", "")
            .Replace("(r)", "")
            .Replace("\u2122", "")
            .Replace("\u00ae", "")
            .Trim();
        return Regex.Replace(normalized, "\\s+", " ");
    }

    private static IEnumerable<string> SplitRules(string rules)
    {
        return rules.Split(new[] { '|', ',', ';', '\n', '\r' }, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    }
}
