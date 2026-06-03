using OptiClick.Core.Runtime;
using OptiClick.Core.Abstractions;

namespace OptiClick.Wpf.Services;

public sealed class ConfigurationRemoteEndpointProvider : IRemoteEndpointProvider
{
    private readonly RemoteDataOptions _options;

    public ConfigurationRemoteEndpointProvider(RemoteDataOptions options)
    {
        _options = options ?? new RemoteDataOptions();
    }

    public RemoteDataOptions GetRemoteDataOptions()
    {
        return _options;
    }
}
