namespace OptiClick.Core.Runtime;

public static class RuntimeSummaryFormatter
{
    public static string BuildDeviceSummary(DeviceInfo? device)
    {
        var manufacturer = (device?.Manufacturer ?? "").Trim();
        var model = (device?.Model ?? "").Trim();
        var deviceName = (device?.DeviceName ?? "").Trim();

        if (!string.IsNullOrWhiteSpace(manufacturer) && !string.IsNullOrWhiteSpace(model))
        {
            return $"{manufacturer} {model}";
        }

        if (!string.IsNullOrWhiteSpace(deviceName))
        {
            return deviceName;
        }

        if (!string.IsNullOrWhiteSpace(model))
        {
            return model;
        }

        return "Unknown device";
    }

    public static string BuildGpuSummary(IReadOnlyList<GpuInfo>? gpus)
    {
        if (gpus is null || gpus.Count == 0)
        {
            return "Unknown GPU";
        }

        var primary = gpus.FirstOrDefault(static gpu => gpu.IsPrimary);
        var primaryGpu = primary ?? gpus[0];
        var primaryName = GetGpuName(primaryGpu);
        if (gpus.Count == 1)
        {
            return primaryName;
        }

        var secondary = gpus.FirstOrDefault(gpu => !ReferenceEquals(gpu, primaryGpu));
        if (secondary is null)
        {
            return $"{primaryName} + {gpus.Count - 1} more";
        }

        return $"{primaryName} + {GetGpuName(secondary)}";
    }

    public static string BuildGpuSummary(GpuInfo? selectedGpu, IReadOnlyList<GpuInfo>? gpus)
    {
        if (selectedGpu is not null)
        {
            return GetGpuName(selectedGpu);
        }

        return BuildGpuSummary(gpus);
    }

    private static string GetGpuName(GpuInfo? gpu)
    {
        var name = (gpu?.Name ?? "").Trim();
        return string.IsNullOrWhiteSpace(name) ? "Unknown GPU" : name;
    }
}
