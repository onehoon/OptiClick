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
    private readonly IComponentInstallFileSystem _fileSystem;

    public Unreal5Installer(
        IComponentArchiveSourceReader archiveSourceReader,
        IComponentArchiveExtractor archiveExtractor,
        IComponentInstallFileSystem fileSystem)
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
        var sha256 = InstallerExecutionHelpers.ExtractModuleSha256(context.ModuleDownloadLinks, "unreal5");
        if (string.IsNullOrWhiteSpace(url) && string.IsNullOrWhiteSpace(context.Unreal5CachedArchivePath))
        {
            return ComponentInstallStepResult.Skipped(ComponentInstallName.Unreal5, "source_not_configured");
        }

        try
        {
            source = await _archiveSourceReader.ResolveSourcePathAsync(
                url,
                context.Unreal5CachedArchivePath,
                fileName,
                cancellationToken,
                sha256);
            if (string.IsNullOrWhiteSpace(source))
            {
                return ComponentInstallStepResult.Skipped(ComponentInstallName.Unreal5, "source_not_resolved");
            }

            if (_fileSystem.FileExists(Path.Combine(context.TargetPath, "dxgi.dll")))
            {
                // Unreal5 is the only archive component that treats an existing root dxgi.dll as an allowed skip.
                return ComponentInstallStepResult.Skipped(ComponentInstallName.Unreal5, "dxgi_exists");
            }

            if (_fileSystem.DirectoryExists(source))
            {
                try
                {
                    CopyPayloadTree(source, context.TargetPath);
                    return ComponentInstallStepResult.Success(ComponentInstallName.Unreal5);
                }
                catch
                {
                    return ComponentInstallStepResult.Failed(ComponentInstallName.Unreal5, ComponentInstallErrorCodes.CopyFailed);
                }
            }

            if (!InstallerExecutionHelpers.IsAllowedArchiveExtension(source))
            {
                return ComponentInstallStepResult.Failed(ComponentInstallName.Unreal5, ComponentInstallErrorCodes.UnsupportedArchive);
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

    private void CopyPayloadTree(string payloadPath, string targetPath)
    {
        var orderedEntries = EnumeratePayloadEntries(payloadPath)
            .OrderBy(static path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        foreach (var entry in orderedEntries)
        {
            var relative = Path.GetRelativePath(payloadPath, entry);
            var destination = InstallerExecutionHelpers.CombineUnderTarget(targetPath, relative);
            if (_fileSystem.DirectoryExists(entry))
            {
                if (_fileSystem.FileExists(destination))
                {
                    InstallerExecutionHelpers.EnsureWritableIfExists(_fileSystem, destination);
                    _fileSystem.DeleteFile(destination);
                }

                _fileSystem.CreateDirectory(destination);
                continue;
            }

            var parent = Path.GetDirectoryName(destination)!;
            if (_fileSystem.FileExists(parent))
            {
                InstallerExecutionHelpers.EnsureWritableIfExists(_fileSystem, parent);
                _fileSystem.DeleteFile(parent);
            }

            _fileSystem.CreateDirectory(parent);
            if (_fileSystem.DirectoryExists(destination))
            {
                throw new InvalidOperationException($"Destination path is a directory: {destination}");
            }

            if (_fileSystem.FileExists(destination))
            {
                InstallerExecutionHelpers.EnsureWritableIfExists(_fileSystem, destination);
            }

            _fileSystem.CopyFile(entry, destination, overwrite: true);
        }
    }

    private IEnumerable<string> EnumeratePayloadEntries(string payloadPath)
    {
        var stack = new Stack<string>();
        stack.Push(payloadPath);
        while (stack.Count > 0)
        {
            var current = stack.Pop();
            foreach (var entry in _fileSystem.EnumerateFileSystemEntries(current))
            {
                yield return entry;
                if (_fileSystem.DirectoryExists(entry))
                {
                    stack.Push(entry);
                }
            }
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
