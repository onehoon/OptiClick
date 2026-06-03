using OptiClick.Wpf.Shell.Selection;

namespace OptiClick.Wpf.ViewModels;

public sealed record MainViewModelBusyStateUpdate
{
    public bool IsAppUpdateInProgress { get; init; }
    public bool IsInstallExecutionInProgress { get; init; }
    public bool IsOperationOverlayVisible { get; init; }
    public string OperationOverlayMessage { get; init; } = "";
    public ShellInstallSelectionState SelectionState { get; init; } = new();
    public bool ShouldRefreshInstallCommand { get; init; }
    public bool ShouldApplySelectedGameActionRunningState { get; init; }
    public string SettingsStatusText { get; init; } = "";
}
