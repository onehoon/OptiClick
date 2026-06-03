using System.IO;

namespace OptiClick.Wpf.Install.Archives;

public sealed record ExtraBundlePreparationPlan
{
    public bool IsSkipped { get; init; }
    public bool IsSuccess { get; init; }
    public string Alias { get; init; } = "";
    public string Url { get; init; } = "";
    public string Filename { get; init; } = "";
    public string ErrorCode { get; init; } = "";
}

public sealed class ExtraBundlePreparationPlanner
{
    public ExtraBundlePreparationPlan CreatePlan(
        string extraBundleAlias,
        IReadOnlyDictionary<string, object?> moduleDownloadLinks)
    {
        var alias = NormalizeAlias(extraBundleAlias);
        if (string.IsNullOrWhiteSpace(alias))
        {
            return new ExtraBundlePreparationPlan
            {
                IsSkipped = true
            };
        }

        if (!moduleDownloadLinks.TryGetValue(alias, out var rawEntry)
            || rawEntry is not IReadOnlyDictionary<string, object?> entry)
        {
            return new ExtraBundlePreparationPlan
            {
                Alias = alias,
                ErrorCode = "missing_resource_entry"
            };
        }

        var url = ReadText(entry, "url");
        if (string.IsNullOrWhiteSpace(url))
        {
            return new ExtraBundlePreparationPlan
            {
                Alias = alias,
                ErrorCode = "missing_url"
            };
        }

        var filename = ResolveFilename(entry, url, alias);
        var extension = Path.GetExtension(filename).ToLowerInvariant();
        if (extension is not ".zip" and not ".7z")
        {
            return new ExtraBundlePreparationPlan
            {
                Alias = alias,
                Url = url,
                Filename = filename,
                ErrorCode = "unsupported_extension"
            };
        }

        return new ExtraBundlePreparationPlan
        {
            IsSuccess = true,
            Alias = alias,
            Url = url,
            Filename = filename
        };
    }

    private static string NormalizeAlias(string value)
    {
        return (value ?? "").Trim().ToLowerInvariant();
    }

    private static string ResolveFilename(IReadOnlyDictionary<string, object?> entry, string url, string alias)
    {
        var filename = Path.GetFileName(ReadText(entry, "filename"));
        if (!string.IsNullOrWhiteSpace(filename))
        {
            return filename;
        }

        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            var fromUrl = Path.GetFileName(Uri.UnescapeDataString(uri.AbsolutePath));
            if (!string.IsNullOrWhiteSpace(fromUrl))
            {
                return fromUrl;
            }
        }

        return $"{alias}.7z";
    }

    private static string ReadText(IReadOnlyDictionary<string, object?> values, string key)
    {
        if (!values.TryGetValue(key, out var value) || value is null)
        {
            return "";
        }

        return value switch
        {
            string text => text.Trim(),
            _ => value.ToString()?.Trim() ?? ""
        };
    }
}
