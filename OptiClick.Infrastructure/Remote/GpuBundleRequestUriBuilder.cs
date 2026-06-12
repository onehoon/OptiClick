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
            ("manifest_version", (request?.ManifestVersion ?? "").Trim())
        };

        return RemoteRequestUriQueryBuilder.Build(baseUri, queryPairs);
    }
}
