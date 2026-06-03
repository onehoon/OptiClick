namespace OptiClick.Core.Models;

public enum GpuVendor
{
    Unknown,
    Intel,
    Amd,
    Nvidia
}

public sealed record GpuContext
{
    public GpuVendor Vendor { get; init; } = GpuVendor.Unknown;
    public string RawName { get; init; } = "";
    public string ModelName { get; init; } = "";
    public bool IsSupported { get; init; }
}
