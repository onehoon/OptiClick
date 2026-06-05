using System.IO;

namespace OptiClick.Infrastructure.Install.Components;

public interface IExtraBundleInstaller
{
    Task<ComponentInstallStepResult> InstallAsync(ExtraBundleInstallContext context, CancellationToken cancellationToken = default);
}

public sealed class ExtraBundleInstaller : IExtraBundleInstaller
{
    private readonly IComponentArchiveDownloader _downloader;
    private readonly IComponentArchiveExtractor _extractor;
    private readonly IComponentInstallFileSystem _fileSystem;
    private readonly TimeSpan _downloadTimeout;
    private readonly string _installExecutionTempRoot;

    public ExtraBundleInstaller(
        IComponentArchiveDownloader downloader,
        IComponentArchiveExtractor extractor,
        IComponentInstallFileSystem fileSystem,
        TimeSpan? downloadTimeout = null,
        string? installExecutionTempRoot = null)
    {
        _downloader = downloader ?? throw new ArgumentNullException(nameof(downloader));
        _extractor = extractor ?? throw new ArgumentNullException(nameof(extractor));
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        _downloadTimeout = downloadTimeout ?? TimeSpan.FromSeconds(60);
        _installExecutionTempRoot = ResolveInstallExecutionTempRoot(installExecutionTempRoot);
    }

    public async Task<ComponentInstallStepResult> InstallAsync(ExtraBundleInstallContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var alias = InstallerExecutionHelpers.NormalizeAlias(context.ExtraBundleAlias);
        if (string.IsNullOrWhiteSpace(alias))
        {
            return ComponentInstallStepResult.Skipped(ComponentInstallName.ExtraBundle, "not_requested");
        }

        if (!_fileSystem.DirectoryExists(context.TargetPath))
        {
            return ComponentInstallStepResult.Failed(
                ComponentInstallName.ExtraBundle,
                ComponentInstallErrorCodes.InvalidDestination,
                $"target={context.TargetPath}");
        }

        if (!TryResolveDownloadEntry(context.ModuleDownloadLinks, alias, out var entry, out var resolvedAlias))
        {
            var availableKeys = string.Join(
                ",",
                context.ModuleDownloadLinks.Keys
                    .Select(InstallerExecutionHelpers.NormalizeAlias)
                    .Where(static key => !string.IsNullOrWhiteSpace(key))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(static key => key, StringComparer.OrdinalIgnoreCase));
            return ComponentInstallStepResult.Failed(
                ComponentInstallName.ExtraBundle,
                ComponentInstallErrorCodes.SourceMissing,
                $"alias={alias};available={availableKeys}");
        }

        var url = InstallerExecutionHelpers.ReadString(entry, "url");
        if (string.IsNullOrWhiteSpace(url))
        {
            url = InstallerExecutionHelpers.ReadFirstString(entry, "download_url", "source_url");
        }

        if (string.IsNullOrWhiteSpace(url))
        {
            return ComponentInstallStepResult.Failed(
                ComponentInstallName.ExtraBundle,
                ComponentInstallErrorCodes.MissingMetadata,
                $"alias={resolvedAlias}");
        }

        var fileName = InstallerExecutionHelpers.ResolveDownloadFileName(
            url,
            InstallerExecutionHelpers.ReadString(entry, "filename"),
            $"{alias}.7z");
        if (!InstallerExecutionHelpers.IsAllowedArchiveExtension(fileName))
        {
            return ComponentInstallStepResult.Failed(
                ComponentInstallName.ExtraBundle,
                ComponentInstallErrorCodes.UnsupportedArchive,
                $"alias={resolvedAlias};filename={fileName}");
        }

        var tempRoot = Path.Combine(
            _installExecutionTempRoot,
            "ExtraBundle",
            Guid.NewGuid().ToString("N"));
        var downloadPath = Path.Combine(tempRoot, fileName);
        var extractPath = Path.Combine(tempRoot, "payload");
        var sha256 = InstallerExecutionHelpers.ReadFirstString(entry, "sha256", "SHA256");

        try
        {
            _fileSystem.CreateDirectory(tempRoot);
            var download = await _downloader.DownloadAsync(url, downloadPath, _downloadTimeout, cancellationToken, sha256);
            if (!download.IsSuccess)
            {
                return ComponentInstallStepResult.Failed(
                    ComponentInstallName.ExtraBundle,
                    ComponentInstallErrorCodes.DownloadFailed,
                    $"alias={resolvedAlias}");
            }

            var extract = await _extractor.ExtractAsync(downloadPath, extractPath, cancellationToken);
            if (!extract.IsSuccess)
            {
                return ComponentInstallStepResult.Failed(
                    ComponentInstallName.ExtraBundle,
                    ComponentInstallErrorCodes.ExtractFailed,
                    $"alias={resolvedAlias}");
            }

            var copiedFileCount = CopyPayloadTree(extractPath, context.TargetPath);
            return new ComponentInstallStepResult
            {
                Component = ComponentInstallName.ExtraBundle,
                Status = ComponentInstallStatus.Success,
                Message = $"alias={resolvedAlias};copied_files={copiedFileCount};target={context.TargetPath}"
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return ComponentInstallStepResult.Failed(
                ComponentInstallName.ExtraBundle,
                ComponentInstallErrorCodes.CopyFailed,
                $"alias={resolvedAlias}");
        }
        finally
        {
            TryDeleteDirectory(tempRoot);
        }
    }

    private int CopyPayloadTree(string payloadPath, string targetPath)
    {
        // Extra bundles are curated OptiScaler override payloads; overwrites are intentional.
        var copiedFileCount = 0;
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
            copiedFileCount++;
        }

        return copiedFileCount;
    }

    private static bool TryResolveDownloadEntry(
        IReadOnlyDictionary<string, object?> moduleDownloadLinks,
        string alias,
        out IReadOnlyDictionary<string, object?> entry,
        out string resolvedAlias)
    {
        resolvedAlias = alias;
        if (moduleDownloadLinks.TryGetValue(alias, out var directEntry)
            && directEntry is IReadOnlyDictionary<string, object?> direct)
        {
            entry = direct;
            return true;
        }

        foreach (var pair in moduleDownloadLinks)
        {
            var keyAlias = InstallerExecutionHelpers.NormalizeAlias(pair.Key);
            if (!string.Equals(keyAlias, alias, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (pair.Value is not IReadOnlyDictionary<string, object?> normalizedEntry)
            {
                continue;
            }

            resolvedAlias = pair.Key;
            entry = normalizedEntry;
            return true;
        }

        entry = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        return false;
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

    private void TryDeleteDirectory(string path)
    {
        try
        {
            if (_fileSystem.DirectoryExists(path))
            {
                _fileSystem.DeleteDirectory(path, recursive: true);
            }
        }
        catch
        {
            // Ignore cleanup failure.
        }
    }

    private static string ResolveInstallExecutionTempRoot(string? installExecutionTempRoot)
    {
        var configured = (installExecutionTempRoot ?? "").Trim();
        if (string.IsNullOrWhiteSpace(configured))
        {
            throw new ArgumentException("Install execution temp root is required.", nameof(installExecutionTempRoot));
        }

        return Path.GetFullPath(configured);
    }
}
