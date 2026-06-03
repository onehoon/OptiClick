namespace OptiClick.Core.Runtime;

public sealed record GpuInfo
{
    public string Name { get; init; } = "";
    public string Vendor { get; init; } = "";
    public string AdapterId { get; init; } = "";
    public bool IsPrimary { get; init; }
}
