namespace OptiClick.Infrastructure.Downloads;

public interface IRemoteRuntimeDataClient
{
    Task<RemoteRuntimeDataFetchResult> FetchAsync(CancellationToken cancellationToken = default);
}
