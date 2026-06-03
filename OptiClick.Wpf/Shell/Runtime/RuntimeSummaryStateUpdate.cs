using System.Windows;
using OptiClick.Core.Runtime;

namespace OptiClick.Wpf.Shell.Runtime;

public sealed record RuntimeSummaryStateUpdate
{
    public RuntimeContext RuntimeContext { get; init; } = new();
    public string DeviceText { get; init; } = "";
    public string GpuText { get; init; } = "";
    public string GpuLogoSource { get; init; } = "";
    public double GpuLogoWidth { get; init; }
    public double GpuLogoHeight { get; init; }
    public Thickness GpuLogoMargin { get; init; } = new(0);
    public bool HasSelectedGpu { get; init; }
    public string SelectedGpuVendor { get; init; } = "";
    public string SelectedGpuName { get; init; } = "";
}
