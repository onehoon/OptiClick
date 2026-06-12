using System.IO;
using OptiClick.Wpf.Install.Flow;

namespace OptiClick.Wpf.Install.Archives;

public sealed record RemoteArchiveEntry
{
    public string Url { get; init; } = "";
    public string Filename { get; init; } = "";
    public string Version { get; init; } = "";
    public string Sha256 { get; init; } = "";
}

public static class ArchiveEntryNormalizer
{
    public static RemoteArchiveEntry Normalize(ModuleDownloadLinkEntry? entry)
    {
        var safeEntry = entry ?? ModuleDownloadLinkEntry.Empty;
        var url = safeEntry.Url;
        var filename = ResolveArchiveFilename(safeEntry, url);
        var version = safeEntry.ReadFirstString("version");
        var sha256 = safeEntry.Sha256;
        return new RemoteArchiveEntry
        {
            Url = url,
            Filename = filename,
            Version = version,
            Sha256 = sha256
        };
    }

    public static RemoteArchiveEntry Normalize(object? rawEntry)
    {
        if (rawEntry is not IReadOnlyDictionary<string, object?> dictionary)
        {
            return new RemoteArchiveEntry();
        }

        var url = ReadFirstText(dictionary, "url", "download_url", "source_url");
        var filename = ResolveArchiveFilename(dictionary, url);
        var version = ReadText(dictionary, "version");
        var sha256 = ReadFirstText(dictionary, "sha256", "SHA256");
        return new RemoteArchiveEntry
        {
            Url = url,
            Filename = filename,
            Version = version,
            Sha256 = sha256
        };
    }

    public static string ResolveArchiveFilename(IReadOnlyDictionary<string, object?> entry, string normalizedUrl = "")
    {
        var filename = SanitizeFilename(ReadText(entry, "filename"));
        if (!string.IsNullOrWhiteSpace(filename))
        {
            return filename;
        }

        var version = SanitizeFilename(ReadText(entry, "version"));
        if (!string.IsNullOrWhiteSpace(version))
        {
            return version;
        }

        var url = string.IsNullOrWhiteSpace(normalizedUrl) ? ReadText(entry, "url") : normalizedUrl;
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            var name = SanitizeFilename(Path.GetFileName(Uri.UnescapeDataString(uri.AbsolutePath)));
            if (!string.IsNullOrWhiteSpace(name))
            {
                return name;
            }
        }

        return "";
    }

    public static string ResolveOptiScalerCacheVersion(RemoteArchiveEntry entry)
    {
        if (!string.IsNullOrWhiteSpace(entry.Version))
        {
            return entry.Version.Trim();
        }

        return entry.Filename.Trim();
    }

    public static string ResolveOptiScalerCacheEntryName(RemoteArchiveEntry entry)
    {
        return ArchivePayloadCacheEntryNames.ResolveOptiScalerEntryName(entry);
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

    private static string ReadFirstText(IReadOnlyDictionary<string, object?> values, params string[] keys)
    {
        foreach (var key in keys)
        {
            var value = ReadText(values, key);
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return "";
    }

    public static string ResolveArchiveFilename(ModuleDownloadLinkEntry entry, string normalizedUrl = "")
    {
        var safeEntry = entry ?? ModuleDownloadLinkEntry.Empty;
        var filename = SanitizeFilename(safeEntry.Filename);
        if (!string.IsNullOrWhiteSpace(filename))
        {
            return filename;
        }

        var version = SanitizeFilename(safeEntry.ReadFirstString("version"));
        if (!string.IsNullOrWhiteSpace(version))
        {
            return version;
        }

        var url = string.IsNullOrWhiteSpace(normalizedUrl) ? safeEntry.Url : normalizedUrl;
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            var name = SanitizeFilename(Path.GetFileName(Uri.UnescapeDataString(uri.AbsolutePath)));
            if (!string.IsNullOrWhiteSpace(name))
            {
                return name;
            }
        }

        return "";
    }

    private static string SanitizeFilename(string value)
    {
        var fileName = Path.GetFileName(value.Trim());
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return "";
        }

        if (fileName is "." or "..")
        {
            return "";
        }

        return fileName;
    }
}
