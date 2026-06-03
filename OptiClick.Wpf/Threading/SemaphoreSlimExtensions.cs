using System;
using System.Threading;
using System.Threading.Tasks;

namespace OptiClick.Wpf.Threading;

internal static class SemaphoreSlimExtensions
{
    public static async Task<bool> TryRunExclusiveAsync(
        this SemaphoreSlim semaphore,
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(semaphore);
        ArgumentNullException.ThrowIfNull(action);

        if (!await semaphore.WaitAsync(0, cancellationToken))
        {
            return false;
        }

        try
        {
            await action(cancellationToken);
            return true;
        }
        finally
        {
            semaphore.Release();
        }
    }
}
