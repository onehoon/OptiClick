using OptiClick.Core.Runtime;

namespace OptiClick.Wpf.Shell.Runtime;

public sealed record RuntimeSummaryStateUpdate
{
    public RuntimeContext RuntimeContext { get; init; } = new();
    public string DeviceText { get; init; } = "";
    public string GpuText { get; init; } = "";
    public bool HasSelectedGpu { get; init; }
    public string SelectedGpuVendor { get; init; } = "";
    public string SelectedGpuName { get; init; } = "";
}
