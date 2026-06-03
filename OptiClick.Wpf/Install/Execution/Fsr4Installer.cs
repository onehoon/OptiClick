using OptiClick.Wpf.Install.Archives;
using OptiClick.Wpf.Install.FileSystem;
using OptiClick.Infrastructure.FileSystem;

namespace OptiClick.Wpf.Install.Execution;

public interface IFsr4Installer
{
    Task<ComponentInstallStepResult> InstallAsync(ComponentInstallContext context, CancellationToken cancellationToken = default);
}

public sealed class Fsr4Installer : IFsr4Installer
{
    private readonly OptiClick.Infrastructure.Install.Components.Fsr4Installer _inner;

    public Fsr4Installer(IArchiveExtractor archiveExtractor, IInstallFileSystem fileSystem)
    {
        _inner = new OptiClick.Infrastructure.Install.Components.Fsr4Installer(
            new ComponentArchiveExtractorAdapter(archiveExtractor),
            new InstallFileSystemAdapter(fileSystem),
            new OptiClick.Infrastructure.Install.Components.Fsr4InstallEligibilityResolver(),
            new AppLocalDataPathProvider().InstallExecutionTempDirectory);
    }

    public async Task<ComponentInstallStepResult> InstallAsync(ComponentInstallContext context, CancellationToken cancellationToken = default)
    {
        var result = await _inner.InstallAsync(
            InfrastructureComponentInstallerAdapters.ToFsr4Context(context),
            cancellationToken);

        return InfrastructureComponentInstallerAdapters.ToWpfStepResult(result);
    }
}
