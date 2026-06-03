using OptiClick.Wpf.Models;
using OptiClick.Wpf.Shell.Flow;

namespace OptiClick.Wpf.Shell.Scan;

public sealed record ScanFolderActionResult
{
    public string? StatusText { get; init; }
    public AppDialogRequest? DialogRequest { get; init; }
    public IReadOnlyList<IFlowLogEntry> Logs { get; init; } = [];
    public ScanFolderStateUpdate? StateUpdate { get; init; }
}
