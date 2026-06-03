using System.IO;
using System.Text.RegularExpressions;

namespace OptiClick.Infrastructure.Install.Components;

public static class InstallerExecutionHelpers
{
    private static readonly Regex UnsafeCharsPattern = new("[^A-Za-z0-9._-]+", RegexOptions.Compiled);

    public static string ExtractModuleUrl(IReadOnlyDictionary<string, object?> moduleDownloadLinks, string moduleKey)
    {
        if (!moduleDownloadLinks.TryGetValue(moduleKey, out var rawEntry)
            || rawEntry is not IReadOnlyDictionary<string, object?> entry)
        {
            return "";
        }

        return ReadString(entry, "url");
    }

    public static string ReadString(IReadOnlyDictionary<string, object?> values, string key)
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

    public static string NormalizeRelativeDllPath(string destinationRelPath)
    {
        var normalized = (destinationRelPath ?? "").Trim().Replace('\\', '/');
        while (normalized.StartsWith("./", StringComparison.Ordinal))
        {
            normalized = normalized[2..];
        }

        normalized = normalized.Trim('/');
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new InvalidOperationException("Invalid DLL destination path: (empty)");
        }

        if (Path.IsPathRooted(normalized))
        {
            throw new InvalidOperationException($"Invalid DLL destination path: {destinationRelPath}");
        }

        var relative = new PathString(normalized);
        if (relative.Parts.Any(static part => part == ".."))
        {
            throw new InvalidOperationException($"Invalid DLL destination path: {destinationRelPath}");
        }

        var fileName = Path.GetFileName(normalized);
        if (string.IsNullOrWhiteSpace(fileName) || !fileName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Invalid DLL destination path: {destinationRelPath}");
        }

        return normalized;
    }

    public static string CombineUnderTarget(string targetPath, string relativePath)
    {
        var target = EnsureTrailingSeparator(targetPath);
        var candidate = Path.GetFullPath(Path.Combine(target, relativePath));
        if (!candidate.StartsWith(target, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Path traversal detected.");
        }

        return candidate;
    }

    private static string EnsureTrailingSeparator(string path)
    {
        var fullPath = Path.GetFullPath(path);
        return Path.EndsInDirectorySeparator(fullPath)
            ? fullPath
            : fullPath + Path.DirectorySeparatorChar;
    }

    public static string NormalizeAlias(string value)
    {
        var normalized = (value ?? "").Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return "";
        }

        normalized = normalized
            .Replace('-', '_')
            .Replace(' ', '_');
        while (normalized.Contains("__", StringComparison.Ordinal))
        {
            normalized = normalized.Replace("__", "_", StringComparison.Ordinal);
        }

        return normalized.Trim('_');
    }

    public static string ResolveDownloadFileName(string url, string requestedFileName, string fallback)
    {
        var requested = Path.GetFileName((requestedFileName ?? "").Trim());
        if (!string.IsNullOrWhiteSpace(requested))
        {
            return requested;
        }

        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            var parsed = Path.GetFileName(Uri.UnescapeDataString(uri.AbsolutePath));
            if (!string.IsNullOrWhiteSpace(parsed))
            {
                return parsed;
            }
        }

        return fallback;
    }

    public static bool IsAllowedArchiveExtension(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return string.Equals(ext, ".zip", StringComparison.OrdinalIgnoreCase)
               || string.Equals(ext, ".7z", StringComparison.OrdinalIgnoreCase);
    }

    public static string NormalizeCacheEntryName(string input, string fallback)
    {
        var normalized = UnsafeCharsPattern.Replace((input ?? "").Trim(), "_").Trim('_');
        return string.IsNullOrWhiteSpace(normalized) ? fallback : normalized;
    }

    public static void EnsureWritableIfExists(IInstallFileSystem fileSystem, string path)
    {
        if (fileSystem.FileExists(path) && !fileSystem.IsWritable(path))
        {
            fileSystem.SetWritable(path);
        }
    }

    private readonly record struct PathString(string Raw)
    {
        public IEnumerable<string> Parts =>
            Raw.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}
