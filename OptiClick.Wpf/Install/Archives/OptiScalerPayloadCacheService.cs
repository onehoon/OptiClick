using System.IO;
using StageStatuses = OptiClick.Wpf.Install.Archives.ArchivePreparationStageStatuses;

namespace OptiClick.Wpf.Install.Archives;

public sealed record OptiScalerPayloadCacheResult
{
    public bool IsSuccess { get; init; }
    public string PayloadDirectory { get; init; } = "";
    public string CacheEntryName { get; init; } = "";
    public string Version { get; init; } = "";
    public string ErrorCode { get; init; } = "";
    public ArchivePreparationStageStatus StageStatus { get; init; } = ArchivePreparationStageStatus.Unknown;
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
    private const string PayloadCacheKind = "payload_dir";
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
                ErrorCode = "missing_metadata",
                StageStatus = StageStatuses.MissingMetadata()
            };
        }

        Directory.CreateDirectory(cacheRoot);
        var cacheVersion = ArchiveEntryNormalizer.ResolveOptiScalerCacheVersion(entry);
        var cacheEntryName = ArchiveEntryNormalizer.ResolveOptiScalerCacheEntryName(entry);
        var finalEntryDir = Path.Combine(cacheRoot, cacheEntryName);
        var finalPayloadDir = finalEntryDir;

        if (TryResolvePreparedPayload(cacheRoot, cacheVersion, cacheEntryName, out var preparedPayloadDir))
        {
            WriteCurrentVersionEntry(cacheVersion, filename, cacheEntryName);
            CleanupStaleOptiScalerEntries(cacheRoot, cacheEntryName);
            PruneManifestVersionsWithoutPayload(cacheRoot);
            return new OptiScalerPayloadCacheResult
            {
                IsSuccess = true,
                PayloadDirectory = preparedPayloadDir,
                CacheEntryName = cacheEntryName,
                Version = cacheVersion,
                StageStatus = StageStatuses.CachedStatus()
            };
        }

        var workRoot = Path.Combine(cacheRoot, $"{StagePrefix}{cacheEntryName}_{Guid.NewGuid():N}");
        var stagingEntryDir = Path.Combine(workRoot, cacheEntryName);
        var stagingPayloadDir = stagingEntryDir;
        var extractRoot = Path.Combine(workRoot, "_extract");
        var downloadPath = Path.Combine(workRoot, filename);
        var backupEntryDir = Path.Combine(cacheRoot, $"{BackupPrefix}{cacheEntryName}_{Guid.NewGuid():N}");
        var stageStatus = ArchivePreparationStageStatus.Unknown;

        try
        {
            Directory.CreateDirectory(workRoot);
            Directory.CreateDirectory(stagingEntryDir);

            var download = await _downloader.DownloadAsync(
                url,
                downloadPath,
                _options.DownloadTimeout,
                cancellationToken,
                entry.Sha256);
            if (!download.IsSuccess)
            {
                stageStatus = StageStatuses.DownloadFailed(download.ErrorCode);
                return new OptiScalerPayloadCacheResult
                {
                    ErrorCode = download.ErrorCode,
                    StageStatus = stageStatus
                };
            }

            stageStatus = StageStatuses.DownloadSucceeded(download);
            if (string.Equals(Path.GetExtension(downloadPath), ".zip", StringComparison.OrdinalIgnoreCase)
                && !ArchivePreparationHelpers.IsValidZipFile(downloadPath))
            {
                stageStatus = StageStatuses.WithFolderFailure(stageStatus, "invalid_zip");
                return new OptiScalerPayloadCacheResult
                {
                    ErrorCode = "invalid_zip",
                    StageStatus = stageStatus
                };
            }

            Directory.CreateDirectory(extractRoot);
            var extract = await _extractor.ExtractAsync(downloadPath, extractRoot, cancellationToken);
            if (!extract.IsSuccess)
            {
                var extractError = string.IsNullOrWhiteSpace(extract.ErrorCode) ? "extract_failed" : extract.ErrorCode;
                stageStatus = StageStatuses.WithFolderFailure(stageStatus, extractError);
                return new OptiScalerPayloadCacheResult
                {
                    ErrorCode = extractError,
                    StageStatus = stageStatus
                };
            }

            var normalizedSource = ArchivePreparationHelpers.ResolvePayloadSourceDirectory(extractRoot);
            CopyDirectory(normalizedSource, stagingPayloadDir);
            if (!_validator.IsValid(stagingPayloadDir, out var payloadError))
            {
                stageStatus = StageStatuses.WithFolderFailure(stageStatus, payloadError);
                return new OptiScalerPayloadCacheResult
                {
                    ErrorCode = payloadError,
                    StageStatus = stageStatus
                };
            }

            if (Directory.Exists(finalEntryDir))
            {
                Directory.Move(finalEntryDir, backupEntryDir);
            }

            Directory.Move(stagingEntryDir, finalEntryDir);
            stageStatus = StageStatuses.WithFolderOk(stageStatus);
            if (Directory.Exists(backupEntryDir))
            {
                Directory.Delete(backupEntryDir, recursive: true);
            }

            CleanupStaleOptiScalerEntries(cacheRoot, cacheEntryName);
            WriteCurrentVersionEntry(cacheVersion, filename, cacheEntryName);
            PruneManifestVersionsWithoutPayload(cacheRoot);
            stageStatus = StageStatuses.WithJsonOk(stageStatus);

            return new OptiScalerPayloadCacheResult
            {
                IsSuccess = true,
                PayloadDirectory = finalPayloadDir,
                CacheEntryName = cacheEntryName,
                Version = cacheVersion,
                StageStatus = stageStatus
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
                ErrorCode = "prepare_failed",
                StageStatus = StageStatuses.EnsureFailure(stageStatus, "prepare_failed")
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

    private bool TryResolvePreparedPayload(
        string cacheRoot,
        string cacheVersion,
        string cacheEntryName,
        out string payloadDirectory)
    {
        payloadDirectory = "";
        var versionEntry = _manifestStore.TryGetVersionEntry(ArchiveAssetRuntimeDataKeys.OptiScaler, cacheVersion);
        if (IsCurrentPayloadManifestEntry(versionEntry, cacheVersion, cacheEntryName))
        {
            var manifestPayloadDir = Path.Combine(cacheRoot, versionEntry!.CacheEntry.Trim());
            if (_validator.IsValid(manifestPayloadDir, out _))
            {
                payloadDirectory = manifestPayloadDir;
                return true;
            }
        }

        var expectedPayloadDir = Path.Combine(cacheRoot, cacheEntryName);
        if (_validator.IsValid(expectedPayloadDir, out _))
        {
            payloadDirectory = expectedPayloadDir;
            return true;
        }

        return false;
    }

    private void WriteCurrentVersionEntry(string cacheVersion, string filename, string cacheEntryName)
    {
        _manifestStore.WriteVersionEntry(
            ArchiveAssetRuntimeDataKeys.OptiScaler,
            cacheVersion,
            filename,
            PayloadCacheKind,
            cacheEntryName);
    }

    private void PruneManifestVersionsWithoutPayload(string cacheRoot)
    {
        _manifestStore.PruneVersionEntriesByCacheEntry(
            ArchiveAssetRuntimeDataKeys.OptiScaler,
            ResolveExistingPayloadCacheEntries(cacheRoot));
    }

    private static bool IsCurrentPayloadManifestEntry(
        ArchiveManifestEntry? manifestEntry,
        string expectedVersion,
        string expectedEntryName)
    {
        if (manifestEntry is null
            || !string.Equals((manifestEntry.CacheKind ?? "").Trim(), PayloadCacheKind, StringComparison.Ordinal))
        {
            return false;
        }

        return string.Equals((manifestEntry.Version ?? "").Trim(), (expectedVersion ?? "").Trim(), StringComparison.Ordinal)
               && string.Equals((manifestEntry.CacheEntry ?? "").Trim(), (expectedEntryName ?? "").Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static void CleanupStaleOptiScalerEntries(string cacheRoot, string keepEntryName)
    {
        if (!Directory.Exists(cacheRoot))
        {
            return;
        }

        var entries = Directory
            .EnumerateDirectories(cacheRoot)
            .Where(static path =>
            {
                var name = Path.GetFileName(path);
                return !name.StartsWith(StagePrefix, StringComparison.Ordinal)
                       && !name.StartsWith(BackupPrefix, StringComparison.Ordinal);
            })
            .Select(static path => new DirectoryInfo(path))
            .OrderByDescending(static info => info.LastWriteTimeUtc)
            .ToArray();
        var keep = entries
            .Where(info => string.Equals(info.Name, keepEntryName.Trim(), StringComparison.OrdinalIgnoreCase))
            .Concat(entries.Where(info => !string.Equals(info.Name, keepEntryName.Trim(), StringComparison.OrdinalIgnoreCase)))
            .Take(2)
            .Select(static info => info.FullName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in entries)
        {
            if (keep.Contains(entry.FullName))
            {
                continue;
            }

            TryDeleteDirectory(entry.FullName);
        }
    }

    private static IEnumerable<string> ResolveExistingPayloadCacheEntries(string cacheRoot)
    {
        if (!Directory.Exists(cacheRoot))
        {
            return [];
        }

        return Directory
            .EnumerateDirectories(cacheRoot)
            .Select(static path => Path.GetFileName(path))
            .Where(static name =>
                !string.IsNullOrWhiteSpace(name)
                && !name.StartsWith(StagePrefix, StringComparison.Ordinal)
                && !name.StartsWith(BackupPrefix, StringComparison.Ordinal))
            .Select(static name => name!);
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
