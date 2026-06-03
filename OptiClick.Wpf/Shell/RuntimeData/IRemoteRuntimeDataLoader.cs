namespace OptiClick.Wpf.Shell.RuntimeData;

public interface IRemoteRuntimeDataLoader
{
    Task<RemoteRuntimeDataLoadResult> LoadAsync(CancellationToken cancellationToken = default);
}
