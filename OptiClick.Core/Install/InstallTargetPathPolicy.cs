namespace OptiClick.Core.Install;

public static class InstallTargetPathPolicy
{
    public static string NormalizeTargetDirectory(string? candidatePath)
    {
        var normalized = (candidatePath ?? "").Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return "";
        }

        try
        {
            var fullPath = Path.GetFullPath(normalized);
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
