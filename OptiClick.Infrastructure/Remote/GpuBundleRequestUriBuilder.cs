using OptiClick.Core.Games.GpuBundle;

namespace OptiClick.Infrastructure.Remote;

public sealed class GpuBundleRequestUriBuilder : IGpuBundleRequestUriBuilder
{
    public Uri? Build(string endpoint, GpuBundleFetchRequest request)
    {
        var normalizedEndpoint = (endpoint ?? "").Trim();
        if (!Uri.TryCreate(normalizedEndpoint, UriKind.Absolute, out var baseUri))
        {
            return null;
        }

        var vendor = (request?.Vendor ?? "").Trim();
        var bundleKey = (request?.BundleKey ?? "").Trim();
        if (string.IsNullOrWhiteSpace(vendor) || string.IsNullOrWhiteSpace(bundleKey))
        {
            return null;
        }

        var queryPairs = new List<(string Key, string Value)>
        {
            ("vendor", vendor),
            ("bundle", bundleKey),
            ("gpu_raw", (request?.GpuRaw ?? "").Trim()),
            ("request_source", (request?.RequestSource ?? "").Trim()),
            ("device_manufacturer", (request?.DeviceManufacturer ?? "").Trim()),
            ("device_model", (request?.DeviceModel ?? "").Trim()),
            ("app_version", (request?.AppVersion ?? "").Trim()),
            ("manifest_version", (request?.ManifestVersion ?? "").Trim()),
            ("device_info_source", (request?.DeviceInfoSource ?? "").Trim()),
            ("gpu_info_source", (request?.GpuInfoSource ?? "").Trim()),
            ("wmi_device_status", (request?.WmiDeviceStatus ?? "").Trim()),
            ("wmi_gpu_status", (request?.WmiGpuStatus ?? "").Trim()),
            ("wmi_device_attempts", Math.Max(0, request?.WmiDeviceAttempts ?? 0).ToString(System.Globalization.CultureInfo.InvariantCulture)),
            ("wmi_gpu_attempts", Math.Max(0, request?.WmiGpuAttempts ?? 0).ToString(System.Globalization.CultureInfo.InvariantCulture))
        };

        return RemoteRequestUriQueryBuilder.Build(baseUri, queryPairs);
    }
}
