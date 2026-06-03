using System.Windows;

namespace OptiClick.Wpf.ViewModels.Shell;

public sealed class ShellBusyStateViewModel : ViewModelBase
{
    private bool _isOperationOverlayVisible;
    private string _operationOverlayMessage = "";

    public Visibility OperationOverlayVisibility =>
        IsOperationOverlayVisible ? Visibility.Visible : Visibility.Collapsed;

    public bool IsOperationOverlayVisible
    {
        get => _isOperationOverlayVisible;
        private set
        {
            if (SetProperty(ref _isOperationOverlayVisible, value))
            {
                OnPropertyChanged(nameof(OperationOverlayVisibility));
            }
        }
    }

    public string OperationOverlayMessage
    {
        get => _operationOverlayMessage;
        private set => SetProperty(ref _operationOverlayMessage, value);
    }

    public void Apply(bool isVisible, string message)
    {
        IsOperationOverlayVisible = isVisible;
        OperationOverlayMessage = message ?? "";
    }
}
