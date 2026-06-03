namespace OptiClick.Infrastructure.Downloads;

public interface IRemoteDeviceIdentityRulesClient
{
    Task<RemoteDeviceIdentityRulesFetchResult> FetchAsync(CancellationToken cancellationToken = default);
}
