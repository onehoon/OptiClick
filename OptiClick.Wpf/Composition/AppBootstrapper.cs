using System.Windows;
using OptiClick.Infrastructure.Windows;
using OptiClick.Wpf.Logging;
using OptiClick.Wpf.Services;
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

        var root = new AppCompositionRoot();
        ThemeService.Initialize(root.CreateAppUserSettingsStore());
        var startupLogger = root.CreateAppLogger();
        startupLogger.Info(MainViewModelLogCategories.App, "milestone app_process_started");
        startupLogger.Info(MainViewModelLogCategories.App, "milestone app_bootstrapper_started");
        var isElevatedRelaunchChild = startupEventArgs.Args.Any(arg => string.Equals(arg, ProcessElevationService.ElevatedRelaunchArgument, StringComparison.OrdinalIgnoreCase));
        var shouldRequestForegroundAfterUpdate = AppUpdateStartupArguments.RequestsForegroundAfterUpdate(startupEventArgs.Args);
        if (isElevatedRelaunchChild)
        {
            startupLogger.Info(MainViewModelLogCategories.App, "milestone elevation_relaunch_child_started");
        }

        if (shouldRequestForegroundAfterUpdate)
        {
            startupLogger.Info(MainViewModelLogCategories.App, "milestone update_foreground_request_detected");
        }

        if (root.ShouldRelaunchElevated(startupEventArgs.Args))
        {
            startupLogger.Info(MainViewModelLogCategories.App, "milestone elevation_relaunch_requested");
            var relaunched = root.TryRelaunchAsAdministrator(startupEventArgs.Args);
            if (relaunched)
            {
                startupLogger.Info(MainViewModelLogCategories.App, "milestone elevation_relaunch_launched exiting_bootstrap_process");
                app.Shutdown(0);
                return;
            }

            startupLogger.Info(MainViewModelLogCategories.App, "milestone elevation_relaunch_canceled_or_failed shutting_down");
            app.Shutdown(1);
            return;
        }

        if (!root.IsCurrentProcessElevated())
        {
            startupLogger.Info(MainViewModelLogCategories.App, "milestone elevation_required_unavailable shutting_down");
            app.Shutdown(1);
            return;
        }

        continueStartup();

        var mainWindow = root.CreateMainWindow();
        startupLogger.Info(MainViewModelLogCategories.App, "milestone main_window_created");
        app.MainWindow = mainWindow;
        mainWindow.Show();
        startupLogger.Info(MainViewModelLogCategories.App, "milestone main_window_shown");
        StartWindowsTimeSyncRepairInBackground();
        if (shouldRequestForegroundAfterUpdate)
        {
            WindowForegroundActivationService.RequestForeground(mainWindow, startupLogger);
        }

        if (mainWindow.DataContext is MainViewModel viewModel)
        {
            var startupCancellationTokenSource = new CancellationTokenSource();
            ExitEventHandler cancelStartupOnExit = (_, _) => startupCancellationTokenSource.Cancel();
            app.Exit += cancelStartupOnExit;

            _ = mainWindow.Dispatcher.InvokeAsync(async () =>
            {
                try
                {
                    await RunStartupSequenceAsync(
                        viewModel,
                        startupLogger,
                        startupCancellationTokenSource.Token);
                }
                finally
                {
                    app.Exit -= cancelStartupOnExit;
                    startupCancellationTokenSource.Dispose();
                }
            });
        }
    }

    private static void StartWindowsTimeSyncRepairInBackground()
    {
        ThreadPool.QueueUserWorkItem(static _ =>
        {
            _ = new WindowsTimeSyncRepairService().TryRepairAsync();
        });
    }

    private static async Task RunStartupSequenceAsync(
        MainViewModel viewModel,
        IAppLogger startupLogger,
        CancellationToken cancellationToken)
    {
        try
        {
            if (await viewModel.ShowStartupOperatingSystemBlockIfNeededAsync(cancellationToken))
            {
                Application.Current?.Shutdown();
                return;
            }

            await viewModel.InitializeAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            startupLogger.Info(MainViewModelLogCategories.App, "startup sequence canceled");
        }
        catch (Exception ex)
        {
            startupLogger.Error(MainViewModelLogCategories.App, "startup sequence failed", ex);
        }
    }
}
