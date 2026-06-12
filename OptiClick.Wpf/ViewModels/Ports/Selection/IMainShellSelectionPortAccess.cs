using System.Collections.ObjectModel;
using OptiClick.Wpf.Shell.Selection;

namespace OptiClick.Wpf.ViewModels.Ports.Selection;

internal interface IMainShellSelectionPortAccess
{
    ObservableCollection<GameCardViewModel> VisibleCards { get; }
    int VisibleGameCount { get; }
    GameCardViewModel? SelectedGame { get; }
    ShellInstallSelectionState SelectionState { get; set; }
    bool IsInstallExecutionInProgress { get; }
    bool IsAppUpdateInProgress { get; }
    bool SuppressHomeNavigationForAutoSelection { get; set; }
    void SetSelectedGame(GameCardViewModel? selectedGame);
    void ApplyRuntimeCatalogSelectionState(ShellInstallSelectionState selectionState);
    void ApplySelectionBridgeState(ShellInstallSelectionState selectionState);
    void ApplyPrecheckRunningIntermediate();
    long IncrementSelectionVersion();
    long ReadSelectionVersion();
    int ResolveSelectedIndex(GameCardViewModel game);
}
