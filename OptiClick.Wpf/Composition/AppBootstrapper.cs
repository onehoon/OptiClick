using System.Windows;
using OptiClick.Wpf.Logging;
using OptiClick.Wpf.ViewModels;

namespace OptiClick.Wpf.Composition;

public sealed class AppBootstrapper
{
    public void Start(Application app, StartupEventArgs startupEventArgs, Action continueStartup)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(startupEventArgs);
        ArgumentNullException.ThrowIfNull(continueStartup);

        if (startupEventArgs.Args.Contains("--smoke-test", StringComparer.OrdinalIgnoreCase))
        {
            app.Shutdown(0);
            return;
        }

        if (startupEventArgs.Args.Contains("--smoke-remote-data-contract", StringComparer.OrdinalIgnoreCase))
        {
            var compositionRoot = new AppCompositionRoot();
            var runner = compositionRoot.CreateRemoteDataContractSmokeRunner();
            var result = runner.RunAsync().GetAwaiter().GetResult();
            foreach (var line in result.OutputLines)
            {
                Console.WriteLine(line);
            }

            app.Shutdown(result.ExitCode);
            return;
        }

        var root = new AppCompositionRoot();
        var startupLogger = root.CreateAppLogger();
        startupLogger.Info(MainViewModelLogCategories.App, "milestone app_process_started");
        startupLogger.Info(MainViewModelLogCategories.App, "milestone app_bootstrapper_started");
        var elevationRelaunchCanceledOrFailed = false;
        if (root.ShouldRelaunchElevated(startupEventArgs.Args))
        {
            var relaunched = root.TryRelaunchAsAdministrator(startupEventArgs.Args);
            if (relaunched)
            {
                app.Shutdown(0);
                return;
            }

            elevationRelaunchCanceledOrFailed = true;
        }

        continueStartup();

        var mainWindow = root.CreateMainWindow();
        startupLogger.Info(MainViewModelLogCategories.App, "milestone main_window_created");
        app.MainWindow = mainWindow;
        mainWindow.Show();
        startupLogger.Info(MainViewModelLogCategories.App, "milestone main_window_shown");

        if (mainWindow.DataContext is MainViewModel viewModel)
        {
            if (elevationRelaunchCanceledOrFailed)
            {
                viewModel.NotifyAdministratorRelaunchCancelled();
            }

            _ = mainWindow.Dispatcher.InvokeAsync(() => RunStartupSequenceAsync(viewModel, startupLogger));
        }
    }

    private static async Task RunStartupSequenceAsync(MainViewModel viewModel, IAppLogger startupLogger)
    {
        try
        {
            if (await viewModel.ShowStartupOperatingSystemBlockIfNeededAsync())
            {
                Application.Current?.Shutdown();
                return;
            }

            await viewModel.InitializeAsync();
        }
        catch (Exception ex)
        {
            startupLogger.Error(MainViewModelLogCategories.App, "startup sequence failed", ex);
        }
    }
}
