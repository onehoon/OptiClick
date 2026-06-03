using System.IO;

namespace OptiClick.Infrastructure.Install.Components;

public interface IUnreal5Installer
{
    Task<ComponentInstallStepResult> InstallAsync(Unreal5InstallContext context, CancellationToken cancellationToken = default);
}

public sealed class Unreal5Installer : IUnreal5Installer
{
    private readonly IComponentArchiveSourceReader _archiveSourceReader;
    private readonly IComponentArchiveExtractor _archiveExtractor;
    private readonly IInstallFileSystem _fileSystem;

    public Unreal5Installer(
        IComponentArchiveSourceReader archiveSourceReader,
        IComponentArchiveExtractor archiveExtractor,
        IInstallFileSystem fileSystem)
    {
        _archiveSourceReader = archiveSourceReader ?? throw new ArgumentNullException(nameof(archiveSourceReader));
        _archiveExtractor = archiveExtractor ?? throw new ArgumentNullException(nameof(archiveExtractor));
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
    }

    public async Task<ComponentInstallStepResult> InstallAsync(Unreal5InstallContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var source = "";
        if (!context.UseUnreal5)
        {
            return ComponentInstallStepResult.Skipped(ComponentInstallName.Unreal5);
        }

        var url = InstallerExecutionHelpers.ExtractModuleUrl(context.ModuleDownloadLinks, "unreal5");
        var fileName = ReadEntryFileName(context.ModuleDownloadLinks, "unreal5");
        if (string.IsNullOrWhiteSpace(url) && string.IsNullOrWhiteSpace(context.Unreal5CachedArchivePath))
        {
            return ComponentInstallStepResult.Skipped(ComponentInstallName.Unreal5, "source_not_configured");
        }

        try
        {
            source = await _archiveSourceReader.ResolveSourcePathAsync(url, context.Unreal5CachedArchivePath, fileName, cancellationToken);
            if (string.IsNullOrWhiteSpace(source))
            {
                return ComponentInstallStepResult.Skipped(ComponentInstallName.Unreal5, "source_not_resolved");
            }

            if (!InstallerExecutionHelpers.IsAllowedArchiveExtension(source))
            {
                return ComponentInstallStepResult.Failed(ComponentInstallName.Unreal5, ComponentInstallErrorCodes.UnsupportedArchive);
            }

            if (_fileSystem.FileExists(Path.Combine(context.TargetPath, "dxgi.dll")))
            {
                return ComponentInstallStepResult.Skipped(ComponentInstallName.Unreal5, "dxgi_exists");
            }

            var extract = await _archiveExtractor.ExtractAsync(source, context.TargetPath, cancellationToken);
            return extract.IsSuccess
                ? ComponentInstallStepResult.Success(ComponentInstallName.Unreal5)
                : ComponentInstallStepResult.Failed(ComponentInstallName.Unreal5, ComponentInstallErrorCodes.ExtractFailed);
        }
        finally
        {
            _archiveSourceReader.CleanupTemporaryPath(source);
        }
    }

    private static string ReadEntryFileName(IReadOnlyDictionary<string, object?> links, string key)
    {
        if (!links.TryGetValue(key, out var rawEntry)
            || rawEntry is not IReadOnlyDictionary<string, object?> entry)
        {
            return "";
        }

        return InstallerExecutionHelpers.ReadString(entry, "filename");
    }
}
