using OptiClick.Wpf.Shell.Navigation;

namespace OptiClick.Wpf.ViewModels.Shell;

public sealed class ShellNavigationViewModel : ViewModelBase
{
    private readonly ShellNavigationState _navigationState;

    public ShellNavigationViewModel(ShellNavigationState navigationState)
    {
        _navigationState = navigationState ?? throw new ArgumentNullException(nameof(navigationState));
    }

    public ShellViewKind CurrentViewKind => _navigationState.CurrentView;

    public bool IsHomeViewActive => CurrentViewKind == ShellViewKind.Home;

    public bool IsSupportedGamesWikiViewActive => CurrentViewKind == ShellViewKind.SupportedGamesWiki;

    public bool IsScanViewActive => CurrentViewKind == ShellViewKind.Scan;

    public bool IsSettingsViewActive => CurrentViewKind == ShellViewKind.Settings;

    public void Refresh()
    {
        OnPropertyChanged(nameof(CurrentViewKind));
        OnPropertyChanged(nameof(IsHomeViewActive));
        OnPropertyChanged(nameof(IsSupportedGamesWikiViewActive));
        OnPropertyChanged(nameof(IsScanViewActive));
        OnPropertyChanged(nameof(IsSettingsViewActive));
    }
}
