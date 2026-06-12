using System.IO;
using System.Text.RegularExpressions;
using OptiClick.Core.Scan;

namespace OptiClick.Infrastructure.Scan;

public sealed partial class WindowsScanFolderDiscoveryService : IScanFolderDiscoveryService
{
    private readonly IScanFolderDiscoveryFileSystem _fileSystem;

    public WindowsScanFolderDiscoveryService(IScanFolderDiscoveryFileSystem? fileSystem = null)
    {
        _fileSystem = fileSystem ?? new WindowsScanFolderDiscoveryFileSystem();
    }

    public IReadOnlyList<ScanFolderDiscoveryEntry> DiscoverDefaultFolders()
    {
        var entries = new List<ScanFolderDiscoveryEntry>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var steamRoots = DiscoverSteamRoots();

        foreach (var steamRoot in steamRoots)
        {
            AddEntry(entries, seen, "Steam Library", Path.Combine(steamRoot, "steamapps", "common"));
        }

        foreach (var steamLibraryPath in DiscoverSteamLibraryPaths(steamRoots))
        {
            var commonPath = Path.Combine(steamLibraryPath, "steamapps", "common");
            AddEntry(entries, seen, "Steam Library", commonPath);
        }

        foreach (var driveRoot in _fileSystem.GetFixedReadyDriveRoots())
        {
            AddEntry(entries, seen, "Xbox Games", Path.Combine(driveRoot, "XboxGames"));

            AddEntry(entries, seen, "Epic Games", Path.Combine(driveRoot, "Program Files", "Epic Games"));
            AddEntry(entries, seen, "Epic Games", Path.Combine(driveRoot, "Epic Games"));
            AddEntry(entries, seen, "Epic Games", Path.Combine(driveRoot, "Games", "Epic Games"));
        }

        return entries;
    }

    private void AddEntry(ICollection<ScanFolderDiscoveryEntry> entries, ISet<string> seen, string name, string path)
    {
        var normalizedPath = ScanFolderPathPolicy.NormalizePathOrEmpty(path);
        if (string.IsNullOrWhiteSpace(normalizedPath) || seen.Contains(normalizedPath))
        {
            return;
        }

        if (!_fileSystem.DirectoryExists(normalizedPath))
        {
            return;
        }

        seen.Add(normalizedPath);
        entries.Add(new ScanFolderDiscoveryEntry
        {
            Name = name,
            Path = normalizedPath,
            IsChecked = true
        });
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
                    var normalized = ScanFolderPathPolicy.NormalizePathOrEmpty(unescaped);
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
        var normalized = ScanFolderPathPolicy.NormalizePathOrEmpty(candidatePath);
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

    [GeneratedRegex("\\\"path\\\"\\s+\\\"(?<path>[^\\\"]+)\\\"", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex SteamPathRegex();
}
