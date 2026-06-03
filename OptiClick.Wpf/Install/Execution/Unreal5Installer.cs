using OptiClick.Wpf.Install.Archives;
using OptiClick.Wpf.Install.FileSystem;

namespace OptiClick.Wpf.Install.Execution;

public interface IUnreal5Installer
{
    Task<ComponentInstallStepResult> InstallAsync(ComponentInstallContext context, CancellationToken cancellationToken = default);
}

public sealed class Unreal5Installer : IUnreal5Installer
{
    private readonly OptiClick.Infrastructure.Install.Components.Unreal5Installer _inner;

    public Unreal5Installer(
        IArchiveSourceReader archiveSourceReader,
        IArchiveExtractor archiveExtractor,
        IInstallFileSystem fileSystem)
    {
        _inner = new OptiClick.Infrastructure.Install.Components.Unreal5Installer(
            new ComponentArchiveSourceReaderAdapter(archiveSourceReader),
            new ComponentArchiveExtractorAdapter(archiveExtractor),
            new InstallFileSystemAdapter(fileSystem));
    }

    public async Task<ComponentInstallStepResult> InstallAsync(ComponentInstallContext context, CancellationToken cancellationToken = default)
    {
        var result = await _inner.InstallAsync(
            InfrastructureComponentInstallerAdapters.ToUnreal5Context(context),
            cancellationToken);

        return InfrastructureComponentInstallerAdapters.ToWpfStepResult(result);
    }
}
