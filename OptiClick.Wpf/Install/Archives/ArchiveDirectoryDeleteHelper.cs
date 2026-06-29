using System.IO;
using System.Threading;

namespace OptiClick.Wpf.Install.Archives;

internal static class ArchiveDirectoryDeleteHelper
{
    private const int RetryDelayMilliseconds = 200;

    public static void DeleteRecursive(string path)
    {
        if (!Directory.Exists(path))
        {
            return;
        }

        try
        {
            ClearReadOnlyAttributes(path);
            Directory.Delete(path, recursive: true);
        }
        catch when (Directory.Exists(path))
        {
            Thread.Sleep(RetryDelayMilliseconds);
            ClearReadOnlyAttributes(path);
            Directory.Delete(path, recursive: true);
        }
    }

    public static void TryDeleteRecursive(string path)
    {
        try
        {
            DeleteRecursive(path);
        }
        catch
        {
            // Best-effort cleanup.
        }
    }

    private static void ClearReadOnlyAttributes(string root)
    {
        if (!Directory.Exists(root))
        {
            return;
        }

        foreach (var file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
        {
            ClearReadOnlyAttribute(file);
        }

        foreach (var directory in Directory.EnumerateDirectories(root, "*", SearchOption.AllDirectories))
        {
            ClearReadOnlyAttribute(directory);
        }

        ClearReadOnlyAttribute(root);
    }

    private static void ClearReadOnlyAttribute(string path)
    {
        try
        {
            var attributes = File.GetAttributes(path);
            if ((attributes & FileAttributes.ReadOnly) != 0)
            {
                File.SetAttributes(path, attributes & ~FileAttributes.ReadOnly);
            }
        }
        catch
        {
            // Leave deletion to surface the real failure.
        }
    }
}
