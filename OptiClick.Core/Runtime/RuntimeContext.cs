namespace OptiClick.Core.Runtime;

public sealed record RuntimeContext
{
    public IReadOnlyList<GpuInfo> Gpus { get; init; } = Array.Empty<GpuInfo>();
    public GpuInfo? SelectedGpu { get; init; }
    public DeviceInfo Device { get; init; } = new();
    public AppLanguage Language { get; init; } = AppLanguage.English;
    public RemoteDataOptions RemoteData { get; init; } = new();
    public RuntimeHardwareDetectionInfo HardwareDetection { get; init; } = new();
}
