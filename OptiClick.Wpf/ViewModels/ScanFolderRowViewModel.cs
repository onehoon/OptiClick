using System.Windows;
using System.Windows.Media;

namespace OptiClick.Wpf.ViewModels;

public sealed class ScanFolderRowViewModel : ViewModelBase
{
    private string _name;
    private string _status;
    private bool _isChecked;
    private bool _canOpen;
    private Brush _statusBackground;

    public ScanFolderRowViewModel(
        string name,
        string path,
        string status,
        bool isChecked,
        bool canOpen,
        bool canRemove,
        Brush statusBackground)
    {
        _name = name;
        Path = path;
        _status = status;
        _isChecked = isChecked;
        _canOpen = canOpen;
        CanRemove = canRemove;
        _statusBackground = statusBackground;
    }

    public string Name
    {
        get => _name;
        private set => SetProperty(ref _name, value);
    }

    public string Path { get; }

    public string Status
    {
        get => _status;
        private set => SetProperty(ref _status, value);
    }

    public bool IsChecked
    {
        get => _isChecked;
        set => SetProperty(ref _isChecked, value);
    }

    public bool CanOpen
    {
        get => _canOpen;
        private set
        {
            if (SetProperty(ref _canOpen, value))
            {
                OnPropertyChanged(nameof(IsMissing));
                OnPropertyChanged(nameof(MissingVisibility));
                OnPropertyChanged(nameof(PathForeground));
            }
        }
    }

    public bool CanRemove { get; }

    public Brush StatusBackground
    {
        get => _statusBackground;
        private set => SetProperty(ref _statusBackground, value);
    }

    public Brush StatusForeground { get; } = new SolidColorBrush(Color.FromRgb(7, 17, 14));
    public bool IsMissing => !CanOpen;
    public Visibility MissingVisibility => IsMissing ? Visibility.Visible : Visibility.Collapsed;
    public Brush PathForeground => IsMissing
        ? new SolidColorBrush(Color.FromRgb(212, 180, 142))
        : new SolidColorBrush(Color.FromRgb(168, 176, 188));

    public void ApplyLocalization(
        string? name,
        string status,
        bool canOpen,
        Brush statusBackground)
    {
        if (!string.IsNullOrWhiteSpace(name))
        {
            Name = name.Trim();
        }

        Status = (status ?? "").Trim();
        CanOpen = canOpen;
        StatusBackground = statusBackground;
    }
}
