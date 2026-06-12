using OptiClick.Wpf.Install.FileSystem;

namespace OptiClick.Wpf.Install.Execution;

public interface IOptiPatcherInstaller
{
    Task<ComponentInstallStepResult> InstallAsync(ComponentInstallContext context, CancellationToken cancellationToken = default);
}

public sealed class OptiPatcherInstaller : IOptiPatcherInstaller
{
    private readonly OptiClick.Infrastructure.Install.Components.OptiPatcherInstaller _inner;

    public OptiPatcherInstaller(IArchiveSourceReader archiveSourceReader, IInstallFileSystem fileSystem)
    {
        _inner = new OptiClick.Infrastructure.Install.Components.OptiPatcherInstaller(
            new ComponentArchiveSourceReaderAdapter(archiveSourceReader),
            new InstallFileSystemAdapter(fileSystem));
    }

    public async Task<ComponentInstallStepResult> InstallAsync(ComponentInstallContext context, CancellationToken cancellationToken = default)
    {
        var result = await _inner.InstallAsync(
            InfrastructureComponentContextMapper.ToOptiPatcherContext(context),
            cancellationToken);

        return InfrastructureComponentResultMapper.ToWpfStepResult(result);
    }
}
