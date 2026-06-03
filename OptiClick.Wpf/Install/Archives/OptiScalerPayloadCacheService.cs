using System.IO;

namespace OptiClick.Wpf.Install.Archives;

public sealed record OptiScalerPayloadCacheResult
{
    public bool IsSuccess { get; init; }
    public string PayloadDirectory { get; init; } = "";
    public string CacheEntryName { get; init; } = "";
    public string Version { get; init; } = "";
    public string ErrorCode { get; init; } = "";
}

public interface IOptiScalerPayloadCacheService
{
    Task<OptiScalerPayloadCacheResult> PrepareAsync(
        RemoteArchiveEntry entry,
        string cacheRoot,
        CancellationToken cancellationToken = default);
}

public sealed class OptiScalerPayloadCacheService : IOptiScalerPayloadCacheService
{
    private const string StagePrefix = ".optiscaler_stage_";
    private const string BackupPrefix = ".optiscaler_backup_";
    private readonly IArchiveDownloader _downloader;
    private readonly IArchiveExtractor _extractor;
    private readonly IArchiveDownloadManifestStore _manifestStore;
    private readonly OptiScalerPayloadValidator _validator;
    private readonly ArchivePreparationOptions _options;

    public OptiScalerPayloadCacheService(
        IArchiveDownloader downloader,
        IArchiveExtractor extractor,
        IArchiveDownloadManifestStore manifestStore,
        OptiScalerPayloadValidator validator,
        ArchivePreparationOptions? options = null)
    {
        _downloader = downloader;
        _extractor = extractor;
        _manifestStore = manifestStore;
        _validator = validator;
        _options = options ?? new ArchivePreparationOptions();
    }

    public async Task<OptiScalerPayloadCacheResult> PrepareAsync(
        RemoteArchiveEntry entry,
        string cacheRoot,
        CancellationToken cancellationToken = default)
    {
        var url = (entry.Url ?? "").Trim();
        var filename = (entry.Filename ?? "").Trim();
        if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(filename))
        {
            return new OptiScalerPayloadCacheResult
            {
                ErrorCode = "missing_metadata"
            };
        }

        Directory.CreateDirectory(cacheRoot);
        var cacheVersion = ArchiveEntryNormalizer.ResolveOptiScalerCacheVersion(entry);
        var cacheEntryName = ResolveCacheEntryName(entry, cacheRoot);
        var finalEntryDir = Path.Combine(cacheRoot, cacheEntryName);
        var finalPayloadDir = finalEntryDir;

        if (_validator.IsValid(finalPayloadDir, out _) && !_manifestStore.IsUpdateNeeded(ArchiveAssetRuntimeDataKeys.OptiScaler, cacheVersion))
        {
            CleanupStaleOptiScalerEntries(cacheRoot, cacheEntryName);
            return new OptiScalerPayloadCacheResult
            {
                IsSuccess = true,
                PayloadDirectory = finalPayloadDir,
                CacheEntryName = cacheEntryName,
                Version = cacheVersion
            };
        }

        var workRoot = Path.Combine(cacheRoot, $"{StagePrefix}{cacheEntryName}_{Guid.NewGuid():N}");
        var stagingEntryDir = Path.Combine(workRoot, cacheEntryName);
        var stagingPayloadDir = stagingEntryDir;
        var extractRoot = Path.Combine(workRoot, "_extract");
        var downloadPath = Path.Combine(workRoot, filename);
        var backupEntryDir = Path.Combine(cacheRoot, $"{BackupPrefix}{cacheEntryName}_{Guid.NewGuid():N}");

        try
        {
            Directory.CreateDirectory(workRoot);
            Directory.CreateDirectory(stagingEntryDir);

            var download = await _downloader.DownloadAsync(url, downloadPath, _options.DownloadTimeout, cancellationToken);
            if (!download.IsSuccess)
            {
                return new OptiScalerPayloadCacheResult
                {
                    ErrorCode = download.ErrorCode
                };
            }

            if (string.Equals(Path.GetExtension(downloadPath), ".zip", StringComparison.OrdinalIgnoreCase)
                && !ArchivePreparationHelpers.IsValidZipFile(downloadPath))
            {
                return new OptiScalerPayloadCacheResult
                {
                    ErrorCode = "invalid_zip"
                };
            }

            Directory.CreateDirectory(extractRoot);
            var extract = await _extractor.ExtractAsync(downloadPath, extractRoot, cancellationToken);
            if (!extract.IsSuccess)
            {
                return new OptiScalerPayloadCacheResult
                {
                    ErrorCode = extract.ErrorCode
                };
            }

            var normalizedSource = ArchivePreparationHelpers.ResolvePayloadSourceDirectory(extractRoot);
            CopyDirectory(normalizedSource, stagingPayloadDir);
            if (!_validator.IsValid(stagingPayloadDir, out var payloadError))
            {
                return new OptiScalerPayloadCacheResult
                {
                    ErrorCode = payloadError
                };
            }

            if (Directory.Exists(finalEntryDir))
            {
                Directory.Move(finalEntryDir, backupEntryDir);
            }

            Directory.Move(stagingEntryDir, finalEntryDir);
            if (Directory.Exists(backupEntryDir))
            {
                Directory.Delete(backupEntryDir, recursive: true);
            }

            CleanupStaleOptiScalerEntries(cacheRoot, cacheEntryName);
            _manifestStore.WriteEntry(
                ArchiveAssetRuntimeDataKeys.OptiScaler,
                cacheVersion,
                filename,
                cacheKind: "payload_dir",
                cacheEntry: cacheEntryName);

            return new OptiScalerPayloadCacheResult
            {
                IsSuccess = true,
                PayloadDirectory = finalPayloadDir,
                CacheEntryName = cacheEntryName,
                Version = cacheVersion
            };
        }
        catch
        {
            if (Directory.Exists(backupEntryDir) && !Directory.Exists(finalEntryDir))
            {
                TryMoveDirectory(backupEntryDir, finalEntryDir);
            }

            return new OptiScalerPayloadCacheResult
            {
                ErrorCode = "prepare_failed"
            };
        }
        finally
        {
            TryDeleteDirectory(workRoot);
            if (Directory.Exists(backupEntryDir) && Directory.Exists(finalEntryDir))
            {
                TryDeleteDirectory(backupEntryDir);
            }
        }
    }

    private string ResolveCacheEntryName(RemoteArchiveEntry entry, string cacheRoot)
    {
        var derivedName = ArchiveEntryNormalizer.ResolveOptiScalerCacheEntryName(entry);
        var manifestEntry = _manifestStore.TryGetEntry(ArchiveAssetRuntimeDataKeys.OptiScaler);
        if (manifestEntry is null)
        {
            return derivedName;
        }

        if (!string.Equals((manifestEntry.CacheKind ?? "").Trim(), "payload_dir", StringComparison.Ordinal))
        {
            return derivedName;
        }

        var manifestCacheEntry = (manifestEntry.CacheEntry ?? "").Trim();
        if (string.IsNullOrWhiteSpace(manifestCacheEntry))
        {
            return derivedName;
        }

        var manifestPayloadDir = Path.Combine(cacheRoot, manifestCacheEntry);
        return _validator.IsValid(manifestPayloadDir, out _) ? manifestCacheEntry : derivedName;
    }

    private static void CleanupStaleOptiScalerEntries(string cacheRoot, string keepEntryName)
    {
        if (!Directory.Exists(cacheRoot))
        {
            return;
        }

        var keep = keepEntryName.Trim();
        foreach (var entryPath in Directory.EnumerateFileSystemEntries(cacheRoot))
        {
            var name = Path.GetFileName(entryPath);
            if (string.Equals(name, keep, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (string.Equals(name, "cache_manifest.json", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (Directory.Exists(entryPath))
            {
                TryDeleteDirectory(entryPath);
                continue;
            }

            var ext = Path.GetExtension(entryPath);
            if (ext is ".7z" or ".zip" or ".rar" or ".tar" or ".gz" or ".xz" or ".bz2" or ".asi")
            {
                ArchivePreparationHelpers.TryDeleteFile(entryPath);
            }
        }
    }

    private static void CopyDirectory(string sourceDir, string destinationDir)
    {
        var source = new DirectoryInfo(sourceDir);
        if (!source.Exists)
        {
            throw new DirectoryNotFoundException(sourceDir);
        }

        Directory.CreateDirectory(destinationDir);
        foreach (var file in source.GetFiles("*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(source.FullName, file.FullName);
            var destinationFile = Path.Combine(destinationDir, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(destinationFile)!);
            file.CopyTo(destinationFile, overwrite: true);
        }
    }

    private static void TryMoveDirectory(string source, string destination)
    {
        try
        {
            Directory.Move(source, destination);
        }
        catch
        {
            // Ignore restore failure.
        }
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch
        {
            // Ignore cleanup failure.
        }
    }
}
