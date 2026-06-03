namespace OptiClick.Wpf.Shell.Startup;

public sealed class ArchiveReadinessRefreshCoordinator
{
    private readonly object _backgroundGate = new();
    private readonly SemaphoreSlim _refreshGate = new(1, 1);
    private CancellationTokenSource? _backgroundRefreshSource;

    public async Task<T> RunBackgroundRefreshAsync<T>(
        Func<CancellationToken, Task<T>> refreshAction,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(refreshAction);

        using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        RegisterBackgroundSource(linkedSource);
        try
        {
            await _refreshGate.WaitAsync(linkedSource.Token).ConfigureAwait(false);
            try
            {
                var result = await refreshAction(linkedSource.Token).ConfigureAwait(false);
                linkedSource.Token.ThrowIfCancellationRequested();
                return result;
            }
            finally
            {
                _refreshGate.Release();
            }
        }
        finally
        {
            ClearBackgroundSource(linkedSource);
        }
    }

    public async Task<T> RunForegroundRefreshAsync<T>(
        Func<CancellationToken, Task<T>> refreshAction,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(refreshAction);

        CancelBackgroundRefresh();
        await _refreshGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var result = await refreshAction(cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            return result;
        }
        finally
        {
            _refreshGate.Release();
        }
    }

    public void CancelBackgroundRefresh()
    {
        CancellationTokenSource? source;
        lock (_backgroundGate)
        {
            source = _backgroundRefreshSource;
        }

        try
        {
            source?.Cancel();
        }
        catch
        {
            // Ignore cancellation failures while a foreground refresh takes priority.
        }
    }

    private void RegisterBackgroundSource(CancellationTokenSource source)
    {
        CancellationTokenSource? previousSource;
        lock (_backgroundGate)
        {
            previousSource = _backgroundRefreshSource;
            _backgroundRefreshSource = source;
        }

        try
        {
            previousSource?.Cancel();
        }
        catch
        {
            // Ignore cancellation failures for an obsolete background refresh.
        }
    }

    private void ClearBackgroundSource(CancellationTokenSource source)
    {
        lock (_backgroundGate)
        {
            if (ReferenceEquals(_backgroundRefreshSource, source))
            {
                _backgroundRefreshSource = null;
            }
        }
    }
}
