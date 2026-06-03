using System.IO;
using OptiClick.Wpf.Shell.Games;

namespace OptiClick.Wpf.Install.Precheck;

public sealed class ReFrameworkLegacyFindingService
{
    public IReadOnlyList<ModConflictFinding> AppendLegacyFinding(
        IEnumerable<ModConflictFinding> findings,
        string targetPath,
        ShellGameCardModel? game)
    {
        var normalized = findings?.ToList() ?? new List<ModConflictFinding>();
        if (normalized.Any(finding => string.Equals((finding.Kind ?? "").Trim(), ModConflictKinds.ReFrameworkLegacy, StringComparison.OrdinalIgnoreCase)))
        {
            return normalized;
        }

        var legacy = DetectLegacyFinding(targetPath, game);
        if (legacy is null)
        {
            return normalized;
        }

        normalized.Add(legacy);
        return normalized;
    }

    public ModConflictFinding? DetectLegacyFinding(string targetPath, ShellGameCardModel? game)
    {
        var destinationRelPath = ShellGameInstallMetadataResolver.GetReFrameworkUrl(game);
        if (string.IsNullOrWhiteSpace(destinationRelPath))
        {
            return null;
        }

        var destinationName = Path.GetFileName(destinationRelPath).ToLowerInvariant();
        if (destinationName == "dinput8.dll")
        {
            return null;
        }

        var targetDir = (targetPath ?? "").Trim();
        var legacyDllPath = Path.Combine(targetDir, "dinput8.dll");
        if (!File.Exists(legacyDllPath))
        {
            return null;
        }

        var destinationDisplay = DisplayDestination(destinationRelPath);
        return new ModConflictFinding
        {
            Kind = ModConflictKinds.ReFrameworkLegacy,
            Evidence = new[] { "dinput8.dll" },
            Context = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["destination"] = destinationDisplay
            }
        };
    }

    private static string DisplayDestination(string destination)
    {
        var normalized = (destination ?? "").Trim().Replace('\\', '/');
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return "";
        }

        if (Path.IsPathRooted(normalized))
        {
            return Path.GetFileName(normalized);
        }

        return normalized;
    }
}
