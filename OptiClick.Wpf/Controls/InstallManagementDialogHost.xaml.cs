using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using OptiClick.Wpf.ViewModels;

namespace OptiClick.Wpf.Controls;

public partial class InstallManagementDialogHost : UserControl
{
    public InstallManagementDialogHost()
    {
        InitializeComponent();
        IsVisibleChanged += OnVisibilityChanged;
    }

    private void Overlay_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not InstallManagementDialogHostViewModel viewModel)
        {
            return;
        }

        if (viewModel.OverlayClickCommand.CanExecute(null))
        {
            viewModel.OverlayClickCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void OnVisibilityChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (!IsVisible)
        {
            return;
        }

        Dispatcher.BeginInvoke(() => PrimaryActionButton.Focus());
    }
}
