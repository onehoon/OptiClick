using OptiClick.Core.Runtime;
using OptiClick.Core.Abstractions;

namespace OptiClick.Wpf.Services;

public sealed class MockRemoteEndpointProvider : IRemoteEndpointProvider
{
    private readonly RemoteDataOptions _options;

    public MockRemoteEndpointProvider()
        : this(
            new RemoteDataOptions
            {
                RuntimeDataUrl = "",
                GpuBundleManifestUrl = "",
                GpuBundleUrl = "",
                ManifestEndpoint = "",
                Channel = "stable",
                AllowMockGpuManifestFallback = true
            })
    {
    }

    public MockRemoteEndpointProvider(RemoteDataOptions options)
    {
        _options = options ?? new RemoteDataOptions();
    }

    public RemoteDataOptions GetRemoteDataOptions()
    {
        return _options;
    }
}
