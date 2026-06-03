using System.IO;

namespace OptiClick.Wpf.Install.Archives;

public sealed record RemoteArchiveEntry
{
    public string Url { get; init; } = "";
    public string Filename { get; init; } = "";
    public string Version { get; init; } = "";
}

public static class ArchiveEntryNormalizer
{
    public static RemoteArchiveEntry Normalize(object? rawEntry)
    {
        if (rawEntry is not IReadOnlyDictionary<string, object?> dictionary)
        {
            return new RemoteArchiveEntry();
        }

        var url = ReadText(dictionary, "url");
        var filename = ResolveArchiveFilename(dictionary, url);
        var version = ReadText(dictionary, "version");
        return new RemoteArchiveEntry
        {
            Url = url,
            Filename = filename,
            Version = version
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
        var candidate = Path.GetFileNameWithoutExtension(entry.Filename);
        if (string.IsNullOrWhiteSpace(candidate))
        {
            candidate = ResolveOptiScalerCacheVersion(entry);
        }

        var normalized = RegexReplaceInvalidCacheNameChars(candidate);
        return string.IsNullOrWhiteSpace(normalized) ? "optiscaler" : normalized;
    }

    private static string RegexReplaceInvalidCacheNameChars(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "";
        }

        var text = value.Trim();
        var chars = text.Select(static ch =>
            (ch is >= 'a' and <= 'z')
            || (ch is >= 'A' and <= 'Z')
            || (ch is >= '0' and <= '9')
            || ch is '.' or '_' or '-'
                ? ch
                : '_')
            .ToArray();
        return new string(chars).Trim('.', '_', '-');
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
