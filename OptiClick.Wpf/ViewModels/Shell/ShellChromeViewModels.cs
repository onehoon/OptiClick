using OptiClick.Wpf.Shell.Navigation;

namespace OptiClick.Wpf.ViewModels.Shell;

public sealed record ShellChromeViewModels(
    ShellNavigationState NavigationState,
    ShellNavigationViewModel Navigation,
    RuntimeHeaderViewModel RuntimeHeader,
    StartupOverlayViewModel StartupOverlay,
    ShellBusyStateViewModel ShellBusyState)
{
    public static ShellChromeViewModels Create(ShellNavigationState navigationState)
    {
        ArgumentNullException.ThrowIfNull(navigationState);

        return new ShellChromeViewModels(
            navigationState,
            new ShellNavigationViewModel(navigationState),
            new RuntimeHeaderViewModel(),
            new StartupOverlayViewModel(),
            new ShellBusyStateViewModel());
    }
}
