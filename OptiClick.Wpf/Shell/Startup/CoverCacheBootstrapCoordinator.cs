using OptiClick.Wpf.Install.Archives;

namespace OptiClick.Wpf.Shell.Startup;

public sealed class CoverCacheBootstrapCoordinator
{
    private readonly ICoverCacheBootstrapService _coverCacheBootstrapService;

    public CoverCacheBootstrapCoordinator(ICoverCacheBootstrapService coverCacheBootstrapService)
    {
        _coverCacheBootstrapService = coverCacheBootstrapService ?? throw new ArgumentNullException(nameof(coverCacheBootstrapService));
    }

    public bool IsReady()
    {
        return _coverCacheBootstrapService.IsReady();
    }

    public async Task<CoverCacheBootstrapResult> StartForColdStartAsync(
        StartupPreparationCoordinatorRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        request.StatePort.UpdateStartupPreparationState(state => state with
        {
            CoverCacheBootstrapState = CoverCacheBootstrapState.Pending
        });

        var progress = new Progress<CoverCacheBootstrapState>(
            nextState => request.StatePort.UpdateStartupPreparationState(state => state with
            {
                CoverCacheBootstrapState = nextState
            }));

        try
        {
            var result = await _coverCacheBootstrapService.BootstrapAsync(progress, cancellationToken);
            request.StatePort.UpdateStartupPreparationState(state => state with
            {
                CoverCacheBootstrapState = result.State
            });
            request.LogPort.LogAppInfo(
                $"cover_cache_bootstrap completed state={NormalizeStatusCode(result.State.ToString(), "unknown")} attempted={(result.Attempted ? "true" : "false")} copied_files={result.CopiedFileCount} error={NormalizeStatusCode(result.ErrorCode, "none")}");
            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            request.LogPort.LogAppWarning("cover_cache_bootstrap skipped reason=canceled");
            throw;
        }
        catch (Exception ex)
        {
            request.LogPort.LogAppWarning($"cover_cache_bootstrap fallback_enabled type={ex.GetType().Name}");
            var fallback = CoverCacheBootstrapResult.FailedFallbackEnabled("cover_cache_bootstrap_failed");
            request.StatePort.UpdateStartupPreparationState(state => state with
            {
                CoverCacheBootstrapState = fallback.State
            });
            return fallback;
        }
    }

    public async Task<CoverCacheBootstrapResult> ResolveCompletionAsync(
        StartupPreparationCoordinatorRequest request,
        Task<CoverCacheBootstrapResult>? coverCacheBootstrapTask,
        CancellationTokenSource? coverCacheBootstrapCancellation,
        ArchiveReadinessWarmupState archiveWarmupState,
        ArchiveReadinessFlowResult? archiveReadinessResult)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (coverCacheBootstrapTask is null)
        {
            return CoverCacheBootstrapResult.NotRequired();
        }

        var readiness = archiveReadinessResult?.Readiness ?? request.StatePort.ReadLatestArchiveReadiness();
        if (archiveWarmupState == ArchiveReadinessWarmupState.Completed
            && readiness.AreAllStartupArchivesReady())
        {
            try
            {
                return await coverCacheBootstrapTask;
            }
            catch (OperationCanceledException)
            {
                return CoverCacheBootstrapResult.NotRequired();
            }
        }

        coverCacheBootstrapCancellation?.Cancel();
        try
        {
            await coverCacheBootstrapTask;
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            request.LogPort.LogAppWarning($"cover_cache_bootstrap abandoned_after_archive_failure type={ex.GetType().Name}");
        }

        return CoverCacheBootstrapResult.NotRequired();
    }

    public static bool IsReady(CoverCacheBootstrapState state)
    {
        return state is CoverCacheBootstrapState.NotRequired
            or CoverCacheBootstrapState.Completed
            or CoverCacheBootstrapState.FailedFallbackEnabled;
    }

    private static string NormalizeStatusCode(string? value, string fallback)
    {
        var normalized = (value ?? "").Trim();
        return string.IsNullOrWhiteSpace(normalized)
            ? fallback
            : normalized.Replace(' ', '_');
    }
}
