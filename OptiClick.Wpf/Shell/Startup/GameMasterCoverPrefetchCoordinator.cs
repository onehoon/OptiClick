using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OptiClick.Wpf.Services;
using OptiClick.Wpf.Shell.Games;
using OptiClick.Wpf.Shell.RuntimeData;
using OptiClick.Wpf.ViewModels;

namespace OptiClick.Wpf.Shell.Startup;

public sealed class GameMasterCoverPrefetchCoordinator
{
    private const string CoverPrefetchFailedCode = "cover_prefetch_failed";

    private readonly IGameMasterCoverPrefetchService _prefetchService;
    private readonly StartupBackgroundTaskManager _backgroundTaskManager;
    private int _gameMasterCoverPrefetchStarted;
    private int _homeCoverPrefetchRunning;

    public GameMasterCoverPrefetchCoordinator(
        IGameMasterCoverPrefetchService prefetchService,
        StartupBackgroundTaskManager backgroundTaskManager)
    {
        _prefetchService = prefetchService ?? throw new ArgumentNullException(nameof(prefetchService));
        _backgroundTaskManager = backgroundTaskManager ?? throw new ArgumentNullException(nameof(backgroundTaskManager));
    }

    public void StartGameMasterCoverPrefetchInBackground(GameMasterCoverPrefetchCoordinatorRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (Interlocked.CompareExchange(ref _gameMasterCoverPrefetchStarted, 1, 0) != 0)
        {
            return;
        }

        request.UpdateStartupPreparationState(state => state with
        {
            GameMasterCoverPrefetchStarted = true,
            GameMasterCoverPrefetchRunning = true,
            GameMasterCoverPrefetchCompleted = false,
            GameMasterCoverPrefetchCanceled = false,
            GameMasterCoverPrefetchFailed = false
        });
        var cancellationTokenSource = _backgroundTaskManager.CreateSource();
        _ = RunGameMasterCoverPrefetchInBackgroundAsync(cancellationTokenSource, request);
    }

    public void QueueHomeCoverPrefetchInBackground(GameMasterHomeCoverPrefetchCoordinatorRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.HomeCardsAccessor().Count == 0)
        {
            return;
        }

        var normalizedReason = NormalizeStatusCode(request.Reason, "unknown");
        if (Interlocked.CompareExchange(ref _homeCoverPrefetchRunning, 1, 0) != 0)
        {
            request.LogInfo($"home_cover_prefetch skipped reason=already_running source={normalizedReason}");
            return;
        }

        var cancellationTokenSource = _backgroundTaskManager.CreateSource();
        _ = RunHomeCoverPrefetchInBackgroundAsync(cancellationTokenSource, request, normalizedReason);
    }

    private async Task RunGameMasterCoverPrefetchInBackgroundAsync(
        CancellationTokenSource cancellationTokenSource,
        GameMasterCoverPrefetchCoordinatorRequest request)
    {
        var cancellationToken = cancellationTokenSource.Token;
        var prefetchStopwatch = Stopwatch.StartNew();
        var canceled = false;
        var failed = false;
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);

            var gameMaster = request.GameMasterAccessor();
            if (gameMaster.Count == 0)
            {
                request.LogInfo("cover_prefetch skipped reason=no_game_master");
                return;
            }

            var prefetchTargets = GameMasterCoverPrefetchService.CollectTargets(gameMaster);
            if (prefetchTargets.Count == 0)
            {
                request.LogInfo("cover_prefetch skipped reason=no_targets");
                return;
            }

            var prioritizedTargets = ResolveHomeCoverPrefetchTargets(
                prefetchTargets,
                request.HomeCardsAccessor());

            if (prioritizedTargets.Length == 0)
            {
                request.LogInfo("cover_prefetch skipped reason=no_home_targets");
                return;
            }

            request.LogInfo(
                $"cover_prefetch started total={prefetchTargets.Count} home_priority={prioritizedTargets.Length} scope=home_only remaining=not_prefetched");
            var homeStageStopwatch = Stopwatch.StartNew();
            var homeSummary = await _prefetchService.PrefetchAsync(
                prioritizedTargets,
                [],
                cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            await request.RefreshHomeCoversOnDispatcherAsync();

            request.LogInfo(
                $"cover_prefetch completed total={homeSummary.Total} cached={homeSummary.Cached} downloaded={homeSummary.Downloaded} skipped={homeSummary.Skipped} failed={homeSummary.Failed} duration_ms={homeStageStopwatch.ElapsedMilliseconds}");
        }
        catch (OperationCanceledException)
        {
            canceled = true;
            request.LogWarning($"cover_prefetch skipped reason=canceled duration_ms={prefetchStopwatch.ElapsedMilliseconds}");
        }
        catch (Exception ex)
        {
            failed = true;
            request.LogWarning(
                $"cover_prefetch failed type={ex.GetType().Name} duration_ms={prefetchStopwatch.ElapsedMilliseconds}");
        }
        finally
        {
            request.UpdateStartupPreparationState(state => state with
            {
                GameMasterCoverPrefetchRunning = false,
                GameMasterCoverPrefetchCompleted = !canceled && !failed,
                GameMasterCoverPrefetchCanceled = canceled,
                GameMasterCoverPrefetchFailed = failed,
                LastErrorCode = failed
                    ? CoverPrefetchFailedCode
                    : !canceled
                        ? request.ClearLastErrorCode(state.LastErrorCode, CoverPrefetchFailedCode)
                        : state.LastErrorCode
            });
            _backgroundTaskManager.Remove(cancellationTokenSource);
        }
    }

    private async Task RunHomeCoverPrefetchInBackgroundAsync(
        CancellationTokenSource cancellationTokenSource,
        GameMasterHomeCoverPrefetchCoordinatorRequest request,
        string normalizedReason)
    {
        var cancellationToken = cancellationTokenSource.Token;
        try
        {
            var gameMaster = request.GameMasterAccessor();
            if (gameMaster.Count == 0)
            {
                return;
            }

            var prefetchTargets = GameMasterCoverPrefetchService.CollectTargets(gameMaster);
            var homeTargets = ResolveHomeCoverPrefetchTargets(
                prefetchTargets,
                request.HomeCardsAccessor());
            if (homeTargets.Length == 0)
            {
                return;
            }

            request.LogInfo($"home_cover_prefetch started reason={normalizedReason} total={homeTargets.Length}");
            var stopwatch = Stopwatch.StartNew();
            var summary = await _prefetchService.PrefetchAsync(
                homeTargets,
                [],
                cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            await request.RefreshHomeCoversOnDispatcherAsync();
            request.LogInfo(
                $"home_cover_prefetch completed reason={normalizedReason} total={summary.Total} cached={summary.Cached} downloaded={summary.Downloaded} skipped={summary.Skipped} failed={summary.Failed} duration_ms={stopwatch.ElapsedMilliseconds}");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            request.LogWarning($"home_cover_prefetch skipped reason=canceled source={normalizedReason}");
        }
        catch (Exception ex)
        {
            request.LogWarning($"home_cover_prefetch failed reason={normalizedReason} type={ex.GetType().Name}");
        }
        finally
        {
            Interlocked.Exchange(ref _homeCoverPrefetchRunning, 0);
            _backgroundTaskManager.Remove(cancellationTokenSource);
        }
    }

    private static GameMasterCoverPrefetchTarget[] ResolveHomeCoverPrefetchTargets(
        IReadOnlyList<GameMasterCoverPrefetchTarget> prefetchTargets,
        IReadOnlyCollection<GameCardViewModel> homeCards)
    {
        if (prefetchTargets.Count == 0)
        {
            return [];
        }

        var homeCardKeys = ResolveHomeCardCoverMatchKeys(homeCards);
        if (homeCardKeys.Count == 0)
        {
            return [];
        }

        return [..prefetchTargets.Where(target => IsHomeCardCoverTarget(target, homeCardKeys))];
    }

    private static HashSet<string> ResolveHomeCardCoverMatchKeys(IReadOnlyCollection<GameCardViewModel> homeCards)
    {
        if (homeCards.Count == 0)
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var card in homeCards)
        {
            var sourceUrl = (card.CoverImageSource ?? "").Trim();
            if (!string.IsNullOrWhiteSpace(sourceUrl)
                && sourceUrl.StartsWith("pack://application:", StringComparison.OrdinalIgnoreCase) == false)
            {
                var cacheSourceUrl = ResolveHomeCardSourceUrl(card.SourceModel);
                if (!string.IsNullOrWhiteSpace(cacheSourceUrl))
                {
                    keys.Add(BuildHomeCardMatchKey("source", cacheSourceUrl));
                }
            }

            var sourceGameId = (card.SourceModel?.GameId ?? "").Trim();
            if (!string.IsNullOrWhiteSpace(sourceGameId))
            {
                keys.Add(BuildHomeCardMatchKey("gameId", sourceGameId));
            }

            var entryGameId = (card.GameEntry?.GameId ?? "").Trim();
            if (!string.IsNullOrWhiteSpace(entryGameId))
            {
                keys.Add(BuildHomeCardMatchKey("gameId", entryGameId));
            }

            var sourceModelSteamAppId = (card.SourceModel?.CoverSteamAppId ?? "").Trim();
            if (!string.IsNullOrWhiteSpace(sourceModelSteamAppId))
            {
                keys.Add(BuildHomeCardMatchKey("steam", sourceModelSteamAppId));
            }

            var coverUrl = (card.SourceModel?.CoverUrl ?? "").Trim();
            if (!string.IsNullOrWhiteSpace(coverUrl))
            {
                keys.Add(BuildHomeCardMatchKey("source", coverUrl));
            }
        }

        return keys;
    }

    private static bool IsHomeCardCoverTarget(GameMasterCoverPrefetchTarget target, HashSet<string> homeCardKeys)
    {
        if (homeCardKeys.Count == 0)
        {
            return false;
        }

        var gameId = (target.GameId ?? "").Trim();
        if (!string.IsNullOrWhiteSpace(gameId) && homeCardKeys.Contains(BuildHomeCardMatchKey("gameId", gameId)))
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(target.SteamAppId)
            && homeCardKeys.Contains(BuildHomeCardMatchKey("steam", target.SteamAppId)))
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(target.SourceUrl)
            && homeCardKeys.Contains(BuildHomeCardMatchKey("source", target.SourceUrl)))
        {
            return true;
        }

        return false;
    }

    private static string ResolveHomeCardSourceUrl(ShellGameCardModel? sourceModel)
    {
        if (sourceModel is null)
        {
            return "";
        }

        if (!string.IsNullOrWhiteSpace(sourceModel.CoverUrl))
        {
            return sourceModel.CoverUrl.Trim();
        }

        if (!string.IsNullOrWhiteSpace(sourceModel.CoverSteamAppId))
        {
            return CoverImageCacheService.BuildSteamCoverUrl(sourceModel.CoverSteamAppId);
        }

        return "";
    }

    private static string BuildHomeCardMatchKey(string category, string value)
    {
        return $"{category}::{(value ?? "").Trim()}";
    }

    private static string NormalizeStatusCode(string? value, string fallback)
    {
        var normalized = (value ?? "").Trim();
        return string.IsNullOrWhiteSpace(normalized)
            ? fallback
            : normalized;
    }
}

public sealed record GameMasterCoverPrefetchCoordinatorRequest
{
    public required Func<IReadOnlyList<RuntimeDataGameProfile>> GameMasterAccessor { get; init; }
    public required Func<IReadOnlyCollection<GameCardViewModel>> HomeCardsAccessor { get; init; }
    public required Func<Task> RefreshHomeCoversOnDispatcherAsync { get; init; }
    public required Action<Func<StartupPreparationState, StartupPreparationState>> UpdateStartupPreparationState { get; init; }
    public required Func<string, string, string> ClearLastErrorCode { get; init; }
    public required Action<string> LogInfo { get; init; }
    public required Action<string> LogWarning { get; init; }
}

public sealed record GameMasterHomeCoverPrefetchCoordinatorRequest
{
    public required string Reason { get; init; }
    public required Func<IReadOnlyList<RuntimeDataGameProfile>> GameMasterAccessor { get; init; }
    public required Func<IReadOnlyCollection<GameCardViewModel>> HomeCardsAccessor { get; init; }
    public required Func<Task> RefreshHomeCoversOnDispatcherAsync { get; init; }
    public required Action<string> LogInfo { get; init; }
    public required Action<string> LogWarning { get; init; }
}
