using System.IO;

namespace OptiClick.Wpf.Shell.Scan;

public static class ScanFolderPathPolicy
{
    private static readonly string[] OneDriveEnvironmentVariableNames =
    [
        "OneDrive",
        "OneDriveConsumer",
        "OneDriveCommercial"
    ];

    public static bool IsAllowedScanFolderPath(string? path)
    {
        return !IsBlockedBroadPath(path);
    }

    public static bool IsBlockedBroadPath(string? path)
    {
        var normalized = NormalizePathForComparison(path);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return true;
        }

        if (IsDriveRootPath(normalized))
        {
            return true;
        }

        var blockedExactPaths = BuildBlockedExactPathSet();
        return blockedExactPaths.Contains(normalized);
    }

    private static HashSet<string> BuildBlockedExactPathSet()
    {
        var blocked = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        AddBlockedPath(blocked, Environment.GetFolderPath(Environment.SpecialFolder.Windows));
        AddBlockedPath(blocked, Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
        AddBlockedPath(blocked, Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles));
        AddBlockedPath(blocked, Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86));
        AddBlockedPath(blocked, Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData));
        AddBlockedPath(blocked, Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
        AddBlockedPath(blocked, Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));
        AddBlockedPath(blocked, Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory));
        AddBlockedPath(blocked, Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));

        var userProfile = NormalizePathForComparison(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
        if (!string.IsNullOrWhiteSpace(userProfile))
        {
            AddBlockedPath(blocked, Path.Combine(userProfile, "Downloads"));
        }

        foreach (var variableName in OneDriveEnvironmentVariableNames)
        {
            AddBlockedPath(blocked, Environment.GetEnvironmentVariable(variableName));
        }

        return blocked;
    }

    private static bool IsDriveRootPath(string normalizedPath)
    {
        if (IsDriveLetterRootToken(normalizedPath))
        {
            return true;
        }

        try
        {
            var fullPath = Path.GetFullPath(normalizedPath);
            var root = Path.GetPathRoot(fullPath);
            if (string.IsNullOrWhiteSpace(root))
            {
                return false;
            }

            return string.Equals(
                TrimPathSeparators(fullPath),
                TrimPathSeparators(root),
                StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private static bool IsDriveLetterRootToken(string normalizedPath)
    {
        if (string.IsNullOrWhiteSpace(normalizedPath) || normalizedPath.Length != 2)
        {
            return false;
        }

        return char.IsLetter(normalizedPath[0]) && normalizedPath[1] == ':';
    }

    private static void AddBlockedPath(ISet<string> blocked, string? path)
    {
        var normalized = NormalizePathForComparison(path);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return;
        }

        blocked.Add(normalized);
    }

    private static string NormalizePathForComparison(string? path)
    {
        var trimmed = (path ?? "").Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return "";
        }

        var normalizedToken = TrimPathSeparators(trimmed);
        if (IsDriveLetterRootToken(normalizedToken))
        {
            return normalizedToken;
        }

        try
        {
            var fullPath = Path.GetFullPath(trimmed);
            return TrimPathSeparators(fullPath);
        }
        catch
        {
            return TrimPathSeparators(trimmed);
        }
    }

    private static string TrimPathSeparators(string path)
    {
        return (path ?? "").Trim().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }
}
