using System.Windows.Input;
using OptiClick.Wpf.ViewModels.Shell;

namespace OptiClick.Wpf.ViewModels;

public sealed partial class MainViewModel
{
    private RelayCommand _openGameSupportRequestCommand = null!;

    public ICommand SelectGameCommand => Home.SelectGameCommand;
    public ICommand ShowHomeCommand => Commands.ShowHomeCommand;
    public ICommand ShowSupportedGamesWikiViewCommand => Commands.ShowSupportedGamesWikiViewCommand;
    public ICommand ShowScanViewCommand => Commands.ShowScanViewCommand;
    public ICommand ShowSettingsCommand => Commands.ShowSettingsCommand;
    public ICommand AddScanFolderCommand => Scan.AddScanFolderCommand;
    public ICommand RemoveScanFolderCommand => Scan.RemoveScanFolderCommand;
    public ICommand OpenScanFolderCommand => Scan.OpenScanFolderCommand;
    public ICommand SaveAndScanCommand => Scan.SaveAndScanCommand;
    public ICommand OpenLogFolderCommand => Settings.OpenLogFolderCommand;
    public ICommand OpenSupportRequestCommand => Settings.OpenSupportRequestCommand;
    public ICommand OpenGameSupportRequestCommand => _openGameSupportRequestCommand;
    public ICommand RefreshInstallFilesCommand => Settings.RefreshInstallFilesCommand;
    public ICommand ShowDetailsCommand => Home.ShowDetailsCommand;
    public ICommand ShowInstallCommand => Home.ShowInstallCommand;

    private void InitializeCommandSet()
    {
        Commands = new ShellCommandsViewModel(SetCurrentView, ShowScanView);
        _openGameSupportRequestCommand = new RelayCommand(_ => OpenGameSupportRequest());
    }

    private void RefreshNavigationAndScanCommandStates()
    {
        Commands?.RefreshNavigationCommandStates();
        Scan?.RefreshCommandStates();
        Home?.RefreshCommandStates();
    }
}
