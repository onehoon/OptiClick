using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using OptiClick.Wpf.Logging;
using OptiClick.Wpf.ViewModels;

namespace OptiClick.Wpf.Services;

public static class WindowForegroundActivationService
{
    private const int SwShow = 5;

    private static readonly TimeSpan[] ActivationDelays =
    [
        TimeSpan.Zero,
        TimeSpan.FromMilliseconds(150),
        TimeSpan.FromMilliseconds(400)
    ];

    public static void RequestForeground(Window window, IAppLogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(window);

        var safeLogger = logger ?? NullAppLogger.Instance;
        foreach (var delay in ActivationDelays)
        {
            _ = RequestForegroundAfterDelayAsync(window, delay, safeLogger);
        }
    }

    private static async Task RequestForegroundAfterDelayAsync(Window window, TimeSpan delay, IAppLogger logger)
    {
        try
        {
            if (delay > TimeSpan.Zero)
            {
                await Task.Delay(delay).ConfigureAwait(false);
            }

            if (window.Dispatcher.HasShutdownStarted || window.Dispatcher.HasShutdownFinished)
            {
                return;
            }

            await window.Dispatcher.InvokeAsync(() => TryActivateWindow(window));
        }
        catch (Exception ex)
        {
            logger.Warning(MainViewModelLogCategories.App, $"foreground activation failed type={ex.GetType().Name}");
        }
    }

    private static void TryActivateWindow(Window window)
    {
        if (!window.IsVisible)
        {
            window.Show();
        }

        if (window.WindowState == WindowState.Minimized)
        {
            window.WindowState = WindowState.Normal;
        }

        var wasTopmost = window.Topmost;
        window.Activate();
        window.Topmost = true;
        window.Topmost = wasTopmost;
        window.Focus();

        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var handle = new WindowInteropHelper(window).Handle;
        if (handle == IntPtr.Zero)
        {
            return;
        }

        ShowWindow(handle, SwShow);
        BringWindowToTop(handle);
        SetForegroundWindow(handle);
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool BringWindowToTop(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetForegroundWindow(IntPtr hWnd);
}
