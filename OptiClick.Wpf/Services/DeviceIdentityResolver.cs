using OptiClick.Core.Runtime;

namespace OptiClick.Wpf.Services;

public sealed class DeviceIdentityResolver : IDeviceIdentityResolver
{
    private const string HideMarker = "__HIDE__";
    private readonly IDeviceIdentityRulesProvider _rulesProvider;

    public DeviceIdentityResolver(IDeviceIdentityRulesProvider rulesProvider)
    {
        _rulesProvider = rulesProvider ?? throw new ArgumentNullException(nameof(rulesProvider));
    }

    public DeviceInfo Resolve(DeviceInfo rawDeviceInfo)
    {
        if (rawDeviceInfo is null)
        {
            return new DeviceInfo();
        }

        var manufacturer = (rawDeviceInfo.Manufacturer ?? "").Trim();
        var model = (rawDeviceInfo.Model ?? "").Trim();
        var deviceName = (rawDeviceInfo.DeviceName ?? "").Trim();

        var rules = _rulesProvider.Current;
        manufacturer = ResolveValue(rules.ManufacturerAliases, manufacturer);
        model = ResolveModel(rules.ModelAliases, model);

        return new DeviceInfo
        {
            Manufacturer = manufacturer,
            Model = model,
            DeviceName = deviceName
        };
    }

    private static string ResolveModel(IReadOnlyDictionary<string, string> aliases, string value)
    {
        var resolved = ResolveValue(aliases, value);
        return string.Equals(resolved, HideMarker, StringComparison.OrdinalIgnoreCase)
            ? ""
            : resolved;
    }

    private static string ResolveValue(IReadOnlyDictionary<string, string> aliases, string value)
    {
        if (string.IsNullOrWhiteSpace(value) || aliases.Count == 0)
        {
            return value;
        }

        if (aliases.TryGetValue(value, out var resolved))
        {
            return (resolved ?? "").Trim();
        }

        return value;
    }
}
