using System.IO;
using OptiClick.Core.Games;
using OptiClick.Core.Runtime;
using OptiClick.Core.Scan;
using OptiClick.Infrastructure.Scan;
using OptiClick.Wpf.Logging;
using OptiClick.Wpf.Shell.Games;
using OptiClick.Wpf.Shell.Games.Support;

namespace OptiClick.Wpf.Shell.Scan;

public sealed class ShellGameScanMatcher : IShellGameScanMatcher
    , IShellGameScanMatcherWithIndex
{
    private readonly IGameSupportPolicy _supportPolicy;
    private readonly IAppLogger _logger;
    private readonly IShellGameExeMatchIndexBuilder _matchIndexBuilder;
    private readonly IScanFileSystemProbe _fileSystemProbe;

    public ShellGameScanMatcher()
        : this(new GameSupportPolicy(), null)
    {
    }

    public ShellGameScanMatcher(IAppLogger? logger)
        : this(new GameSupportPolicy(), new ShellGameExeMatchIndexBuilder(), logger)
    {
    }

    public ShellGameScanMatcher(IGameSupportPolicy supportPolicy, IAppLogger? logger = null)
        : this(supportPolicy, new ShellGameExeMatchIndexBuilder(), logger)
    {
    }

    public ShellGameScanMatcher(
        IGameSupportPolicy supportPolicy,
        IShellGameExeMatchIndexBuilder matchIndexBuilder,
        IAppLogger? logger = null,
        IScanFileSystemProbe? fileSystemProbe = null)
    {
        _supportPolicy = supportPolicy;
        _matchIndexBuilder = matchIndexBuilder;
        _logger = logger ?? NullAppLogger.Instance;
        _fileSystemProbe = fileSystemProbe ?? new ScanFileSystemProbe();
    }

    public IReadOnlyList<ShellGameMatchResult> Match(
        ExecutableScanResult scanResult,
        ShellGameCatalog catalog,
        RuntimeContext? runtimeContext)
    {
        var matchIndex = _matchIndexBuilder.Build(catalog);
        return Match(scanResult, matchIndex, runtimeContext);
    }

    public IReadOnlyList<ShellGameMatchResult> Match(
        ExecutableScanResult scanResult,
        ShellGameExeMatchIndex matchIndex,
        RuntimeContext? runtimeContext)
    {
        if (scanResult is null || matchIndex is null || matchIndex.RulesByExecutableName.Count == 0)
        {
            return [];
        }

        var results = new List<ShellGameMatchResult>();
        if (scanResult.Executables.Count == 0)
        {
            return [];
        }

        var detectedFileNamesByDirectory = BuildDetectedFileNamesByDirectory(scanResult);
        var requiredFileCheckCache = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        foreach (var executable in scanResult.Executables)
        {
            var executableName = NormalizeExecutableName(ResolveExecutableName(executable));
            if (string.IsNullOrWhiteSpace(executableName))
            {
                continue;
            }

            var executableFolder = ResolveExecutableFolder(executable, scanResult.FolderPath);
            if (!matchIndex.RulesByExecutableName.TryGetValue(executableName, out var indexedRules))
            {
                continue;
            }

            var candidates = indexedRules
                .Where(rule => IsRequiredFilesSatisfied(
                    executableFolder,
                    rule.RequiredFiles,
                    detectedFileNamesByDirectory,
                    requiredFileCheckCache,
                    _fileSystemProbe))
                .Select(static rule => rule.Game)
                .GroupBy(static game => (game.GameId ?? "").Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(static group => group.First())
                .ToList();

            if (candidates.Count == 0)
            {
                continue;
            }

            if (candidates.Count > 1)
            {
                _logger.Warning("Scan", $"multiple_candidates exe={executableName} count={candidates.Count}");
                results.Add(new ShellGameMatchResult
                {
                    Status = ShellGameMatchStatus.MultipleCandidates,
                    MatchedExe = executableName,
                    FolderPath = executableFolder
                });
                continue;
            }

            var game = candidates[0];
            if (!game.Enabled)
            {
                _logger.Warning("Scan", $"match disabled game_id={game.GameId} exe={executableName}");
                results.Add(new ShellGameMatchResult
                {
                    Status = ShellGameMatchStatus.Disabled,
                    Game = game,
                    MatchedExe = executableName,
                    FolderPath = executableFolder,
                    ReasonCode = GameSupportReasonCodes.EnabledFalse
                });
                continue;
            }

            var supportDecision = _supportPolicy.Evaluate(game, runtimeContext);
            if (!supportDecision.IsSupported)
            {
                _logger.Warning("Scan", $"match unsupported game_id={game.GameId} reason={supportDecision.ReasonCode}");
                results.Add(new ShellGameMatchResult
                {
                    Status = ShellGameMatchStatus.UnsupportedGpu,
                    Game = game,
                    MatchedExe = executableName,
                    FolderPath = executableFolder,
                    ReasonCode = supportDecision.ReasonCode
                });
                continue;
            }

            results.Add(new ShellGameMatchResult
            {
                Status = ShellGameMatchStatus.Matched,
                Game = game,
                MatchedExe = executableName,
                FolderPath = executableFolder,
                ReasonCode = supportDecision.ReasonCode
            });
        }

        return results;
    }

    private static string ResolveExecutableName(DetectedExecutable executable)
    {
        var fullPath = (executable.FullPath ?? "").Trim();
        if (!string.IsNullOrWhiteSpace(fullPath))
        {
            var fileNameFromPath = Path.GetFileName(fullPath);
            if (!string.IsNullOrWhiteSpace(fileNameFromPath))
            {
                return fileNameFromPath;
            }
        }

        return (executable.FileName ?? "").Trim();
    }

    private static string NormalizeExecutableName(string value)
    {
        return MatchExePatternParser.NormalizeExecutableName(value);
    }

    private static string ResolveExecutableFolder(DetectedExecutable executable, string fallbackFolder)
    {
        var fullPath = (executable.FullPath ?? "").Trim();
        if (!string.IsNullOrWhiteSpace(fullPath))
        {
            var folder = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrWhiteSpace(folder))
            {
                return folder;
            }
        }

        return (fallbackFolder ?? "").Trim();
    }

    private static IReadOnlyDictionary<string, IReadOnlySet<string>> BuildDetectedFileNamesByDirectory(
        ExecutableScanResult scanResult)
    {
        var map = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        var fallbackFolder = NormalizeDirectoryPathForComparison(scanResult.FolderPath);
        foreach (var executable in scanResult.Executables)
        {
            var fileName = NormalizeExecutableName(ResolveExecutableName(executable));
            if (string.IsNullOrWhiteSpace(fileName))
            {
                continue;
            }

            var directory = ResolveExecutableFolder(executable, fallbackFolder);
            var normalizedDirectory = NormalizeDirectoryPathForComparison(directory);
            if (string.IsNullOrWhiteSpace(normalizedDirectory))
            {
                continue;
            }

            if (!map.TryGetValue(normalizedDirectory, out var files))
            {
                files = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                map[normalizedDirectory] = files;
            }

            files.Add(fileName);
        }

        return map.ToDictionary(
            static pair => pair.Key,
            static pair => (IReadOnlySet<string>)pair.Value,
            StringComparer.OrdinalIgnoreCase);
    }

    private static bool IsRequiredFilesSatisfied(
        string directoryPath,
        IReadOnlyList<string> requiredFiles,
        IReadOnlyDictionary<string, IReadOnlySet<string>> detectedFileNamesByDirectory,
        IDictionary<string, bool> requiredFileCheckCache,
        IScanFileSystemProbe fileSystemProbe)
    {
        if (requiredFiles is null || requiredFiles.Count == 0)
        {
            return false;
        }

        var normalizedDirectory = NormalizeDirectoryPathForComparison(directoryPath);
        if (string.IsNullOrWhiteSpace(normalizedDirectory))
        {
            return false;
        }

        detectedFileNamesByDirectory.TryGetValue(normalizedDirectory, out var detectedFileNames);
        foreach (var requiredFile in requiredFiles)
        {
            var normalizedRequiredFile = NormalizeExecutableName(requiredFile);
            if (string.IsNullOrWhiteSpace(normalizedRequiredFile))
            {
                return false;
            }

            if (detectedFileNames?.Contains(normalizedRequiredFile) == true)
            {
                continue;
            }

            var candidatePath = Path.Combine(normalizedDirectory, normalizedRequiredFile);
            if (!requiredFileCheckCache.TryGetValue(candidatePath, out var exists))
            {
                exists = fileSystemProbe.FileExists(candidatePath);
                requiredFileCheckCache[candidatePath] = exists;
            }

            if (!exists)
            {
                return false;
            }
        }

        return true;
    }

    private static string NormalizeDirectoryPathForComparison(string path)
    {
        var normalized = (path ?? "").Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return "";
        }

        try
        {
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
        catch
        {
            return normalized.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
    }
}
