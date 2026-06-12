using OptiClick.Wpf.Shell.Selection;

namespace OptiClick.Wpf.ViewModels.Ports.Install;

internal interface IMainShellInstallPortAccess
{
    bool IsAppUpdateInProgress { get; }
    ShellInstallSelectionState SelectionState { get; }
    string InstallButtonText { get; }
    string PreferredOptiScalerVariant { get; set; }
    string LanguagePreference { get; }
    void ApplyBusyStateUpdate(MainViewModelBusyStateUpdate update);
    void ClearSelectedGameContext();
    string NormalizeOptiScalerVariantPreference(string? preference);
    void ApplyOptiScalerVariantOptions();
}
