using System.Windows.Input;
using OptiClick.Wpf.Shell.Navigation;

namespace OptiClick.Wpf.ViewModels.Shell;

public sealed class ShellCommandsViewModel
{
    private readonly RelayCommand _showHomeCommand;
    private readonly RelayCommand _showSupportedGamesWikiCommand;
    private readonly RelayCommand _showScanViewCommand;
    private readonly RelayCommand _showSettingsCommand;

    public ShellCommandsViewModel(
        Action<ShellViewKind> setCurrentView,
        Action showScanView)
    {
        ArgumentNullException.ThrowIfNull(setCurrentView);
        ArgumentNullException.ThrowIfNull(showScanView);

        _showHomeCommand = new RelayCommand(_ => setCurrentView(ShellViewKind.Home));
        _showSupportedGamesWikiCommand = new RelayCommand(_ => setCurrentView(ShellViewKind.SupportedGamesWiki));
        _showScanViewCommand = new RelayCommand(_ => showScanView());
        _showSettingsCommand = new RelayCommand(_ => setCurrentView(ShellViewKind.Settings));
    }

    public ICommand ShowHomeCommand => _showHomeCommand;

    public ICommand ShowSupportedGamesWikiViewCommand => _showSupportedGamesWikiCommand;

    public ICommand ShowScanViewCommand => _showScanViewCommand;

    public ICommand ShowSettingsCommand => _showSettingsCommand;

    public void RefreshNavigationCommandStates()
    {
        _showHomeCommand.RaiseCanExecuteChanged();
        _showSupportedGamesWikiCommand.RaiseCanExecuteChanged();
        _showScanViewCommand.RaiseCanExecuteChanged();
        _showSettingsCommand.RaiseCanExecuteChanged();
    }
}
