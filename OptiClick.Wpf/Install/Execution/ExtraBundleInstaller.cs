using OptiClick.Wpf.Install.Archives;
using OptiClick.Wpf.Install.FileSystem;
using OptiClick.Infrastructure.FileSystem;

namespace OptiClick.Wpf.Install.Execution;

public interface IExtraBundleInstaller
{
    Task<ComponentInstallStepResult> InstallAsync(ComponentInstallContext context, CancellationToken cancellationToken = default);
}

public sealed class ExtraBundleInstaller : IExtraBundleInstaller
{
    private readonly OptiClick.Infrastructure.Install.Components.ExtraBundleInstaller _inner;

    public ExtraBundleInstaller(IArchiveDownloader downloader, IArchiveExtractor extractor, IInstallFileSystem fileSystem)
        : this(
            downloader,
            extractor,
            fileSystem,
            new AppLocalDataPathProvider().InstallExecutionTempDirectory)
    {
    }

    public ExtraBundleInstaller(
        IArchiveDownloader downloader,
        IArchiveExtractor extractor,
        IInstallFileSystem fileSystem,
        string installExecutionTempRoot)
    {
        _inner = new OptiClick.Infrastructure.Install.Components.ExtraBundleInstaller(
            new ComponentArchiveDownloaderAdapter(downloader),
            new ComponentArchiveExtractorAdapter(extractor),
            new InstallFileSystemAdapter(fileSystem),
            installExecutionTempRoot: installExecutionTempRoot);
    }

    public async Task<ComponentInstallStepResult> InstallAsync(ComponentInstallContext context, CancellationToken cancellationToken = default)
    {
        var result = await _inner.InstallAsync(
            InfrastructureComponentContextMapper.ToExtraBundleContext(context),
            cancellationToken);

        return InfrastructureComponentResultMapper.ToWpfStepResult(result);
    }
}
