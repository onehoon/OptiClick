using System.Windows;

namespace OptiClick.Wpf.Shell.Runtime;

public sealed record RuntimeHeaderPresentation
{
    public string DeviceText { get; init; } = "";
    public string GpuText { get; init; } = "";
    public string GpuLogoSource { get; init; } = "";
    public double GpuLogoWidth { get; init; }
    public double GpuLogoHeight { get; init; }
    public Thickness GpuLogoMargin { get; init; } = new(0);
}
