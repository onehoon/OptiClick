namespace OptiClick.Wpf.Shell.Runtime.DeviceIdentity;

public interface IRemoteDeviceIdentityRulesLoader
{
    Task<RemoteDeviceIdentityRulesLoadResult> LoadAsync(CancellationToken cancellationToken = default);
    bool TryApplyLocalCache();
}
