using System.IO;
using System.IO.Compression;

namespace OptiClick.Wpf.Install.Archives;

internal static class ArchivePreparationHelpers
{
    private static readonly HashSet<string> ArchiveSuffixes = new(StringComparer.OrdinalIgnoreCase)
    {
        ".7z",
        ".zip",
        ".rar",
        ".tar",
        ".gz",
        ".xz",
        ".bz2",
        ".asi"
    };

    public static bool IsValidZipFile(string path)
    {
        try
        {
            using var archive = ZipFile.OpenRead(path);
            _ = archive.Entries.Count;
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static void CleanupStaleArchives(string cacheDirectory, string keepFileName)
    {
        if (!Directory.Exists(cacheDirectory))
        {
            return;
        }

        var keep = Path.GetFileName(keepFileName).Trim();
        foreach (var candidate in Directory.EnumerateFiles(cacheDirectory))
        {
            var name = Path.GetFileName(candidate);
            if (!string.IsNullOrWhiteSpace(keep) && string.Equals(name, keep, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var extension = Path.GetExtension(candidate);
            if (!ArchiveSuffixes.Contains(extension))
            {
                continue;
            }

            TryDeleteFile(candidate);
        }
    }

    public static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.SetAttributes(path, FileAttributes.Normal);
                File.Delete(path);
            }
        }
        catch
        {
            // Ignore cleanup failure.
        }
    }

    public static string ResolvePayloadSourceDirectory(string extractRoot)
    {
        if (!Directory.Exists(extractRoot))
        {
            return extractRoot;
        }

        var children = Directory.GetFileSystemEntries(extractRoot);
        if (children.Length == 1 && Directory.Exists(children[0]))
        {
            return children[0];
        }

        return extractRoot;
    }
}
