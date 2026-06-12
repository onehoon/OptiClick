using OptiClick.Core.Abstractions;
using OptiClick.Core.Models;
using OptiClick.Wpf.Install.Archives;

namespace OptiClick.Wpf.Shell.Startup;

public sealed class FirstRunStateMarkerService
{
    private readonly IFirstRunStateStore _firstRunStateStore;

    public FirstRunStateMarkerService(IFirstRunStateStore firstRunStateStore)
    {
        _firstRunStateStore = firstRunStateStore ?? throw new ArgumentNullException(nameof(firstRunStateStore));
    }

    public Task SaveCompletedMarkerAsync(
        CancellationToken cancellationToken,
        CoverCacheBootstrapResult? coverCacheBootstrapResult = null)
    {
        return _firstRunStateStore.SaveAsync(
            new FirstRunState
            {
                FirstStartupCompleted = true,
                ArchivePreparedOnce = true,
                CoverCacheBootstrapAttempted = coverCacheBootstrapResult?.Attempted ?? false,
                CoverCacheBootstrapState = coverCacheBootstrapResult?.State.ToString() ?? "",
                CreatedAt = DateTimeOffset.UtcNow
            },
            cancellationToken);
    }
}
