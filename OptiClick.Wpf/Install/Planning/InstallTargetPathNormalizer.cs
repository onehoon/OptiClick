using System.IO;

namespace OptiClick.Wpf.Install.Planning;

public static class InstallTargetPathNormalizer
{
    public static string NormalizeTargetDirectory(string? candidatePath)
    {
        var normalized = (candidatePath ?? "").Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return "";
        }

        if (Directory.Exists(normalized))
        {
            return NormalizeDirectoryPath(normalized);
        }

        if (File.Exists(normalized))
        {
            return NormalizeDirectoryPath(Path.GetDirectoryName(normalized));
        }

        try
        {
            var fullPath = Path.GetFullPath(normalized);
            if (Directory.Exists(fullPath))
            {
                return NormalizeDirectoryPath(fullPath);
            }

            if (File.Exists(fullPath))
            {
                return NormalizeDirectoryPath(Path.GetDirectoryName(fullPath));
            }

            if (LooksLikeExecutablePath(fullPath))
            {
                return NormalizeDirectoryPath(Path.GetDirectoryName(fullPath));
            }

            return NormalizeDirectoryPath(fullPath);
        }
        catch
        {
            return NormalizeDirectoryPath(normalized);
        }
    }

    private static bool LooksLikeExecutablePath(string path)
    {
        var extension = Path.GetExtension(path);
        return string.Equals(extension, ".exe", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeDirectoryPath(string? directoryPath)
    {
        var normalized = (directoryPath ?? "").Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return "";
        }

        var fullPath = Path.GetFullPath(normalized);
        var root = Path.GetPathRoot(fullPath);
        if (!string.IsNullOrWhiteSpace(root)
            && string.Equals(
                fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                StringComparison.OrdinalIgnoreCase))
        {
            return fullPath;
        }

        return fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }
}
