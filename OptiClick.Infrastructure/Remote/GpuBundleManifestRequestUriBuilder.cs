using OptiClick.Core.Games.GpuBundle;

namespace OptiClick.Infrastructure.Remote;

public sealed class GpuBundleManifestRequestUriBuilder : IGpuBundleManifestRequestUriBuilder
{
    public Uri? Build(string endpoint, GpuBundleManifestFetchRequest request)
    {
        var normalizedEndpoint = (endpoint ?? "").Trim();
        if (!Uri.TryCreate(normalizedEndpoint, UriKind.Absolute, out var baseUri))
        {
            return null;
        }

        return baseUri;
    }
}
