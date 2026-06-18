using OptiClick.Core.Runtime;
using OptiClick.Wpf.Models;
using OptiClick.Wpf.Shell.Flow;
using OptiClick.Wpf.Shell.RuntimeData;

namespace OptiClick.Wpf.ViewModels.Ports.ShellInteractionContext.StartupAnnouncement;

internal sealed record MainStartupAnnouncementInteractionContext
{
    public required RemoteRuntimeData RuntimeData { get; init; }
    public required AppLanguage Language { get; init; }
    public required string SelectedGpuVendor { get; init; }
    public required Action<IEnumerable<IFlowLogEntry>, string> DispatchFlowLogs { get; init; }
    public required Func<AppDialogRequest, CancellationToken, Task<AppDialogResult>> ShowDialogAsync { get; init; }
    public required Func<string, bool> OpenExternalUrl { get; init; }
    public required Action<string> LogWarning { get; init; }
}
