using OptiClick.Wpf.Install.FileSystem;

namespace OptiClick.Wpf.Install.Execution;

public interface IReFrameworkInstaller
{
    Task<ComponentInstallStepResult> InstallAsync(ComponentInstallContext context, CancellationToken cancellationToken = default);
}

public sealed class ReFrameworkInstaller : IReFrameworkInstaller
{
    private readonly OptiClick.Infrastructure.Install.Components.ReFrameworkInstaller _inner;

    public ReFrameworkInstaller(
        IDllPayloadInstaller dllPayloadInstaller,
        IInstallFileSystem fileSystem,
        IFileSignatureDetectors detectors)
    {
        _inner = new OptiClick.Infrastructure.Install.Components.ReFrameworkInstaller(
            new DllPayloadInstallerAdapter(dllPayloadInstaller),
            new InstallFileSystemAdapter(fileSystem),
            new FileSignatureDetectorsAdapter(detectors));
    }

    public async Task<ComponentInstallStepResult> InstallAsync(ComponentInstallContext context, CancellationToken cancellationToken = default)
    {
        var result = await _inner.InstallAsync(
            InfrastructureComponentContextMapper.ToReFrameworkContext(context),
            cancellationToken);

        return InfrastructureComponentResultMapper.ToWpfStepResult(result);
    }
}
