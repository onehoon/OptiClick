using System;
using System.Windows;
using System.Windows.Input;
using OptiClick.Wpf.Logging;
using OptiClick.Wpf.ViewModels;
using Wpf.Ui.Controls;

namespace OptiClickShell;

public partial class MainWindow : FluentWindow
{
    private readonly IAppLogger _logger;
    private bool _loggedFirstRender;

    public MainWindow(MainViewModel viewModel, IAppLogger? logger = null)
    {
        _logger = logger ?? NullAppLogger.Instance;
        InitializeComponent();
        DataContext = viewModel;
        ContentRendered += MainWindow_OnContentRendered;
        UpdateWindowButtonGlyph();
    }

    private void MainWindow_OnContentRendered(object? sender, EventArgs e)
    {
        if (_loggedFirstRender)
        {
            return;
        }

        _loggedFirstRender = true;
        _logger.Info(MainViewModelLogCategories.App, "milestone first_render_completed");
    }

    private void Window_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape || DataContext is not MainViewModel viewModel)
        {
            return;
        }

        if (viewModel.InstallManagementDialogHost.HandleEscapeKey())
        {
            e.Handled = true;
            return;
        }

        if (viewModel.DialogHost.HandleEscapeKey())
        {
            e.Handled = true;
        }
    }

    private void TopBar_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            ToggleMaximizeRestore();
            return;
        }

        DragMove();
    }

    private void MinimizeButton_OnClick(object sender, RoutedEventArgs e)
    {
        SystemCommands.MinimizeWindow(this);
    }

    private void MaximizeRestoreButton_OnClick(object sender, RoutedEventArgs e)
    {
        ToggleMaximizeRestore();
    }

    private void CloseButton_OnClick(object sender, RoutedEventArgs e)
    {
        SystemCommands.CloseWindow(this);
    }

    private void Window_OnStateChanged(object sender, EventArgs e)
    {
        UpdateWindowButtonGlyph();
    }

    private void ToggleMaximizeRestore()
    {
        if (ResizeMode == ResizeMode.NoResize)
        {
            return;
        }

        if (WindowState == WindowState.Maximized)
        {
            SystemCommands.RestoreWindow(this);
        }
        else
        {
            SystemCommands.MaximizeWindow(this);
        }
    }

    private void UpdateWindowButtonGlyph()
    {
        if (MaximizeRestoreGlyph is not System.Windows.Controls.TextBlock maximizeRestoreGlyph)
        {
            return;
        }

        maximizeRestoreGlyph.Text = WindowState == WindowState.Maximized ? "\uE923" : "\uE922";
    }
}
