using OptiClick.Core.Runtime;
using OptiClick.Wpf.Localization;
using OptiClick.Wpf.Services;

namespace OptiClick.Wpf.Shell.Runtime;

public sealed class RuntimeHeaderPresenter
{
    private readonly IGpuVendorLogoResolver _gpuVendorLogoResolver;

    public RuntimeHeaderPresenter(IGpuVendorLogoResolver gpuVendorLogoResolver)
    {
        _gpuVendorLogoResolver = gpuVendorLogoResolver;
    }

    public RuntimeHeaderPresentation Build(
        DeviceInfo? resolvedDevice,
        GpuInfo? selectedGpu,
        IReadOnlyList<GpuInfo>? gpus,
        AppStrings strings)
    {
        var logoPresentation = _gpuVendorLogoResolver.ResolvePresentation(selectedGpu);
        return new RuntimeHeaderPresentation
        {
            DeviceText = BuildLocalizedDeviceSummary(resolvedDevice, strings),
            GpuText = BuildLocalizedGpuSummary(selectedGpu, gpus, strings),
            GpuLogoSource = logoPresentation.Source,
            GpuLogoWidth = logoPresentation.Width,
            GpuLogoHeight = logoPresentation.Height,
            GpuLogoMargin = logoPresentation.Margin
        };
    }

    private static string BuildLocalizedDeviceSummary(DeviceInfo? device, AppStrings strings)
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

        return strings.RuntimeUnknownDevice;
    }

    private static string BuildLocalizedGpuSummary(
        GpuInfo? selectedGpu,
        IReadOnlyList<GpuInfo>? gpus,
        AppStrings strings)
    {
        if (selectedGpu is not null)
        {
            return LocalizeGpuName(selectedGpu.Name, strings);
        }

        if (gpus is null || gpus.Count == 0)
        {
            return strings.RuntimeUnknownGpu;
        }

        var primary = gpus.FirstOrDefault(static gpu => gpu.IsPrimary);
        var primaryGpu = primary ?? gpus[0];
        var primaryName = LocalizeGpuName(primaryGpu.Name, strings);
        if (gpus.Count == 1)
        {
            return primaryName;
        }

        var secondary = gpus.FirstOrDefault(gpu => !ReferenceEquals(gpu, primaryGpu));
        if (secondary is null)
        {
            return $"{primaryName} {string.Format(System.Globalization.CultureInfo.CurrentCulture, strings.RuntimeGpuSummaryMoreSuffix, gpus.Count - 1)}";
        }

        return $"{primaryName} + {LocalizeGpuName(secondary.Name, strings)}";
    }

    private static string LocalizeGpuName(string? rawName, AppStrings strings)
    {
        var normalized = (rawName ?? "").Trim();
        if (string.IsNullOrWhiteSpace(normalized) || string.Equals(normalized, "Unknown GPU", StringComparison.OrdinalIgnoreCase))
        {
            return strings.RuntimeUnknownGpu;
        }

        return normalized;
    }
}
