using OptiClick.Core.Runtime;

namespace OptiClick.Core.Abstractions;

public interface IRemoteEndpointProvider
{
    RemoteDataOptions GetRemoteDataOptions();
}
