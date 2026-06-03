namespace OptiClick.Wpf.Shell.Navigation;

public sealed class ShellNavigationState
{
    public ShellViewKind CurrentView { get; private set; } = ShellViewKind.Home;

    public bool SetCurrentView(ShellViewKind view)
    {
        if (CurrentView == view)
        {
            return false;
        }

        CurrentView = view;
        return true;
    }
}
