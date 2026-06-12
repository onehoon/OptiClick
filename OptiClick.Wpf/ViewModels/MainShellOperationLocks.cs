using System.Threading;

namespace OptiClick.Wpf.ViewModels;

internal sealed class MainShellOperationLocks
{
    public object StartupPreparationStateGate { get; } = new();
    public SemaphoreSlim DeviceRulesRefreshLock { get; } = new(1, 1);
    public SemaphoreSlim ScanLock { get; } = new(1, 1);
    public SemaphoreSlim InstallExecutionLock { get; } = new(1, 1);
}
