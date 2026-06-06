using System.Globalization;
using OptiClick.Core.Runtime;
using OptiClick.Wpf.Models;
using OptiClick.Wpf.Shell.Games;
using OptiClick.Wpf.ViewModels;

namespace OptiClick.Wpf.Shell.Scan;

public sealed class ScanFlowController
{
    private readonly IShellGameScanPipeline? _scanPipeline;
    private readonly IShellGameCardViewModelFactory? _shellGameCardViewModelFactory;
    private readonly ScanVisibleGameResolver _visibleGameResolver = new();

    public ScanFlowController(
        IShellGameScanPipeline? scanPipeline,
        IShellGameCardViewModelFactory? shellGameCardViewModelFactory)
    {
        _scanPipeline = scanPipeline;
        _shellGameCardViewModelFactory = shellGameCardViewModelFactory;
    }

    public Task<ScanFlowResult> RunManualScanAsync(
        ScanFlowRequest request,
        CancellationToken cancellationToken = default)
    {
        return RunCoreAsync(request, isStartupScan: false, cancellationToken);
    }

    public Task<ScanFlowResult> RunStartupAutoScanAsync(
        ScanFlowRequest request,
        CancellationToken cancellationToken = default)
    {
        return RunCoreAsync(request, isStartupScan: true, cancellationToken);
    }

    private async Task<ScanFlowResult> RunCoreAsync(
        ScanFlowRequest request,
        bool isStartupScan,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Strings);

        var logs = new List<ScanFlowLogEntry>();
        var strings = request.Strings;
        var safeCatalog = request.LatestRemoteCatalog ?? ShellGameCatalog.Empty;
        var safeRuntimeContext = request.LatestRuntimeContext ?? new RuntimeContext();
        var scanFolders = (request.ScanFolders ?? [])
            .Select(static path => (path ?? "").Trim())
            .Where(static path => !string.IsNullOrWhiteSpace(path))
            .ToArray();
        var currentMatchByGameId = CloneMatchByGameId(request.CurrentMatchByGameId);
        var currentTargetPathByGameId = CloneTargetPathByGameId(request.CurrentTargetPathByGameId);
        var currentModuleDownloadLinks = request.ModuleDownloadLinks
            ?? new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        if (_scanPipeline is null)
        {
            if (isStartupScan)
            {
                logs.Add(Warning("scan", "startup auto scan skipped reason=scan_service_missing"));
                return CreateSkippedResult(currentMatchByGameId, currentTargetPathByGameId, logs);
            }

            logs.Add(Error("scan", "scan requested but services are missing"));
            return CreateSkippedResult(
                currentMatchByGameId,
                currentTargetPathByGameId,
                logs,
                statusText: strings.ScanServiceMissing,
                dialogRequest: BuildScanWarningDialog(strings.NavScan, strings.ScanServiceMissing));
        }

        if (isStartupScan && safeCatalog.Games.Count == 0)
        {
            logs.Add(Warning("scan", "startup auto scan skipped reason=remote_catalog_not_ready"));
            return CreateSkippedResult(
                currentMatchByGameId,
                currentTargetPathByGameId,
                logs,
                statusText: strings.ScanStartupSkippedCatalogNotReady);
        }

        if (scanFolders.Length == 0)
        {
            if (isStartupScan)
            {
                logs.Add(Warning("scan", "startup auto scan skipped reason=no_scan_folders"));
                return CreateSkippedResult(
                    currentMatchByGameId,
                    currentTargetPathByGameId,
                    logs,
                    statusText: strings.ScanStartupNoFolders);
            }

            logs.Add(Warning("scan", "scan skipped no folder selected"));
            var clearedMatchByGameId = new Dictionary<string, ShellGameMatchResult>(StringComparer.OrdinalIgnoreCase);
            var clearedTargetPathByGameId = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            return CreateSkippedResult(
                clearedMatchByGameId,
                clearedTargetPathByGameId,
                logs,
                statusText: strings.ScanNoFolderSelected,
                shouldRecomputeSelection: true,
                visibleGames: [],
                dialogRequest: BuildScanWarningDialog(strings.NavScan, strings.ScanNoFolderSelected));
        }

        if (!isStartupScan && safeCatalog.Games.Count == 0)
        {
            var reason = NormalizeStatusCode(request.LatestRemoteCatalogErrorCode, "runtime_data_or_gpu_bundle_failed");
            logs.Add(Warning("scan", $"scan skipped remote catalog not ready code={reason}"));
            var visibleGames = CreateVisibleGames(
                safeCatalog,
                safeRuntimeContext,
                currentMatchByGameId,
                currentTargetPathByGameId,
                currentModuleDownloadLinks,
                logs);
            return CreateSkippedResult(
                currentMatchByGameId,
                currentTargetPathByGameId,
                logs,
                statusText: Format(strings.RuntimeCatalogNotReadyForScan, reason),
                shouldRecomputeSelection: true,
                visibleGames: visibleGames,
                dialogRequest: BuildScanWarningDialog(
                    strings.RuntimeCatalogPipelineMissingTitle,
                    strings.RuntimeCatalogNotReadyScanBlocked,
                    $"Error code: {reason}"));
        }

        try
        {
            if (isStartupScan)
            {
                logs.Add(Info("scan", $"startup auto scan started folder_count={scanFolders.Length}"));
            }

            var pipelineResult = await _scanPipeline.ScanAsync(new ShellGameScanRequest
            {
                ScanFolders = scanFolders,
                Catalog = safeCatalog,
                RuntimeContext = safeRuntimeContext
            }, cancellationToken);
            var matchByGameId = CloneMatchByGameId(pipelineResult.MatchByGameId);
            var targetPathByGameId = CloneTargetPathByGameId(pipelineResult.TargetPathByGameId);
            var visibleGames = CreateVisibleGames(
                safeCatalog,
                safeRuntimeContext,
                matchByGameId,
                targetPathByGameId,
                currentModuleDownloadLinks,
                logs);
            var summary = NormalizeSummary(pipelineResult.Summary, visibleGames.Count);

            if (isStartupScan)
            {
                var statusText = summary.ExecutableCount > 0 && summary.MatchedCount > 0
                    ? Format(strings.ScanStartupMatchedGamesFromExecutables, summary.MatchedCount, summary.ExecutableCount)
                    : strings.ScanStartupNoSupportedGames;
                logs.Add(Info(
                    "scan",
                    $"startup auto scan completed executable_count={summary.ExecutableCount} candidate_exe_count={summary.CandidateExecutableCount} matched={summary.MatchedCount} duplicate={summary.DuplicateMatchCount} visible_games={summary.VisibleGameCount}"));

                return new ScanFlowResult
                {
                    DidRun = true,
                    ShouldNavigateHome = false,
                    ShouldRecomputeSelection = true,
                    StatusText = statusText,
                    VisibleGames = visibleGames,
                    MatchByGameId = matchByGameId,
                    TargetPathByGameId = targetPathByGameId,
                    Summary = summary,
                    Logs = logs
                };
            }

            if (summary.ExecutableCount == 0)
            {
                return new ScanFlowResult
                {
                    DidRun = true,
                    ShouldNavigateHome = false,
                    ShouldRecomputeSelection = true,
                    StatusText = strings.ScanNoExecutableFound,
                    VisibleGames = visibleGames,
                    MatchByGameId = matchByGameId,
                    TargetPathByGameId = targetPathByGameId,
                    DialogRequest = BuildScanWarningDialog(strings.NavScan, strings.ScanNoExecutableFound),
                    Summary = summary,
                    Logs = logs
                };
            }

            if (summary.MatchedCount == 0)
            {
                var statusText = Format(strings.ScanNoSupportedGamesMatchedFromExecutables, summary.ExecutableCount);
                return new ScanFlowResult
                {
                    DidRun = true,
                    ShouldNavigateHome = false,
                    ShouldRecomputeSelection = true,
                    StatusText = statusText,
                    VisibleGames = visibleGames,
                    MatchByGameId = matchByGameId,
                    TargetPathByGameId = targetPathByGameId,
                    DialogRequest = BuildScanWarningDialog(strings.NavScan, statusText),
                    Summary = summary,
                    Logs = logs
                };
            }

            return new ScanFlowResult
            {
                DidRun = true,
                ShouldNavigateHome = visibleGames.Count > 0,
                ShouldRecomputeSelection = true,
                StatusText = Format(strings.ScanLastScanMatchedGamesFromExecutables, summary.MatchedCount, summary.ExecutableCount),
                VisibleGames = visibleGames,
                MatchByGameId = matchByGameId,
                TargetPathByGameId = targetPathByGameId,
                Summary = summary,
                Logs = logs
            };
        }
        catch (Exception ex)
        {
            if (isStartupScan)
            {
                logs.Add(Error("scan", "startup auto scan failed", ex));
                return CreateSkippedResult(
                    currentMatchByGameId,
                    currentTargetPathByGameId,
                    logs,
                    statusText: strings.ScanStartupFailedTryAgain);
            }

            logs.Add(Error("scan", "scan failed", ex));
            return CreateSkippedResult(
                currentMatchByGameId,
                currentTargetPathByGameId,
                logs,
                statusText: strings.ScanFailedSeeLog,
                dialogRequest: BuildScanWarningDialog(strings.NavScan, strings.ScanFailedSeeLog));
        }
    }

    private IReadOnlyList<GameCardViewModel> CreateVisibleGames(
        ShellGameCatalog catalog,
        RuntimeContext runtimeContext,
        IReadOnlyDictionary<string, ShellGameMatchResult> matchByGameId,
        IReadOnlyDictionary<string, string> targetPathByGameId,
        IReadOnlyDictionary<string, object?> moduleDownloadLinks,
        ICollection<ScanFlowLogEntry> logs)
    {
        if (_shellGameCardViewModelFactory is null)
        {
            return [];
        }

        var matchedGames = _visibleGameResolver.ResolveMatchedCatalogGames(catalog, matchByGameId);
        if (matchedGames.Count == 0)
        {
            return [];
        }

        try
        {
            return _shellGameCardViewModelFactory.CreateCards(
                matchedGames,
                runtimeContext,
                targetPathByGameId,
                moduleDownloadLinks);
        }
        catch (Exception ex)
        {
            logs.Add(Error("scan", "failed to create visible cards from scan matches", ex));
            return [];
        }
    }

    private static ScanExecutionSummary NormalizeSummary(ScanExecutionSummary? summary, int visibleGameCount)
    {
        var safeSummary = summary ?? new ScanExecutionSummary();
        return new ScanExecutionSummary
        {
            ExecutableCount = safeSummary.ExecutableCount,
            CandidateExecutableCount = safeSummary.CandidateExecutableCount,
            MatchedCount = safeSummary.MatchedCount,
            MultipleCandidateCount = safeSummary.MultipleCandidateCount,
            DisabledCount = safeSummary.DisabledCount,
            UnsupportedCount = safeSummary.UnsupportedCount,
            UnmatchedCount = safeSummary.UnmatchedCount,
            DuplicateMatchCount = safeSummary.DuplicateMatchCount,
            VisibleGameCount = visibleGameCount,
            SkippedDirectoryCount = safeSummary.SkippedDirectoryCount
        };
    }

    private static ScanFlowResult CreateSkippedResult(
        IReadOnlyDictionary<string, ShellGameMatchResult> matchByGameId,
        IReadOnlyDictionary<string, string> targetPathByGameId,
        IReadOnlyList<ScanFlowLogEntry> logs,
        string statusText = "",
        bool shouldRecomputeSelection = false,
        IReadOnlyList<GameCardViewModel>? visibleGames = null,
        AppDialogRequest? dialogRequest = null)
    {
        return new ScanFlowResult
        {
            DidRun = false,
            ShouldNavigateHome = false,
            ShouldRecomputeSelection = shouldRecomputeSelection,
            StatusText = statusText,
            VisibleGames = visibleGames ?? [],
            MatchByGameId = matchByGameId,
            TargetPathByGameId = targetPathByGameId,
            DialogRequest = dialogRequest,
            Summary = new ScanExecutionSummary(),
            Logs = logs
        };
    }

    private static AppDialogRequest BuildScanWarningDialog(
        string title,
        string summary,
        params string[] bulletItems)
    {
        return new AppDialogRequest
        {
            Kind = AppDialogKind.Warning,
            Severity = DialogSeverity.Warning,
            Title = title,
            Summary = summary,
            BulletItems = bulletItems ?? []
        };
    }

    private static IReadOnlyDictionary<string, ShellGameMatchResult> CloneMatchByGameId(
        IReadOnlyDictionary<string, ShellGameMatchResult>? source)
    {
        var safeSource = source ?? new Dictionary<string, ShellGameMatchResult>(StringComparer.OrdinalIgnoreCase);
        var result = new Dictionary<string, ShellGameMatchResult>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in safeSource)
        {
            result[pair.Key] = pair.Value;
        }

        return result;
    }

    private static IReadOnlyDictionary<string, string> CloneTargetPathByGameId(IReadOnlyDictionary<string, string>? source)
    {
        var safeSource = source ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in safeSource)
        {
            result[pair.Key] = pair.Value;
        }

        return result;
    }

    private static ScanFlowLogEntry Info(string category, string message)
    {
        return new ScanFlowLogEntry
        {
            Level = "info",
            Category = category,
            Message = message
        };
    }

    private static ScanFlowLogEntry Warning(string category, string message)
    {
        return new ScanFlowLogEntry
        {
            Level = "warning",
            Category = category,
            Message = message
        };
    }

    private static ScanFlowLogEntry Error(string category, string message, Exception? exception = null)
    {
        return new ScanFlowLogEntry
        {
            Level = "error",
            Category = category,
            Message = message,
            Exception = exception
        };
    }

    private static string Format(string template, params object[] args)
    {
        return string.Format(CultureInfo.CurrentCulture, template ?? "", args ?? []);
    }

    private static string NormalizeStatusCode(string? value, string fallback)
    {
        var normalized = (value ?? "").Trim();
        return string.IsNullOrWhiteSpace(normalized) ? fallback : normalized;
    }
}
