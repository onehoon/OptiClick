using OptiClick.Core.Runtime;

namespace OptiClick.Wpf.Shell.Runtime;

public sealed class RuntimeHeaderPresenter
{
    public RuntimeHeaderPresentation Build(
        DeviceInfo? resolvedDevice,
        GpuInfo? selectedGpu,
        IReadOnlyList<GpuInfo>? gpus,
        RuntimeSummaryStateText text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return new RuntimeHeaderPresentation
        {
            DeviceText = BuildLocalizedDeviceSummary(resolvedDevice, text),
            GpuText = BuildLocalizedGpuSummary(selectedGpu, gpus, text)
        };
    }

    private static string BuildLocalizedDeviceSummary(DeviceInfo? device, RuntimeSummaryStateText text)
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

        return text.RuntimeUnknownDevice;
    }

    private static string BuildLocalizedGpuSummary(
        GpuInfo? selectedGpu,
        IReadOnlyList<GpuInfo>? gpus,
        RuntimeSummaryStateText text)
    {
        if (selectedGpu is not null)
        {
            return LocalizeGpuName(selectedGpu.Name, text);
        }

        if (gpus is null || gpus.Count == 0)
        {
            return text.RuntimeUnknownGpu;
        }

        var primary = gpus.FirstOrDefault(static gpu => gpu.IsPrimary);
        var primaryGpu = primary ?? gpus[0];
        var primaryName = LocalizeGpuName(primaryGpu.Name, text);
        if (gpus.Count == 1)
        {
            return primaryName;
        }

        var secondary = gpus.FirstOrDefault(gpu => !ReferenceEquals(gpu, primaryGpu));
        if (secondary is null)
        {
            return $"{primaryName} {string.Format(System.Globalization.CultureInfo.CurrentCulture, text.RuntimeGpuSummaryMoreSuffix, gpus.Count - 1)}";
        }

        return $"{primaryName} + {LocalizeGpuName(secondary.Name, text)}";
    }

    private static string LocalizeGpuName(string? rawName, RuntimeSummaryStateText text)
    {
        var normalized = (rawName ?? "").Trim();
        if (string.IsNullOrWhiteSpace(normalized) || string.Equals(normalized, "Unknown GPU", StringComparison.OrdinalIgnoreCase))
        {
            return text.RuntimeUnknownGpu;
        }

        return normalized;
    }
}
