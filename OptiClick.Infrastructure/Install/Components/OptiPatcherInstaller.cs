using System.IO;

namespace OptiClick.Infrastructure.Install.Components;

public interface IOptiPatcherInstaller
{
    Task<ComponentInstallStepResult> InstallAsync(OptiPatcherInstallContext context, CancellationToken cancellationToken = default);
}

public sealed class OptiPatcherInstaller : IOptiPatcherInstaller
{
    private readonly IComponentArchiveSourceReader _archiveSourceReader;
    private readonly IComponentInstallFileSystem _fileSystem;

    public OptiPatcherInstaller(IComponentArchiveSourceReader archiveSourceReader, IComponentInstallFileSystem fileSystem)
    {
        _archiveSourceReader = archiveSourceReader ?? throw new ArgumentNullException(nameof(archiveSourceReader));
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
    }

    public async Task<ComponentInstallStepResult> InstallAsync(OptiPatcherInstallContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var source = "";
        var payload = "";
        if (!context.UseOptiPatcher)
        {
            return ComponentInstallStepResult.Skipped(ComponentInstallName.OptiPatcher);
        }

        var url = InstallerExecutionHelpers.ExtractModuleUrl(context.ModuleDownloadLinks, "optipatcher");
        var fileName = ReadEntryFileName(context.ModuleDownloadLinks, "optipatcher");
        if (string.IsNullOrWhiteSpace(url) && string.IsNullOrWhiteSpace(context.OptiPatcherCachedArchivePath))
        {
            return ComponentInstallStepResult.Failed(ComponentInstallName.OptiPatcher, ComponentInstallErrorCodes.SourceMissing);
        }

        try
        {
            source = await _archiveSourceReader.ResolveSourcePathAsync(url, context.OptiPatcherCachedArchivePath, fileName, cancellationToken);
            if (string.IsNullOrWhiteSpace(source))
            {
                return ComponentInstallStepResult.Failed(ComponentInstallName.OptiPatcher, ComponentInstallErrorCodes.SourceMissing);
            }

            var preferred = await _archiveSourceReader.FindFilesAsync(
                source,
                path => IsOptiPatcherAsi(Path.GetFileName(path)),
                cancellationToken);
            var selected = SelectSinglePayload(preferred);
            if (selected.ErrorCode == ComponentInstallErrorCodes.MultipleCandidates)
            {
                return ComponentInstallStepResult.Failed(ComponentInstallName.OptiPatcher, selected.ErrorCode);
            }

            payload = selected.Path;
            if (string.IsNullOrWhiteSpace(payload))
            {
                var fallbackAsi = await _archiveSourceReader.FindFilesAsync(
                    source,
                    path => string.Equals(Path.GetExtension(path), ".asi", StringComparison.OrdinalIgnoreCase),
                    cancellationToken);
                selected = SelectSinglePayload(fallbackAsi);
                if (selected.ErrorCode == ComponentInstallErrorCodes.MultipleCandidates)
                {
                    return ComponentInstallStepResult.Failed(ComponentInstallName.OptiPatcher, selected.ErrorCode);
                }

                payload = selected.Path;
            }

            if (string.IsNullOrWhiteSpace(payload))
            {
                return ComponentInstallStepResult.Failed(ComponentInstallName.OptiPatcher, ComponentInstallErrorCodes.PayloadMissing);
            }

            var pluginsDir = Path.Combine(context.TargetPath, "plugins");
            _fileSystem.CreateDirectory(pluginsDir);
            CleanupExistingOptiPatcherAsi(pluginsDir);
            var destination = Path.Combine(pluginsDir, "OptiPatcher.asi");
            if (_fileSystem.FileExists(destination))
            {
                InstallerExecutionHelpers.EnsureWritableIfExists(_fileSystem, destination);
            }

            _fileSystem.CopyFile(payload, destination, overwrite: true);
            return ComponentInstallStepResult.Success(ComponentInstallName.OptiPatcher);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return ComponentInstallStepResult.Failed(ComponentInstallName.OptiPatcher, ComponentInstallErrorCodes.CopyFailed);
        }
        finally
        {
            if (!string.IsNullOrWhiteSpace(payload))
            {
                _archiveSourceReader.CleanupTemporaryPath(payload);
            }

            _archiveSourceReader.CleanupTemporaryPath(source);
        }
    }

    private void CleanupExistingOptiPatcherAsi(string pluginsDir)
    {
        if (!_fileSystem.DirectoryExists(pluginsDir))
        {
            return;
        }

        foreach (var entry in _fileSystem.EnumerateFileSystemEntries(pluginsDir))
        {
            if (!_fileSystem.FileExists(entry))
            {
                continue;
            }

            if (!string.Equals(Path.GetExtension(entry), ".asi", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!IsOptiPatcherAsi(Path.GetFileName(entry)))
            {
                continue;
            }

            InstallerExecutionHelpers.EnsureWritableIfExists(_fileSystem, entry);
            _fileSystem.DeleteFile(entry);
        }
    }

    private static bool IsOptiPatcherAsi(string name)
    {
        var normalized = (name ?? "").Trim().ToLowerInvariant();
        return normalized.EndsWith(".asi", StringComparison.Ordinal) && normalized.Contains("optipatcher", StringComparison.Ordinal);
    }

    private static (string Path, string ErrorCode) SelectSinglePayload(IReadOnlyList<string> candidates)
    {
        if (candidates.Count == 0)
        {
            return ("", "");
        }

        if (candidates.Count == 1)
        {
            return (candidates[0], "");
        }

        var exact = candidates.Where(candidate =>
                string.Equals(Path.GetFileName(candidate), "OptiPatcher.asi", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (exact.Length == 1)
        {
            return (exact[0], "");
        }

        return ("", ComponentInstallErrorCodes.MultipleCandidates);
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
