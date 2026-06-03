using System.Windows.Input;
using OptiClick.Wpf.Shell.Navigation;

namespace OptiClick.Wpf.ViewModels;

public sealed partial class MainViewModel
{
    private sealed record MainViewModelCommandSet
    {
        public required ICommand SelectGameCommand { get; init; }
        public required ICommand ShowHomeCommand { get; init; }
        public required ICommand ShowSupportedGamesWikiViewCommand { get; init; }
        public required ICommand ShowScanViewCommand { get; init; }
        public required ICommand ShowSettingsCommand { get; init; }
        public required ICommand AddScanFolderCommand { get; init; }
        public required ICommand RemoveScanFolderCommand { get; init; }
        public required ICommand OpenScanFolderCommand { get; init; }
        public required ICommand SaveAndScanCommand { get; init; }
        public required ICommand OpenLogFolderCommand { get; init; }
        public required ICommand OpenSupportRequestCommand { get; init; }
        public required ICommand OpenGameSupportRequestCommand { get; init; }
        public required ICommand RefreshInstallFilesCommand { get; init; }
        public required ICommand ShowDetailsCommand { get; init; }
        public required ICommand ShowInstallCommand { get; init; }
    }

    private MainViewModelCommandSet? _commandSet;
    private RelayCommand _showHomeCommand = null!;
    private RelayCommand _showSupportedGamesWikiCommand = null!;
    private RelayCommand _showSettingsCommand = null!;
    private AsyncRelayCommand _saveAndScanCommand = null!;

    public ICommand SelectGameCommand => _commandSet!.SelectGameCommand;
    public ICommand ShowHomeCommand => _commandSet!.ShowHomeCommand;
    public ICommand ShowSupportedGamesWikiViewCommand => _commandSet!.ShowSupportedGamesWikiViewCommand;
    public ICommand ShowScanViewCommand => _commandSet!.ShowScanViewCommand;
    public ICommand ShowSettingsCommand => _commandSet!.ShowSettingsCommand;
    public ICommand AddScanFolderCommand => _commandSet!.AddScanFolderCommand;
    public ICommand RemoveScanFolderCommand => _commandSet!.RemoveScanFolderCommand;
    public ICommand OpenScanFolderCommand => _commandSet!.OpenScanFolderCommand;
    public ICommand SaveAndScanCommand => _commandSet!.SaveAndScanCommand;
    public ICommand OpenLogFolderCommand => _commandSet!.OpenLogFolderCommand;
    public ICommand OpenSupportRequestCommand => _commandSet!.OpenSupportRequestCommand;
    public ICommand OpenGameSupportRequestCommand => _commandSet!.OpenGameSupportRequestCommand;
    public ICommand RefreshInstallFilesCommand => _commandSet!.RefreshInstallFilesCommand;
    public ICommand ShowDetailsCommand => _commandSet!.ShowDetailsCommand;
    public ICommand ShowInstallCommand => _commandSet!.ShowInstallCommand;

    private void InitializeCommandSet()
    {
        _showHomeCommand = new RelayCommand(
            _ => SetCurrentView(ShellViewKind.Home));
        _showSupportedGamesWikiCommand = new RelayCommand(
            _ => SetCurrentView(ShellViewKind.SupportedGamesWiki));
        _showSettingsCommand = new RelayCommand(
            _ => SetCurrentView(ShellViewKind.Settings));
        _saveAndScanCommand = Scan.SaveAndScanCommand;

        _commandSet = new MainViewModelCommandSet
        {
            SelectGameCommand = Home.SelectGameCommand,
            ShowHomeCommand = _showHomeCommand,
            ShowSupportedGamesWikiViewCommand = _showSupportedGamesWikiCommand,
            ShowScanViewCommand = new RelayCommand(_ => ShowScanView()),
            ShowSettingsCommand = _showSettingsCommand,
            AddScanFolderCommand = Scan.AddScanFolderCommand,
            RemoveScanFolderCommand = Scan.RemoveScanFolderCommand,
            OpenScanFolderCommand = Scan.OpenScanFolderCommand,
            SaveAndScanCommand = _saveAndScanCommand,
            OpenLogFolderCommand = Settings.OpenLogFolderCommand,
            OpenSupportRequestCommand = Settings.OpenSupportRequestCommand,
            OpenGameSupportRequestCommand = new RelayCommand(_ => OpenGameSupportRequest()),
            RefreshInstallFilesCommand = Settings.RefreshInstallFilesCommand,
            ShowDetailsCommand = Home.ShowDetailsCommand,
            ShowInstallCommand = Home.ShowInstallCommand
        };
    }

    private void RefreshNavigationAndScanCommandStates()
    {
        _showHomeCommand?.RaiseCanExecuteChanged();
        _showSupportedGamesWikiCommand?.RaiseCanExecuteChanged();
        _showSettingsCommand?.RaiseCanExecuteChanged();
        Scan?.RefreshCommandStates();
        Home?.RefreshCommandStates();
    }
}
