namespace OptiClick.Infrastructure.Remote;

internal static class RemoteRequestUriQueryBuilder
{
    public static Uri Build(Uri baseUri, IReadOnlyList<(string Key, string Value)> pairs)
    {
        var uriBuilder = new UriBuilder(baseUri)
        {
            Query = MergeQuery(baseUri.Query, pairs)
        };
        return uriBuilder.Uri;
    }

    public static string MergeQuery(string existingQuery, IReadOnlyList<(string Key, string Value)> pairs)
    {
        var normalizedExisting = (existingQuery ?? "").Trim();
        if (normalizedExisting.StartsWith("?", StringComparison.Ordinal))
        {
            normalizedExisting = normalizedExisting[1..];
        }

        var encoded = pairs
            .Select(static pair => $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value)}")
            .ToArray();
        if (encoded.Length == 0)
        {
            return normalizedExisting;
        }

        if (string.IsNullOrWhiteSpace(normalizedExisting))
        {
            return string.Join("&", encoded);
        }

        return normalizedExisting + "&" + string.Join("&", encoded);
    }
}
