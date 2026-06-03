using OptiClick.Wpf.Shell.Selection;

namespace OptiClick.Wpf.ViewModels;

public sealed class MainViewModelBusyStateApplier
{
    public MainViewModelBusyStateUpdate CreateAppUpdateBusyState(
        bool inProgress,
        bool currentInstallExecutionInProgress,
        ShellInstallSelectionState currentSelectionState,
        string operationOverlayMessage = "")
    {
        ArgumentNullException.ThrowIfNull(currentSelectionState);

        return new MainViewModelBusyStateUpdate
        {
            IsAppUpdateInProgress = inProgress,
            IsInstallExecutionInProgress = currentInstallExecutionInProgress,
            IsOperationOverlayVisible = inProgress,
            OperationOverlayMessage = inProgress ? operationOverlayMessage : "",
            SelectionState = currentSelectionState with
            {
                AppUpdateInProgress = inProgress
            },
            ShouldRefreshInstallCommand = true,
            ShouldApplySelectedGameActionRunningState = true
        };
    }

    public MainViewModelBusyStateUpdate CreateInstallBusyState(
        bool inProgress,
        bool currentAppUpdateInProgress,
        ShellInstallSelectionState currentSelectionState,
        ShellInstallSelectionState? restoreSelectionState = null,
        string operationOverlayMessage = "")
    {
        ArgumentNullException.ThrowIfNull(currentSelectionState);

        // Restore snapshot is optional and is only used to avoid stale running
        // button presentation when install busy is cleared.
        var selectionState = BuildInstallSelectionState(
            inProgress,
            currentAppUpdateInProgress,
            currentSelectionState,
            restoreSelectionState);

        return new MainViewModelBusyStateUpdate
        {
            IsAppUpdateInProgress = currentAppUpdateInProgress,
            IsInstallExecutionInProgress = inProgress,
            IsOperationOverlayVisible = inProgress,
            OperationOverlayMessage = inProgress ? operationOverlayMessage : "",
            SelectionState = selectionState,
            ShouldRefreshInstallCommand = true,
            ShouldApplySelectedGameActionRunningState = true
        };
    }

    private static ShellInstallSelectionState BuildInstallSelectionState(
        bool inProgress,
        bool currentAppUpdateInProgress,
        ShellInstallSelectionState currentSelectionState,
        ShellInstallSelectionState? restoreSelectionState)
    {
        if (inProgress)
        {
            return currentSelectionState with
            {
                AppUpdateInProgress = currentAppUpdateInProgress,
                InstallInProgress = true,
                InstallButtonPresentation = currentSelectionState.RunningInstallButtonPresentation
            };
        }

        var baseSelectionState = restoreSelectionState ?? currentSelectionState;
        return baseSelectionState with
        {
            AppUpdateInProgress = currentAppUpdateInProgress,
            InstallInProgress = false
        };
    }
}
