using System.Windows;
using OptiClick.Wpf.Composition;
using OptiClick.Wpf.ViewModels;

namespace OptiClickShell;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        var bootstrapper = new AppBootstrapper();
        bootstrapper.Start(this, e, () => base.OnStartup(e));
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (MainWindow?.DataContext is MainViewModel viewModel)
        {
            viewModel.Dispose();
        }

        base.OnExit(e);
    }
}
