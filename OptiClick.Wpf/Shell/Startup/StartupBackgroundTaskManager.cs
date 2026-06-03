namespace OptiClick.Wpf.Shell.Startup;

public sealed class StartupBackgroundTaskManager : IDisposable
{
    private readonly object _gate = new();
    private readonly List<CancellationTokenSource> _sources = [];
    private bool _disposed;

    public CancellationTokenSource CreateSource(CancellationToken parent = default)
    {
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            var source = parent.CanBeCanceled
                ? CancellationTokenSource.CreateLinkedTokenSource(parent)
                : new CancellationTokenSource();
            _sources.Add(source);
            return source;
        }
    }

    public void CancelAll()
    {
        CancellationTokenSource[] sources;
        lock (_gate)
        {
            sources = _sources.ToArray();
        }

        foreach (var source in sources)
        {
            try
            {
                source.Cancel();
            }
            catch
            {
                // Ignore cancellation failures while the app is shutting down.
            }
        }
    }

    public void Remove(CancellationTokenSource source)
    {
        ArgumentNullException.ThrowIfNull(source);
        var shouldDispose = false;
        lock (_gate)
        {
            shouldDispose = _sources.Remove(source);
        }

        if (shouldDispose)
        {
            source.Dispose();
        }
    }

    public void Dispose()
    {
        CancellationTokenSource[] sources;
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            sources = _sources.ToArray();
            _sources.Clear();
        }

        foreach (var source in sources)
        {
            try
            {
                source.Cancel();
            }
            catch
            {
                // Ignore cancellation failures while the app is shutting down.
            }

            source.Dispose();
        }
    }
}
