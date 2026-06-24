namespace OptiClick.Core.Runtime;

public sealed record RuntimeHardwareDetectionInfo
{
    public string DeviceInfoSource { get; init; } = "";
    public string GpuInfoSource { get; init; } = "";
    public string WmiDeviceStatus { get; init; } = "";
    public string WmiGpuStatus { get; init; } = "";
    public string WmiGpuErrorType { get; init; } = "";
    public int WmiDeviceAttempts { get; init; }
    public int WmiGpuAttempts { get; init; }
    public string DxgiGpuStatus { get; init; } = "";
    public int DxgiGpuCount { get; init; }
    public string GpuDetectionErrorType { get; init; } = "";
}
