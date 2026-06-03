namespace OptiClick.Core.Runtime;

public sealed record DeviceInfo
{
    public string Manufacturer { get; init; } = "";
    public string Model { get; init; } = "";
    public string DeviceName { get; init; } = "";
}
