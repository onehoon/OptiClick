using OptiClick.Core.Runtime;
using OptiClick.Wpf.Localization;
using OptiClick.Wpf.Shell.RuntimeData;
using OptiClick.Wpf.Shell.Selection;

namespace OptiClick.Wpf.ViewModels.Ports.ShellInteractionContext.AppUpdate;

internal interface IMainAppUpdateInteractionAccess
{
    AppStrings Strings { get; }
    AppLanguage SelectedLanguage { get; }
    RemoteRuntimeData LatestRuntimeData { get; }
    bool IsAppUpdateInProgress { get; }
    bool IsInstallExecutionInProgress { get; }
    ShellInstallSelectionState SelectionState { get; }
    void SetSettingsStatusText(string message);
    void ApplyBusyStateUpdate(MainViewModelBusyStateUpdate update);
    void ApplyStateUpdate(MainViewModelStateUpdate update);
    void ShutdownApplication();
}
