using System.Windows;
using OptiClick.Wpf.Shell.Startup;

namespace OptiClick.Wpf.ViewModels.Shell;

public sealed class StartupOverlayViewModel : ViewModelBase
{
    private bool _isStartupPreparationOverlayVisible;
    private StartupPreparationState _startupPreparationState = StartupPreparationState.Empty;

    public StartupPreparationState StartupPreparationState
    {
        get => _startupPreparationState;
        private set => SetProperty(ref _startupPreparationState, value);
    }

    public Visibility StartupPreparationOverlayVisibility =>
        IsStartupPreparationOverlayVisible ? Visibility.Visible : Visibility.Collapsed;

    public bool IsStartupPreparationOverlayVisible
    {
        get => _isStartupPreparationOverlayVisible;
        private set
        {
            if (SetProperty(ref _isStartupPreparationOverlayVisible, value))
            {
                OnPropertyChanged(nameof(StartupPreparationOverlayVisibility));
            }
        }
    }

    public void ApplyStartupPreparationOverlay(bool isVisible)
    {
        IsStartupPreparationOverlayVisible = isVisible;
    }

    public void ApplyPreparationState(StartupPreparationState state)
    {
        StartupPreparationState = state;
    }
}
