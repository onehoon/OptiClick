using OptiClick.Wpf.Install.Flow;
using OptiClick.Wpf.Models;
using OptiClick.Wpf.Shell.Flow;
using OptiClick.Wpf.Shell.Selection;

namespace OptiClick.Wpf.ViewModels;

internal sealed record MainInstallContextFactorySharedCompositionInput
{
    public required Action<bool, string, ShellInstallSelectionState?> ApplyInstallBusyState { get; init; }
    public required Func<string> ReadInstallButtonText { get; init; }
    public required Func<string, GameCardViewModel?> TryRefreshVisibleCard { get; init; }
    public required Func<GameCardViewModel?, CancellationToken, bool, bool, Task> SelectGameAsync { get; init; }
    public required Func<ModuleDownloadLinkContext> ReadModuleDownloadLinks { get; init; }
    public required Action<IReadOnlyList<IFlowLogEntry>, string> DispatchFlowLogs { get; init; }
}

internal sealed record MainInstallContextFactorySharedCompositionServices
{
    public required MainInstallBusyActions BusyActions { get; init; }
    public required MainInstallSelectionRefreshActions SelectionRefreshActions { get; init; }
    public required MainInstallRuntimeSnapshotReaders RuntimeSnapshotReaders { get; init; }
    public required MainInstallFlowLogActions FlowLogActions { get; init; }
}

internal static class MainInstallContextFactorySharedComposition
{
    public static MainInstallContextFactorySharedCompositionServices Compose(
        MainInstallContextFactorySharedCompositionInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new MainInstallContextFactorySharedCompositionServices
        {
            BusyActions = new MainInstallBusyActions
            {
                ApplyInstallBusyState = input.ApplyInstallBusyState
            },
            SelectionRefreshActions = new MainInstallSelectionRefreshActions
            {
                ReadInstallButtonText = input.ReadInstallButtonText,
                TryRefreshVisibleCard = input.TryRefreshVisibleCard,
                SelectGameAsync = input.SelectGameAsync
            },
            RuntimeSnapshotReaders = new MainInstallRuntimeSnapshotReaders
            {
                ReadModuleDownloadLinks = input.ReadModuleDownloadLinks
            },
            FlowLogActions = new MainInstallFlowLogActions
            {
                DispatchFlowLogs = input.DispatchFlowLogs
            }
        };
    }
}
