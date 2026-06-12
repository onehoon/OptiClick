using OptiClick.Core.Runtime;
using OptiClick.Wpf.Localization;

namespace OptiClick.Wpf.ViewModels.Ports.App;

internal interface IMainShellAppPortAccess
{
    AppStrings Strings { get; }
    AppLanguage SelectedLanguage { get; }
    MainShellOperationLocks OperationLocks { get; }
    void SetSettingsStatusText(string message);
    void SetScanStatusText(string message);
    void ApplyStateUpdate(MainViewModelStateUpdate update);
    void ApplyDeferredStateUpdate(MainViewModelStateUpdate update);
}
