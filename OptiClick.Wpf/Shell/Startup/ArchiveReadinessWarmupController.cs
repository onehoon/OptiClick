using System.Diagnostics;

namespace OptiClick.Wpf.Shell.Startup;

public sealed class ArchiveReadinessWarmupController
{
    private readonly object _gate = new();
    private ArchiveReadinessWarmupState _state = ArchiveReadinessWarmupState.NotStarted;

    public ArchiveReadinessWarmupState State
    {
        get
        {
            lock (_gate)
            {
                return _state;
            }
        }
    }

    public bool CanStart
    {
        get
        {
            var state = State;
            return state is ArchiveReadinessWarmupState.NotStarted
                or ArchiveReadinessWarmupState.Failed
                or ArchiveReadinessWarmupState.Canceled;
        }
    }

    public async Task StartAsync(
        Func<CancellationToken, Task> warmupAction,
        Action<string> logInfo,
        Action<string> logWarning,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(warmupAction);
        ArgumentNullException.ThrowIfNull(logInfo);
        ArgumentNullException.ThrowIfNull(logWarning);

        if (!TryBeginWarmup(logInfo, logWarning))
        {
            return;
        }

        logInfo("archive_readiness_warmup started");
        var stopwatch = Stopwatch.StartNew();

        try
        {
            await warmupAction(cancellationToken);
            SetState(ArchiveReadinessWarmupState.Completed);
            logInfo("archive_readiness_warmup completed");
            logInfo($"archive_readiness_warmup duration_ms={stopwatch.ElapsedMilliseconds}");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            SetState(ArchiveReadinessWarmupState.Canceled);
            logWarning("archive_readiness_warmup skipped reason=canceled");
            logWarning($"archive_readiness_warmup canceled duration_ms={stopwatch.ElapsedMilliseconds}");
        }
        catch (Exception ex)
        {
            SetState(ArchiveReadinessWarmupState.Failed);
            logWarning($"archive_readiness_warmup failed type={ex.GetType().Name}");
            logWarning($"archive_readiness_warmup failed duration_ms={stopwatch.ElapsedMilliseconds}");
        }
    }

    private bool TryBeginWarmup(Action<string> logInfo, Action<string> logWarning)
    {
        lock (_gate)
        {
            switch (_state)
            {
                case ArchiveReadinessWarmupState.Running:
                    logWarning("archive_readiness_warmup skipped reason=already_running");
                    return false;
                case ArchiveReadinessWarmupState.Completed:
                    logInfo("archive_readiness_warmup skipped reason=already_completed");
                    return false;
                default:
                    _state = ArchiveReadinessWarmupState.Running;
                    return true;
            }
        }
    }

    private void SetState(ArchiveReadinessWarmupState state)
    {
        lock (_gate)
        {
            _state = state;
        }
    }
}
