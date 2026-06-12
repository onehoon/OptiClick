using System.Windows.Input;
using OptiClick.Wpf.Shell.Navigation;
using OptiClick.Wpf.ViewModels.Shell;

namespace OptiClick.Wpf.ViewModels.Ports.Ui;

internal interface IMainShellUiPortAccess
{
    StartupOverlayViewModel StartupOverlay { get; }
    ShellViewKind CurrentViewKind { get; }
    ICommand OpenGameSupportRequestCommand { get; }
    bool SupportedGamesHasEntries { get; }
    void SetCurrentView(ShellViewKind view);
    void RebuildSupportedGamesRows();
    void RefreshSupportedGamesAfterLanguageChange();
    void ApplySelectedGameLocalization();
    void StartSupportedGamesWikiRefreshInBackground();
}
