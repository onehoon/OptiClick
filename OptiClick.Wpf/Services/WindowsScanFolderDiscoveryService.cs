using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Media;
using OptiClick.Wpf.ViewModels;

namespace OptiClick.Wpf.Services;

public sealed partial class WindowsScanFolderDiscoveryService : IScanFolderDiscoveryService
{
    private static readonly Brush AutoDetectedBrush = new SolidColorBrush(Color.FromRgb(179, 227, 186));
    private readonly IScanFolderDiscoveryFileSystem _fileSystem;

    public WindowsScanFolderDiscoveryService(IScanFolderDiscoveryFileSystem? fileSystem = null)
    {
        _fileSystem = fileSystem ?? new WindowsScanFolderDiscoveryFileSystem();
    }

    public IReadOnlyList<ScanFolderRowViewModel> DiscoverDefaultFolders()
    {
        var rows = new List<ScanFolderRowViewModel>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var steamRoots = DiscoverSteamRoots();

        foreach (var steamRoot in steamRoots)
        {
            AddRow(rows, seen, "Steam Library", Path.Combine(steamRoot, "steamapps", "common"));
        }

        foreach (var steamLibraryPath in DiscoverSteamLibraryPaths(steamRoots))
        {
            var commonPath = Path.Combine(steamLibraryPath, "steamapps", "common");
            AddRow(rows, seen, "Steam Library", commonPath);
        }

        foreach (var driveRoot in _fileSystem.GetFixedReadyDriveRoots())
        {
            AddRow(rows, seen, "Xbox Games", Path.Combine(driveRoot, "XboxGames"));

            AddRow(rows, seen, "Epic Games", Path.Combine(driveRoot, "Program Files", "Epic Games"));
            AddRow(rows, seen, "Epic Games", Path.Combine(driveRoot, "Epic Games"));
            AddRow(rows, seen, "Epic Games", Path.Combine(driveRoot, "Games", "Epic Games"));
        }

        return rows;
    }

    private void AddRow(ICollection<ScanFolderRowViewModel> rows, ISet<string> seen, string name, string path)
    {
        var normalizedPath = NormalizePath(path);
        if (string.IsNullOrWhiteSpace(normalizedPath) || seen.Contains(normalizedPath))
        {
            return;
        }

        if (!_fileSystem.DirectoryExists(normalizedPath))
        {
            return;
        }

        seen.Add(normalizedPath);
        rows.Add(new ScanFolderRowViewModel(
            name,
            normalizedPath,
            "",
            true,
            true,
            false,
            AutoDetectedBrush));
    }

    private IReadOnlyList<string> DiscoverSteamLibraryPaths(IReadOnlyList<string> steamRoots)
    {
        var paths = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var steamRoot in steamRoots)
        {
            var steamConfigPath = Path.Combine(steamRoot, "config", "libraryfolders.vdf");
            if (!_fileSystem.FileExists(steamConfigPath))
            {
                continue;
            }

            try
            {
                var lines = _fileSystem.ReadAllLines(steamConfigPath);
                foreach (var line in lines)
                {
                    var match = SteamPathRegex().Match(line);
                    if (!match.Success)
                    {
                        continue;
                    }

                    var rawPath = match.Groups["path"].Value;
                    if (string.IsNullOrWhiteSpace(rawPath))
                    {
                        continue;
                    }

                    var unescaped = rawPath.Replace(@"\\", @"\").Trim();
                    var normalized = NormalizePath(unescaped);
                    if (string.IsNullOrWhiteSpace(normalized) || seen.Contains(normalized))
                    {
                        continue;
                    }

                    seen.Add(normalized);
                    paths.Add(normalized);
                }
            }
            catch
            {
                // Skip unreadable config file and continue discovery.
            }
        }

        return paths;
    }

    private IReadOnlyList<string> DiscoverSteamRoots()
    {
        var roots = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var driveRoot in _fileSystem.GetFixedReadyDriveRoots())
        {
            AddSteamRootCandidate(roots, seen, Path.Combine(driveRoot, "Program Files (x86)", "Steam"));
            AddSteamRootCandidate(roots, seen, Path.Combine(driveRoot, "Program Files", "Steam"));
            AddSteamRootCandidate(roots, seen, Path.Combine(driveRoot, "Steam"));
            AddSteamRootCandidate(roots, seen, Path.Combine(driveRoot, "Games", "Steam"));
        }

        return roots;
    }

    private void AddSteamRootCandidate(ICollection<string> roots, ISet<string> seen, string candidatePath)
    {
        var normalized = NormalizePath(candidatePath);
        if (string.IsNullOrWhiteSpace(normalized) || seen.Contains(normalized))
        {
            return;
        }

        if (!_fileSystem.DirectoryExists(normalized))
        {
            return;
        }

        seen.Add(normalized);
        roots.Add(normalized);
    }

    private static string NormalizePath(string path)
    {
        var trimmed = (path ?? "").Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return "";
        }

        try
        {
            return Path.GetFullPath(trimmed).TrimEnd(Path.DirectorySeparatorChar);
        }
        catch
        {
            return trimmed;
        }
    }

    [GeneratedRegex("\\\"path\\\"\\s+\\\"(?<path>[^\\\"]+)\\\"", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex SteamPathRegex();
}
