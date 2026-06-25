using System.IO;
using OptiClick.Wpf.Install.FileSystem;

namespace OptiClick.Wpf.Install.Precheck;

public interface IModPrecheckScanner
{
    ModPrecheckState Scan(string targetPath);
}

public sealed class ModPrecheckScanner : IModPrecheckScanner
{
    private static readonly string[] MonitoredDllNames =
    [
        "dxgi.dll",
        "d3d12.dll",
        "d3d11.dll",
        "d3d10.dll",
        "d3d9.dll",
        "dinput8.dll",
        "opengl32.dll",
        "reshade64.dll",
        "specialk64.dll",
        "specialk32.dll",
        "version.dll",
        "winmm.dll"
    ];

    private const string RenoDxPattern = "renodx*.addon";
    private const string LennyModLoaderFileName = "lml.ini";
    private const string ScriptHookRdr2FileName = "ScriptHookRDR2.dll";

    private readonly IInstallFileSystem _fileSystem;
    private readonly IBinaryOwnerDetector _ownerDetector;

    public ModPrecheckScanner(IInstallFileSystem fileSystem, IBinaryOwnerDetector ownerDetector)
    {
        _fileSystem = fileSystem;
        _ownerDetector = ownerDetector;
    }

    public ModPrecheckState Scan(string targetPath)
    {
        var normalizedTarget = (targetPath ?? "").Trim();
        if (!_fileSystem.DirectoryExists(normalizedTarget))
        {
            return ModPrecheckState.Empty;
        }

        var dllCandidates = ScanCandidateDlls(normalizedTarget);
        var detectedPaths = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
        {
            [ModConflictKinds.ReShade] = [],
            [ModConflictKinds.SpecialK] = []
        };

        foreach (var filePath in dllCandidates.Values)
        {
            var owner = _ownerDetector.DetectOwner(filePath);
            if (detectedPaths.TryGetValue(owner, out var paths))
            {
                paths.Add(filePath);
            }
        }

        var renoDxPaths = ScanRenoDxAddons(normalizedTarget);
        var lennyModLoaderPaths = ScanRootFiles(normalizedTarget, LennyModLoaderFileName);
        var scriptHookRdr2Paths = ScanRootFiles(normalizedTarget, ScriptHookRdr2FileName);
        return new ModPrecheckState
        {
            ReShade = BuildModBinaryState(detectedPaths[ModConflictKinds.ReShade]),
            SpecialK = BuildModBinaryState(detectedPaths[ModConflictKinds.SpecialK]),
            RenoDx = BuildModFilePresenceState(renoDxPaths),
            LennyModLoader = BuildModFilePresenceState(lennyModLoaderPaths),
            ScriptHookRdr2 = BuildModFilePresenceState(scriptHookRdr2Paths)
        };
    }

    private Dictionary<string, string> ScanCandidateDlls(string targetPath)
    {
        var monitored = new HashSet<string>(MonitoredDllNames, StringComparer.OrdinalIgnoreCase);
        var dllFiles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        IEnumerable<string> entries;
        try
        {
            entries = _fileSystem.EnumerateFileSystemEntries(targetPath);
        }
        catch
        {
            return dllFiles;
        }

        foreach (var entry in entries)
        {
            var fileName = Path.GetFileName(entry);
            if (!monitored.Contains(fileName))
            {
                continue;
            }

            if (!_fileSystem.FileExists(entry))
            {
                continue;
            }

            dllFiles.TryAdd(fileName.ToLowerInvariant(), entry);
        }

        return dllFiles;
    }

    private IReadOnlyList<string> ScanRenoDxAddons(string targetPath)
    {
        var targetDir = targetPath;
        var renoDxDir = Path.Combine(targetDir, "reshade-shaders", "Addons");
        var hits = new List<string>();
        foreach (var searchDir in new[] { targetDir, renoDxDir })
        {
            if (!_fileSystem.DirectoryExists(searchDir))
            {
                continue;
            }

            IEnumerable<string> entries;
            try
            {
                entries = _fileSystem.EnumerateFileSystemEntries(searchDir);
            }
            catch
            {
                continue;
            }

            foreach (var entry in entries)
            {
                if (!_fileSystem.FileExists(entry))
                {
                    continue;
                }

                var name = Path.GetFileName(entry);
                if (!System.IO.Enumeration.FileSystemName.MatchesSimpleExpression(RenoDxPattern, name, ignoreCase: true))
                {
                    continue;
                }

                var relative = Path.GetRelativePath(targetDir, entry).Replace('\\', '/');
                hits.Add(relative);
            }
        }

        return NormalizeUniqueStrings(hits);
    }

    private IReadOnlyList<string> ScanRootFiles(string targetPath, params string[] fileNames)
    {
        var hits = new List<string>();
        foreach (var fileName in fileNames)
        {
            var normalizedName = (fileName ?? "").Trim();
            if (string.IsNullOrWhiteSpace(normalizedName))
            {
                continue;
            }

            var filePath = Path.Combine(targetPath, normalizedName);
            if (_fileSystem.FileExists(filePath))
            {
                hits.Add(normalizedName);
            }
        }

        return NormalizeUniqueStrings(hits);
    }

    private static ModBinaryState BuildModBinaryState(IEnumerable<string> paths)
    {
        var names = NormalizeUniqueStrings(paths.Select(path => Path.GetFileName(path) ?? ""));
        return new ModBinaryState
        {
            Detected = names.Count > 0,
            DllNames = names
        };
    }

    private static ModFilePresenceState BuildModFilePresenceState(IEnumerable<string> relativePaths)
    {
        var normalized = NormalizeUniqueStrings(relativePaths);
        return new ModFilePresenceState
        {
            Detected = normalized.Count > 0,
            RelativePaths = normalized
        };
    }

    public static IReadOnlyList<string> NormalizeUniqueStrings(IEnumerable<string> values)
    {
        return values
            .Select(value => (value ?? "").Trim())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
