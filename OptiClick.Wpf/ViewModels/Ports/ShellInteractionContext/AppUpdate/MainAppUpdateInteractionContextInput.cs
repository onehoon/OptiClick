using OptiClick.Wpf.Localization;
using OptiClick.Wpf.Models;
using OptiClick.Wpf.Services;
using OptiClick.Wpf.Shell.Flow;
using OptiClick.Wpf.Shell.RuntimeData;
using OptiClick.Wpf.Shell.Selection;
using OptiClick.Wpf.Shell.Update;

namespace OptiClick.Wpf.ViewModels.Ports.ShellInteractionContext.AppUpdate;

internal sealed record MainAppUpdateInteractionContextInput
{
    public required MainViewModelBusyStateApplier BusyStateApplier { get; init; }
    public required MainViewModelResultApplier ResultApplier { get; init; }
    public required AppUpdateCoordinator AppUpdateCoordinator { get; init; }
    public required AppUpdateFlowController AppUpdateFlowController { get; init; }
    public required Func<AppStrings> ReadStrings { get; init; }
    public required Func<RemoteRuntimeData> ReadLatestRuntimeData { get; init; }
    public required Func<string> ReadCurrentAppVersion { get; init; }
    public required Func<bool> IsAppUpdateInProgress { get; init; }
    public required Func<bool> IsInstallExecutionInProgress { get; init; }
    public required Func<ShellInstallSelectionState> ReadSelectionState { get; init; }
    public required Action<string> SetSettingsStatusText { get; init; }
    public required Action<IEnumerable<IFlowLogEntry>, string> DispatchFlowLogs { get; init; }
    public required Func<AppDialogRequest, CancellationToken, Task<AppDialogResult>> ShowDialogAsync { get; init; }
    public required Action<MainViewModelBusyStateUpdate> ApplyBusyStateUpdate { get; init; }
    public required Action<MainViewModelStateUpdate> ApplyStateUpdate { get; init; }
    public required Action<string> LogError { get; init; }
    public required Action ShutdownApplication { get; init; }
}
