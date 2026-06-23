namespace OptiClick.Core.Runtime;

public sealed record RuntimeHardwareDetectionInfo
{
    public string DeviceInfoSource { get; init; } = "";
    public string GpuInfoSource { get; init; } = "";
    public string WmiDeviceStatus { get; init; } = "";
    public string WmiGpuStatus { get; init; } = "";
    public int WmiDeviceAttempts { get; init; }
    public int WmiGpuAttempts { get; init; }
}
