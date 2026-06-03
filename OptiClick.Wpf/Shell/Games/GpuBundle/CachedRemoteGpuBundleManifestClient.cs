using System.Collections.Concurrent;

namespace OptiClick.Wpf.Shell.Games.GpuBundle;

public sealed class CachedRemoteGpuBundleManifestClient : IRemoteGpuBundleManifestClient
{
    private readonly IRemoteGpuBundleManifestClient _inner;
    private readonly IGpuBundleManifestRequestUriBuilder _requestUriBuilder;
    private readonly ConcurrentDictionary<string, RemoteGpuBundleManifestFetchResult> _successCache = new(StringComparer.Ordinal);

    public CachedRemoteGpuBundleManifestClient(
        IRemoteGpuBundleManifestClient inner,
        IGpuBundleManifestRequestUriBuilder requestUriBuilder)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _requestUriBuilder = requestUriBuilder ?? throw new ArgumentNullException(nameof(requestUriBuilder));
    }

    public async Task<RemoteGpuBundleManifestFetchResult> FetchAsync(
        string endpoint,
        GpuBundleManifestFetchRequest request,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = BuildCacheKey(endpoint, request);
        if (!string.IsNullOrWhiteSpace(cacheKey) && _successCache.TryGetValue(cacheKey, out var cachedResult))
        {
            return cachedResult;
        }

        var result = await _inner.FetchAsync(endpoint, request, cancellationToken).ConfigureAwait(false);
        if (result.IsSuccess && !string.IsNullOrWhiteSpace(cacheKey))
        {
            _successCache[cacheKey] = result;
        }

        return result;
    }

    private string BuildCacheKey(string endpoint, GpuBundleManifestFetchRequest request)
    {
        var normalizedEndpoint = (endpoint ?? "").Trim();
        if (string.IsNullOrWhiteSpace(normalizedEndpoint))
        {
            return "";
        }

        var safeRequest = request ?? new GpuBundleManifestFetchRequest();
        var requestUri = _requestUriBuilder.Build(normalizedEndpoint, safeRequest);
        if (requestUri is null)
        {
            return "";
        }

        return requestUri.AbsoluteUri;
    }
}
