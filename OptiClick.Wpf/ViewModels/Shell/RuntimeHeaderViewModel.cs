using System.Windows;
using OptiClick.Wpf.Shell.Runtime;

namespace OptiClick.Wpf.ViewModels.Shell;

public sealed class RuntimeHeaderViewModel : ViewModelBase
{
    private string _deviceText = "";
    private string _gpuText = "";
    private string _gpuLogoSource = "";
    private double _gpuLogoWidth;
    private double _gpuLogoHeight;
    private Thickness _gpuLogoMargin = new(0);

    public string DeviceText
    {
        get => _deviceText;
        private set
        {
            if (SetProperty(ref _deviceText, value))
            {
                OnPropertyChanged(nameof(DeviceGpuInlineText));
            }
        }
    }

    public string GpuText
    {
        get => _gpuText;
        private set
        {
            if (SetProperty(ref _gpuText, value))
            {
                OnPropertyChanged(nameof(DeviceGpuInlineText));
            }
        }
    }

    public string DeviceGpuInlineText
    {
        get
        {
            var device = (DeviceText ?? "").Trim();
            var gpu = (GpuText ?? "").Trim();

            if (string.IsNullOrWhiteSpace(device) && string.IsNullOrWhiteSpace(gpu))
            {
                return "";
            }

            if (string.IsNullOrWhiteSpace(device))
            {
                return gpu;
            }

            if (string.IsNullOrWhiteSpace(gpu))
            {
                return device;
            }

            return $"{device} | {gpu}";
        }
    }

    public string GpuLogoSource
    {
        get => _gpuLogoSource;
        private set
        {
            if (SetProperty(ref _gpuLogoSource, value))
            {
                OnPropertyChanged(nameof(GpuLogoVisibility));
            }
        }
    }

    public Visibility GpuLogoVisibility =>
        string.IsNullOrWhiteSpace(GpuLogoSource) ? Visibility.Collapsed : Visibility.Visible;

    public double GpuLogoWidth
    {
        get => _gpuLogoWidth;
        private set => SetProperty(ref _gpuLogoWidth, value);
    }

    public double GpuLogoHeight
    {
        get => _gpuLogoHeight;
        private set => SetProperty(ref _gpuLogoHeight, value);
    }

    public Thickness GpuLogoMargin
    {
        get => _gpuLogoMargin;
        private set => SetProperty(ref _gpuLogoMargin, value);
    }

    public void ApplyText(string deviceText, string gpuText)
    {
        DeviceText = deviceText;
        GpuText = gpuText;
    }

    public void ApplyTextUpdate(string deviceText, string gpuText)
    {
        if (string.IsNullOrWhiteSpace(deviceText) && string.IsNullOrWhiteSpace(gpuText))
        {
            return;
        }

        ApplyText(
            string.IsNullOrWhiteSpace(deviceText) ? DeviceText : deviceText,
            string.IsNullOrWhiteSpace(gpuText) ? GpuText : gpuText);
    }

    public void Apply(RuntimeSummaryStateUpdate update)
    {
        ArgumentNullException.ThrowIfNull(update);

        DeviceText = update.DeviceText;
        GpuText = update.GpuText;
        GpuLogoSource = update.GpuLogoSource;
        GpuLogoWidth = update.GpuLogoWidth;
        GpuLogoHeight = update.GpuLogoHeight;
        GpuLogoMargin = update.GpuLogoMargin;
    }
}
