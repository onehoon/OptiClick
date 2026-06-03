using System.IO;

namespace OptiClick.Infrastructure.Install.Uninstall;

internal static class OptiClickUninstallPathHelper
{
    public static string NormalizeTargetDirectory(string? candidatePath, IOptiClickUninstallFileSystem fileSystem)
    {
        ArgumentNullException.ThrowIfNull(fileSystem);

        var normalized = (candidatePath ?? "").Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return "";
        }

        if (fileSystem.DirectoryExists(normalized))
        {
            return NormalizeDirectoryPath(normalized);
        }

        if (fileSystem.FileExists(normalized))
        {
            return NormalizeDirectoryPath(Path.GetDirectoryName(normalized));
        }

        try
        {
            var fullPath = Path.GetFullPath(normalized);
            if (fileSystem.DirectoryExists(fullPath))
            {
                return NormalizeDirectoryPath(fullPath);
            }

            if (fileSystem.FileExists(fullPath))
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

    public static bool IsPathUnderRoot(string rootDirectoryPath, string candidatePath)
    {
        var root = EnsureTrailingSeparator(rootDirectoryPath);
        var fullCandidate = Path.GetFullPath(candidatePath);
        return fullCandidate.StartsWith(root, StringComparison.OrdinalIgnoreCase);
    }

    public static string EnsureTrailingSeparator(string path)
    {
        var fullPath = Path.GetFullPath(path);
        return Path.EndsInDirectorySeparator(fullPath)
            ? fullPath
            : fullPath + Path.DirectorySeparatorChar;
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
