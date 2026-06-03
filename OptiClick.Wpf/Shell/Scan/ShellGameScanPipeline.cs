using OptiClick.Core.Runtime;
using OptiClick.Wpf.Logging;
using OptiClick.Wpf.Shell.Games;

namespace OptiClick.Wpf.Shell.Scan;

public sealed class ShellGameScanPipeline : IShellGameScanPipeline
{
    private const int MaxScanFolderParallelism = 2;

    private readonly IExecutableScanService _executableScanService;
    private readonly IShellGameScanMatcher _scanMatcher;
    private readonly IShellGameExeMatchIndexBuilder _matchIndexBuilder;
    private readonly IAppLogger _logger;

    public ShellGameScanPipeline(
        IExecutableScanService executableScanService,
        IShellGameScanMatcher scanMatcher,
        IShellGameExeMatchIndexBuilder matchIndexBuilder,
        IAppLogger? logger = null)
    {
        _executableScanService = executableScanService;
        _scanMatcher = scanMatcher;
        _matchIndexBuilder = matchIndexBuilder;
        _logger = logger ?? NullAppLogger.Instance;
    }

    public Task<ShellGameScanPipelineResult> ScanAsync(
        ShellGameScanRequest request,
        CancellationToken cancellationToken = default)
    {
        var safeRequest = request ?? new ShellGameScanRequest();
        return Task.Run(
            () => ScanCore(safeRequest, cancellationToken),
            cancellationToken);
    }

    private ShellGameScanPipelineResult ScanCore(
        ShellGameScanRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var scanFolders = (request.ScanFolders ?? [])
            .Select(static path => (path ?? "").Trim())
            .Where(static path => !string.IsNullOrWhiteSpace(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var catalog = request.Catalog ?? ShellGameCatalog.Empty;
        var runtimeContext = request.RuntimeContext;
        _logger.Info("scan", $"scan started folder_count={scanFolders.Length} catalog_games={catalog.Games.Count}");

        var matchIndex = _matchIndexBuilder.Build(catalog);
        var indexedGameCount = matchIndex.GamesByExeName
            .SelectMany(static pair => pair.Value)
            .Select(static game => (game.GameId ?? "").Trim())
            .Where(static gameId => !string.IsNullOrWhiteSpace(gameId))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();
        _logger.Info("scan", $"scan index built exe_patterns={matchIndex.AllowedExeNames.Count} indexed_games={indexedGameCount}");

        var matchByGameId = new Dictionary<string, ShellGameMatchResult>(StringComparer.OrdinalIgnoreCase);
        var targetPathByGameId = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var rawMatches = new List<ShellGameMatchResult>();
        var scannedExecutableCount = 0;
        var candidateExecutableCount = 0;
        var matchedCount = 0;
        var multipleCandidateCount = 0;
        var disabledCount = 0;
        var unsupportedCount = 0;
        var duplicateCount = 0;
        var skippedDirectoryCount = 0;

        var scanResults = ScanFolders(scanFolders, matchIndex.AllowedExeNames, cancellationToken);
        foreach (var folderResult in scanResults)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var scanResult = folderResult.ScanResult;
            scannedExecutableCount += scanResult.ScannedExecutableCount;
            candidateExecutableCount += scanResult.Executables.Count;
            skippedDirectoryCount += scanResult.SkippedDirectoryCount;

            var matches = Match(scanResult, matchIndex, catalog, runtimeContext);
            foreach (var match in matches)
            {
                cancellationToken.ThrowIfCancellationRequested();
                rawMatches.Add(match);
                switch (match.Status)
                {
                    case ShellGameMatchStatus.MultipleCandidates:
                        multipleCandidateCount++;
                        break;
                    case ShellGameMatchStatus.Disabled:
                        disabledCount++;
                        break;
                    case ShellGameMatchStatus.UnsupportedGpu:
                        unsupportedCount++;
                        break;
                }

                var gameId = (match.Game?.GameId ?? "").Trim();
                if (match.Status != ShellGameMatchStatus.Matched || string.IsNullOrWhiteSpace(gameId))
                {
                    continue;
                }

                var targetPath = (match.FolderPath ?? "").Trim();
                if (matchByGameId.ContainsKey(gameId))
                {
                    duplicateCount++;
                    var existingTarget = targetPathByGameId.TryGetValue(gameId, out var path) ? path : "";
                    _logger.Warning("scan", $"duplicate match skipped game_id={gameId} existing_target={existingTarget} new_target={targetPath}");
                    continue;
                }

                matchByGameId[gameId] = match;
                targetPathByGameId[gameId] = targetPath;
                matchedCount++;
                _logger.Info("scan", $"match game_id={gameId} exe={match.MatchedExe} target={targetPath}");
            }
        }

        var unmatchedCount = Math.Max(
            0,
            candidateExecutableCount - matchedCount - duplicateCount - multipleCandidateCount - disabledCount - unsupportedCount);
        var summary = new ScanExecutionSummary
        {
            ExecutableCount = scannedExecutableCount,
            CandidateExecutableCount = candidateExecutableCount,
            MatchedCount = matchedCount,
            MultipleCandidateCount = multipleCandidateCount,
            DisabledCount = disabledCount,
            UnsupportedCount = unsupportedCount,
            UnmatchedCount = unmatchedCount,
            DuplicateMatchCount = duplicateCount,
            VisibleGameCount = matchByGameId.Count,
            SkippedDirectoryCount = skippedDirectoryCount
        };

        _logger.Info(
            "scan",
            $"scan completed executable_count={summary.ExecutableCount} candidate_exe_count={summary.CandidateExecutableCount} matched={summary.MatchedCount} unmatched={summary.UnmatchedCount} multiple={summary.MultipleCandidateCount} disabled={summary.DisabledCount} unsupported={summary.UnsupportedCount} duplicate={summary.DuplicateMatchCount} visible_games={summary.VisibleGameCount}");

        return new ShellGameScanPipelineResult
        {
            MatchByGameId = matchByGameId,
            TargetPathByGameId = targetPathByGameId,
            Summary = summary,
            RawMatches = rawMatches
        };
    }

    private IReadOnlyList<ScanFolderPipelineResult> ScanFolders(
        IReadOnlyList<string> scanFolders,
        IReadOnlySet<string> allowedExeNames,
        CancellationToken cancellationToken)
    {
        var results = new ScanFolderPipelineResult?[scanFolders.Count];
        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = MaxScanFolderParallelism,
            CancellationToken = cancellationToken
        };

        Parallel.For(
            0,
            scanFolders.Count,
            options,
            index =>
            {
                options.CancellationToken.ThrowIfCancellationRequested();
                var folderPath = scanFolders[index];

                var scanResult = ScanFolder(folderPath, allowedExeNames, options.CancellationToken);
                results[index] = new ScanFolderPipelineResult(folderPath, scanResult);
            });

        return results
            .Where(static result => result is not null)
            .Select(static result => result!)
            .ToArray();
    }

    private ShellScanResult ScanFolder(
        string folderPath,
        IReadOnlySet<string> allowedExeNames,
        CancellationToken cancellationToken)
    {
        if (allowedExeNames.Count == 0)
        {
            return new ShellScanResult
            {
                FolderPath = folderPath,
                Executables = [],
                ScannedExecutableCount = 0,
                SkippedDirectoryCount = 0
            };
        }

        return _executableScanService
            .ScanAsync(folderPath, allowedExeNames, cancellationToken)
            .GetAwaiter()
            .GetResult();
    }

    private IReadOnlyList<ShellGameMatchResult> Match(
        ShellScanResult scanResult,
        ShellGameExeMatchIndex matchIndex,
        ShellGameCatalog catalog,
        RuntimeContext runtimeContext)
    {
        if (_scanMatcher is IShellGameScanMatcherWithIndex indexedMatcher)
        {
            return indexedMatcher.Match(scanResult, matchIndex, runtimeContext);
        }

        return _scanMatcher.Match(scanResult, catalog, runtimeContext);
    }

    private sealed record ScanFolderPipelineResult(string FolderPath, ShellScanResult ScanResult);
}
