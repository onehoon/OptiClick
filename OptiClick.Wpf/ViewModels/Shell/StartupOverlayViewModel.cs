using System.Windows;
using OptiClick.Wpf.Shell.Startup;

namespace OptiClick.Wpf.ViewModels.Shell;

public sealed class StartupOverlayViewModel : ViewModelBase
{
    private bool _isFirstRunPreparationOverlayVisible;
    private StartupPreparationState _startupPreparationState = StartupPreparationState.Empty;

    public StartupPreparationState StartupPreparationState
    {
        get => _startupPreparationState;
        private set => SetProperty(ref _startupPreparationState, value);
    }

    public Visibility FirstRunPreparationOverlayVisibility =>
        IsFirstRunPreparationOverlayVisible ? Visibility.Visible : Visibility.Collapsed;

    public bool IsFirstRunPreparationOverlayVisible
    {
        get => _isFirstRunPreparationOverlayVisible;
        private set
        {
            if (SetProperty(ref _isFirstRunPreparationOverlayVisible, value))
            {
                OnPropertyChanged(nameof(FirstRunPreparationOverlayVisibility));
            }
        }
    }

    public void ApplyFirstRunPreparationOverlay(bool isVisible)
    {
        IsFirstRunPreparationOverlayVisible = isVisible;
    }

    public void ApplyPreparationState(StartupPreparationState state)
    {
        StartupPreparationState = state;
    }
}
