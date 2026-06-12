namespace OptiClick.Core.Runtime.DeviceIdentity;

public sealed class DeviceIdentityRules
{
    public static readonly DeviceIdentityRules Empty = new();

    public IReadOnlyDictionary<string, string> ManufacturerAliases { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, string> ModelAliases { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
