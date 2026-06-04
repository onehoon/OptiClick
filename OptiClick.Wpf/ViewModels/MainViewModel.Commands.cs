using OptiClick.Wpf.ViewModels.Shell;

namespace OptiClick.Wpf.ViewModels;

public sealed partial class MainViewModel
{
    private RelayCommand _openGameSupportRequestCommand = null!;

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
