using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using OptiClick.Wpf.Shell.Navigation;

namespace OptiClick.Wpf.ViewModels.Shell;

public sealed class ShellCommandsViewModel
{
    private readonly AsyncRelayCommand _showHomeCommand;
    private readonly AsyncRelayCommand _showSupportedGamesWikiCommand;
    private readonly AsyncRelayCommand _showScanViewCommand;
    private readonly AsyncRelayCommand _showOptiScalerCommand;
    private readonly AsyncRelayCommand _showSettingsCommand;

    public ShellCommandsViewModel(
        Func<ShellViewKind, CancellationToken, Task> requestCurrentViewAsync,
        Func<CancellationToken, Task> showScanViewAsync,
        Action<Exception>? onCommandException = null)
    {
        ArgumentNullException.ThrowIfNull(requestCurrentViewAsync);
        ArgumentNullException.ThrowIfNull(showScanViewAsync);

        _showHomeCommand = new AsyncRelayCommand(
            (_, cancellationToken) => requestCurrentViewAsync(ShellViewKind.Home, cancellationToken),
            onException: onCommandException);
        _showSupportedGamesWikiCommand = new AsyncRelayCommand(
            (_, cancellationToken) => requestCurrentViewAsync(ShellViewKind.SupportedGamesWiki, cancellationToken),
            onException: onCommandException);
        _showScanViewCommand = new AsyncRelayCommand(
            (_, cancellationToken) => showScanViewAsync(cancellationToken),
            onException: onCommandException);
        _showOptiScalerCommand = new AsyncRelayCommand(
            (_, cancellationToken) => requestCurrentViewAsync(ShellViewKind.OptiScaler, cancellationToken),
            onException: onCommandException);
        _showSettingsCommand = new AsyncRelayCommand(
            (_, cancellationToken) => requestCurrentViewAsync(ShellViewKind.Settings, cancellationToken),
            onException: onCommandException);
    }

    public ICommand ShowHomeCommand => _showHomeCommand;

    public ICommand ShowSupportedGamesWikiViewCommand => _showSupportedGamesWikiCommand;

    public ICommand ShowScanViewCommand => _showScanViewCommand;

    public ICommand ShowOptiScalerCommand => _showOptiScalerCommand;

    public ICommand ShowSettingsCommand => _showSettingsCommand;

    public void RefreshNavigationCommandStates()
    {
        _showHomeCommand.RaiseCanExecuteChanged();
        _showSupportedGamesWikiCommand.RaiseCanExecuteChanged();
        _showScanViewCommand.RaiseCanExecuteChanged();
        _showOptiScalerCommand.RaiseCanExecuteChanged();
        _showSettingsCommand.RaiseCanExecuteChanged();
    }
}
