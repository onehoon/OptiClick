using OptiClick.Core.Abstractions;
using OptiClick.Core.Runtime;

namespace OptiClick.Wpf.Services;

public sealed class RuntimeTestEnvironmentOverrideProviderFactory
{
    public const string GpuDualEnvName = "OPTICLICK_TEST_GPU_DUAL";
    public const string GpuNamesEnvName = "OPTICLICK_TEST_GPU_NAMES";
    public const string DeviceInfoEnvName = "OPTICLICK_TEST_DEVICE_INFO";
    public const string DeviceManufacturerEnvName = "OPTICLICK_TEST_DEVICE_MANUFACTURER";
    public const string DeviceModelEnvName = "OPTICLICK_TEST_DEVICE_MODEL";

    private readonly Func<string, string?> _environmentReader;

    public RuntimeTestEnvironmentOverrideProviderFactory(Func<string, string?>? environmentReader = null)
    {
        _environmentReader = environmentReader ?? Environment.GetEnvironmentVariable;
    }

    public IGpuInfoProvider ResolveGpuProvider(IGpuInfoProvider fallbackProvider)
    {
        ArgumentNullException.ThrowIfNull(fallbackProvider);

        if (!IsEnabled(_environmentReader(GpuDualEnvName)))
        {
            return fallbackProvider;
        }

        var parsedGpuNames = ParseGpuNames(_environmentReader(GpuNamesEnvName));
        if (parsedGpuNames.Count < 1)
        {
            return fallbackProvider;
        }

        var gpus = new List<GpuInfo>(parsedGpuNames.Count);
        for (var i = 0; i < parsedGpuNames.Count; i++)
        {
            gpus.Add(BuildGpuInfo(parsedGpuNames[i], i == 0, i));
        }

        return new MockGpuInfoProvider(gpus);
    }

    public IDeviceInfoProvider ResolveDeviceProvider(IDeviceInfoProvider fallbackProvider)
    {
        ArgumentNullException.ThrowIfNull(fallbackProvider);

        if (!IsEnabled(_environmentReader(DeviceInfoEnvName)))
        {
            return fallbackProvider;
        }

        var manufacturer = (_environmentReader(DeviceManufacturerEnvName) ?? "").Trim();
        var model = (_environmentReader(DeviceModelEnvName) ?? "").Trim();
        var deviceName = string.IsNullOrWhiteSpace(model) ? (Environment.MachineName ?? "") : model;

        return new MockDeviceInfoProvider(
            new DeviceInfo
            {
                Manufacturer = manufacturer,
                Model = model,
                DeviceName = deviceName
            });
    }

    private static GpuInfo BuildGpuInfo(string name, bool isPrimary, int index)
    {
        return new GpuInfo
        {
            Name = name,
            Vendor = DetectVendor(name),
            AdapterId = $"TEST-GPU-{index + 1}",
            IsPrimary = isPrimary
        };
    }

    private static IReadOnlyList<string> ParseGpuNames(string? rawGpuNames)
    {
        return (rawGpuNames ?? "")
            .Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .ToArray();
    }

    private static bool IsEnabled(string? rawValue)
    {
        var normalized = (rawValue ?? "").Trim();
        if (string.Equals(normalized, "1", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (bool.TryParse(normalized, out var parsed))
        {
            return parsed;
        }

        return false;
    }

    private static string DetectVendor(string gpuName)
    {
        var normalized = (gpuName ?? "").Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return "Unknown";
        }

        if (ContainsAny(normalized, ["NVIDIA", "GeForce", "RTX"]))
        {
            return "NVIDIA";
        }

        if (ContainsAny(normalized, ["AMD", "Radeon"]))
        {
            return "AMD";
        }

        if (ContainsAny(normalized, ["Intel", "Arc"]))
        {
            return "Intel";
        }

        return "Unknown";
    }

    private static bool ContainsAny(string source, IEnumerable<string> keywords)
    {
        foreach (var keyword in keywords)
        {
            if (source.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
