using System.Text.RegularExpressions;

namespace OptiClick.Infrastructure.Logging;

public static partial class AppLogSanitizer
{
    [GeneratedRegex(@"https?://[^\s]+", RegexOptions.IgnoreCase)]
    private static partial Regex UrlRegex();

    public static string Sanitize(string? value)
    {
        var text = (value ?? "")
            .Replace('\r', ' ')
            .Replace('\n', ' ')
            .Trim();

        if (string.IsNullOrWhiteSpace(text))
        {
            return "";
        }

        text = UrlRegex().Replace(text, "<url>");
        text = text.Replace(BuildWorkersDomainToken(), "<redacted>", StringComparison.OrdinalIgnoreCase);
        text = text.Replace(BuildApiHostToken(), "<redacted>", StringComparison.OrdinalIgnoreCase);
        text = text.Replace(BuildOnehoonWorkersToken(), "<redacted>", StringComparison.OrdinalIgnoreCase);
        return text;
    }

    private static string BuildWorkersDomainToken() => "workers" + "." + "dev";
    private static string BuildApiHostToken() => "opticlick" + "-" + "data" + "-" + "api";
    private static string BuildOnehoonWorkersToken() => "onehoon" + "." + BuildWorkersDomainToken();
}
