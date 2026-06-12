using System.IO;
using OptiClick.Core.Install;
using OptiClick.Wpf.Install.Flow;

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
        ModuleDownloadLinkContext moduleDownloadLinks)
    {
        var displayAlias = NormalizeDisplayAlias(extraBundleAlias);
        var lookupAlias = ModuleDownloadLinkAliasPolicy.Normalize(extraBundleAlias);
        if (string.IsNullOrWhiteSpace(lookupAlias))
        {
            return new ExtraBundlePreparationPlan
            {
                IsSkipped = true
            };
        }

        moduleDownloadLinks ??= ModuleDownloadLinkContext.Empty;
        if (!moduleDownloadLinks.TryResolveLink(lookupAlias, out var entry))
        {
            return new ExtraBundlePreparationPlan
            {
                Alias = displayAlias,
                ErrorCode = "missing_resource_entry"
            };
        }

        var url = entry.Url;
        if (string.IsNullOrWhiteSpace(url))
        {
            return new ExtraBundlePreparationPlan
            {
                Alias = displayAlias,
                ErrorCode = "missing_url"
            };
        }

        var filename = ResolveFilename(entry, url, displayAlias);
        var extension = Path.GetExtension(filename).ToLowerInvariant();
        if (extension is not ".zip" and not ".7z")
        {
            return new ExtraBundlePreparationPlan
            {
                Alias = displayAlias,
                Url = url,
                Filename = filename,
                ErrorCode = "unsupported_extension"
            };
        }

        return new ExtraBundlePreparationPlan
        {
            IsSuccess = true,
            Alias = displayAlias,
            Url = url,
            Filename = filename
        };
    }

    private static string NormalizeDisplayAlias(string value)
    {
        return (value ?? "").Trim().ToLowerInvariant();
    }

    private static string ResolveFilename(ModuleDownloadLinkEntry entry, string url, string alias)
    {
        var filename = Path.GetFileName(entry.Filename);
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
}
