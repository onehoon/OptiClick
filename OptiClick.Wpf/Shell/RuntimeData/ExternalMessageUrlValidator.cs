namespace OptiClick.Wpf.Shell.RuntimeData;

internal static class ExternalMessageUrlValidator
{
    public static bool TryNormalizeHttpUrl(string? value, out string normalized)
    {
        normalized = "";
        var candidate = (value ?? "").Trim();
        if (string.IsNullOrWhiteSpace(candidate))
        {
            return false;
        }

        if (candidate.Any(static ch => char.IsControl(ch)))
        {
            return false;
        }

        if (!Uri.TryCreate(candidate, UriKind.Absolute, out var uri))
        {
            return false;
        }

        if (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp)
        {
            return false;
        }

        normalized = uri.ToString();
        return true;
    }
}
