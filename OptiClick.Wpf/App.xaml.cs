using System.Windows;
using OptiClick.Wpf.Composition;
using OptiClick.Wpf.ViewModels;
using Velopack;

namespace OptiClickShell;

public partial class App : Application
{
    public App()
    {
        VelopackApp.Build().Run();
    }

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
