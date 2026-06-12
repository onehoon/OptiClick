using OptiClick.Wpf.Install.FileSystem;

namespace OptiClick.Wpf.Install.Execution;

public interface IUltimateAsiLoaderInstaller
{
    Task<ComponentInstallStepResult> InstallAsync(ComponentInstallContext context, CancellationToken cancellationToken = default);
}

public sealed class UltimateAsiLoaderInstaller : IUltimateAsiLoaderInstaller
{
    private readonly OptiClick.Infrastructure.Install.Components.UltimateAsiLoaderInstaller _inner;

    public UltimateAsiLoaderInstaller(
        IDllPayloadInstaller dllPayloadInstaller,
        IInstallFileSystem fileSystem,
        IFileSignatureDetectors detectors)
    {
        _inner = new OptiClick.Infrastructure.Install.Components.UltimateAsiLoaderInstaller(
            new DllPayloadInstallerAdapter(dllPayloadInstaller),
            new InstallFileSystemAdapter(fileSystem),
            new FileSignatureDetectorsAdapter(detectors));
    }

    public async Task<ComponentInstallStepResult> InstallAsync(ComponentInstallContext context, CancellationToken cancellationToken = default)
    {
        var result = await _inner.InstallAsync(
            InfrastructureComponentContextMapper.ToUltimateAsiLoaderContext(context),
            cancellationToken);

        return InfrastructureComponentResultMapper.ToWpfStepResult(result);
    }
}
