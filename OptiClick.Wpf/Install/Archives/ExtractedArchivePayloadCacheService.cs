using System.IO;

namespace OptiClick.Wpf.Install.Archives;

public sealed record ExtractedArchivePayloadCacheRequest
{
    public ArchiveAssetKey AssetKey { get; init; }
    public string AssetRuntimeDataKey { get; init; } = "";
    public string AssetLabel { get; init; } = "archive";
    public RemoteArchiveEntry Entry { get; init; } = new();
    public string CacheRoot { get; init; } = "";
    public string CacheEntryName { get; init; } = "";
    public IArchivePayloadValidator Validator { get; init; } = new NonEmptyPayloadValidator();
    public bool ForceRefresh { get; init; }
    public bool AllowExistingFallback { get; init; } = true;
    public bool AllowDirectPayloadFile { get; init; }
    public bool EnableRetentionCleanup { get; init; } = true;
    public int RetentionCount { get; init; } = 2;
    public string ManifestVersion { get; init; } = "";
}

public sealed class ExtractedArchivePayloadCacheService
{
    private const string PayloadCacheKind = "payload_dir";
    private const string StagePrefix = ".__stage_";
    private const string BackupPrefix = ".__backup_";

    private readonly IArchiveDownloader _downloader;
    private readonly IArchiveExtractor _extractor;
    private readonly IArchiveDownloadManifestStore _manifestStore;
    private readonly ArchivePreparationOptions _options;

    public ExtractedArchivePayloadCacheService(
        IArchiveDownloader downloader,
        IArchiveExtractor extractor,
        IArchiveDownloadManifestStore manifestStore,
        ArchivePreparationOptions? options = null)
    {
        _downloader = downloader ?? throw new ArgumentNullException(nameof(downloader));
        _extractor = extractor ?? throw new ArgumentNullException(nameof(extractor));
        _manifestStore = manifestStore ?? throw new ArgumentNullException(nameof(manifestStore));
        _options = options ?? new ArchivePreparationOptions();
    }

    public async Task<ArchivePreparationState> PrepareAsync(
        ExtractedArchivePayloadCacheRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var entry = request.Entry ?? new RemoteArchiveEntry();
        var filename = SanitizeFilename(entry.Filename);
        var url = (entry.Url ?? "").Trim();
        var assetKey = ResolveAssetKey(request);
        var cacheEntryName = ArchivePayloadCacheEntryNames.Normalize(request.CacheEntryName, assetKey);
        var cacheRoot = Path.GetFullPath((request.CacheRoot ?? "").Trim());
        var manifestVersion = ResolveManifestVersion(request, cacheEntryName);
        var finalEntryDir = Path.Combine(cacheRoot, cacheEntryName);
        var stageStatus = ArchivePreparationStageStatus.Unknown;

        Directory.CreateDirectory(cacheRoot);

        if (IsValidPayload(finalEntryDir, request.Validator)
            && !request.ForceRefresh
            && !IsUpdateNeeded(assetKey, manifestVersion))
        {
            CleanupRetention(request, cacheRoot, cacheEntryName);
            return ReadyState(filename, finalEntryDir, CachedStatus());
        }

        if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(filename))
        {
            stageStatus = StageStatus("missing_metadata", "skipped", "skipped", "missing", "skipped");
            return TryResolveFallback(request, cacheRoot, cacheEntryName, filename, stageStatus, out var fallback)
                ? fallback
                : MissingMetadataState(request.AssetLabel, filename, stageStatus);
        }

        var workRoot = Path.Combine(cacheRoot, $"{StagePrefix}{cacheEntryName}_{Guid.NewGuid():N}");
        var stagingEntryDir = Path.Combine(workRoot, cacheEntryName);
        var extractRoot = Path.Combine(workRoot, "_extract");
        var downloadPath = Path.Combine(workRoot, filename);
        var backupEntryDir = Path.Combine(cacheRoot, $"{BackupPrefix}{cacheEntryName}_{Guid.NewGuid():N}");

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
                stageStatus = StageStatus("download", Failed(download.ErrorCode), "skipped", "skipped", "skipped");
                return TryResolveFallback(request, cacheRoot, cacheEntryName, filename, stageStatus, out var fallback)
                    ? fallback
                    : FailedState(filename, download.ErrorCode, stageStatus);
            }

            stageStatus = StageStatus("download", "ok", ResolveShaStatus(download), "pending", "pending");
            var materializeError = await MaterializePayloadAsync(
                downloadPath,
                stagingEntryDir,
                extractRoot,
                request.AllowDirectPayloadFile,
                cancellationToken);
            if (!string.IsNullOrWhiteSpace(materializeError))
            {
                stageStatus = stageStatus with { Folder = Failed(materializeError), Json = "skipped" };
                return TryResolveFallback(request, cacheRoot, cacheEntryName, filename, stageStatus, out var fallback)
                    ? fallback
                    : FailedState(filename, materializeError, stageStatus);
            }

            if (!request.Validator.IsValid(stagingEntryDir, out var validationError))
            {
                stageStatus = stageStatus with { Folder = Failed(validationError), Json = "skipped" };
                return TryResolveFallback(request, cacheRoot, cacheEntryName, filename, stageStatus, out var fallback)
                    ? fallback
                    : FailedState(filename, validationError, stageStatus);
            }

            Promote(stagingEntryDir, finalEntryDir, backupEntryDir);
            stageStatus = stageStatus with { Folder = "ok" };
            CleanupRetention(request, cacheRoot, cacheEntryName);
            _manifestStore.WriteEntry(
                assetKey,
                manifestVersion,
                filename,
                PayloadCacheKind,
                cacheEntryName);
            stageStatus = stageStatus with { Json = "ok" };

            return ReadyState(filename, finalEntryDir, stageStatus);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            RestoreBackup(finalEntryDir, backupEntryDir);
            stageStatus = EnsureFailureStatus(stageStatus, "prepare_failed");
            return TryResolveFallback(request, cacheRoot, cacheEntryName, filename, stageStatus, out var fallback)
                ? fallback
                : FailedState(filename, "prepare_failed", stageStatus);
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

    private async Task<string> MaterializePayloadAsync(
        string downloadPath,
        string stagingEntryDir,
        string extractRoot,
        bool allowDirectPayloadFile,
        CancellationToken cancellationToken)
    {
        var extension = Path.GetExtension(downloadPath);
        if (allowDirectPayloadFile && !InstallerArchiveExtensions.IsArchive(extension))
        {
            File.Copy(downloadPath, Path.Combine(stagingEntryDir, Path.GetFileName(downloadPath)), overwrite: true);
            return "";
        }

        if (!InstallerArchiveExtensions.IsArchive(extension))
        {
            return allowDirectPayloadFile ? "prepare_failed" : "unsupported_archive";
        }

        if (ShouldInvalidateZip(downloadPath))
        {
            return "invalid_zip";
        }

        Directory.CreateDirectory(extractRoot);
        var extract = await _extractor.ExtractAsync(downloadPath, extractRoot, cancellationToken);
        if (!extract.IsSuccess)
        {
            return string.IsNullOrWhiteSpace(extract.ErrorCode) ? "extract_failed" : extract.ErrorCode;
        }

        var source = ArchivePreparationHelpers.ResolvePayloadSourceDirectory(extractRoot);
        CopyDirectory(source, stagingEntryDir);
        return "";
    }

    private bool TryResolveFallback(
        ExtractedArchivePayloadCacheRequest request,
        string cacheRoot,
        string cacheEntryName,
        string filename,
        ArchivePreparationStageStatus stageStatus,
        out ArchivePreparationState state)
    {
        state = new ArchivePreparationState();
        if (!request.AllowExistingFallback)
        {
            return false;
        }

        foreach (var candidate in ResolveFallbackCandidates(request, cacheRoot, cacheEntryName))
        {
            if (!request.Validator.IsValid(candidate, out _))
            {
                continue;
            }

            state = ReadyState(filename, candidate, FallbackStatus(stageStatus));
            return true;
        }

        return false;
    }

    private IEnumerable<string> ResolveFallbackCandidates(
        ExtractedArchivePayloadCacheRequest request,
        string cacheRoot,
        string cacheEntryName)
    {
        var candidates = new List<string>
        {
            Path.Combine(cacheRoot, cacheEntryName)
        };
        var assetKey = ResolveAssetKey(request);
        var manifestEntry = _manifestStore.TryGetEntry(assetKey);
        if (manifestEntry is not null
            && string.Equals((manifestEntry.CacheKind ?? "").Trim(), PayloadCacheKind, StringComparison.Ordinal))
        {
            AddCandidate(candidates, Path.Combine(cacheRoot, (manifestEntry.CacheEntry ?? "").Trim()));
        }

        if (Directory.Exists(cacheRoot))
        {
            foreach (var directory in Directory
                         .EnumerateDirectories(cacheRoot)
                         .Where(static path => !Path.GetFileName(path).StartsWith(".", StringComparison.Ordinal))
                         .OrderByDescending(static path => Directory.GetLastWriteTimeUtc(path)))
            {
                AddCandidate(candidates, directory);
            }
        }

        return candidates;
    }

    private void CleanupRetention(
        ExtractedArchivePayloadCacheRequest request,
        string cacheRoot,
        string keepEntryName)
    {
        if (!request.EnableRetentionCleanup || request.RetentionCount <= 0 || !Directory.Exists(cacheRoot))
        {
            return;
        }

        var directories = Directory
            .EnumerateDirectories(cacheRoot)
            .Where(static path => !Path.GetFileName(path).StartsWith(".", StringComparison.Ordinal))
            .Select(static path => new DirectoryInfo(path))
            .OrderByDescending(static info => info.LastWriteTimeUtc)
            .ToArray();
        var keep = directories
            .Where(info => string.Equals(info.Name, keepEntryName, StringComparison.OrdinalIgnoreCase))
            .Concat(directories.Where(info => !string.Equals(info.Name, keepEntryName, StringComparison.OrdinalIgnoreCase)))
            .Take(request.RetentionCount)
            .Select(static info => info.FullName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var directory in directories)
        {
            if (!keep.Contains(directory.FullName))
            {
                TryDeleteDirectory(directory.FullName);
            }
        }
    }

    private bool IsUpdateNeeded(string assetKey, string version)
    {
        return !string.IsNullOrWhiteSpace(version) && _manifestStore.IsUpdateNeeded(assetKey, version);
    }

    private static bool IsValidPayload(string payloadDirectory, IArchivePayloadValidator validator)
    {
        return validator.IsValid(payloadDirectory, out _);
    }

    private bool ShouldInvalidateZip(string path)
    {
        return _options.ValidateZipFiles
               && string.Equals(Path.GetExtension(path), ".zip", StringComparison.OrdinalIgnoreCase)
               && !ArchivePreparationHelpers.IsValidZipFile(path);
    }

    private static void Promote(string stagingEntryDir, string finalEntryDir, string backupEntryDir)
    {
        if (Directory.Exists(finalEntryDir))
        {
            Directory.Move(finalEntryDir, backupEntryDir);
        }

        Directory.Move(stagingEntryDir, finalEntryDir);
        if (Directory.Exists(backupEntryDir))
        {
            Directory.Delete(backupEntryDir, recursive: true);
        }
    }

    private static void RestoreBackup(string finalEntryDir, string backupEntryDir)
    {
        if (!Directory.Exists(backupEntryDir) || Directory.Exists(finalEntryDir))
        {
            return;
        }

        TryMoveDirectory(backupEntryDir, finalEntryDir);
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

    private static string ResolveAssetKey(ExtractedArchivePayloadCacheRequest request)
    {
        return string.IsNullOrWhiteSpace(request.AssetRuntimeDataKey)
            ? ArchiveAssetRuntimeDataKeys.ToRuntimeDataEntryKey(request.AssetKey)
            : request.AssetRuntimeDataKey.Trim();
    }

    private static string SanitizeFilename(string value)
    {
        var fileName = Path.GetFileName((value ?? "").Trim());
        return fileName is "." or ".." ? "" : fileName;
    }

    private static string ResolveManifestVersion(ExtractedArchivePayloadCacheRequest request, string cacheEntryName)
    {
        if (!string.IsNullOrWhiteSpace(request.ManifestVersion))
        {
            return request.ManifestVersion.Trim();
        }

        var entryVersion = (request.Entry.Version ?? "").Trim();
        return string.IsNullOrWhiteSpace(entryVersion) ? cacheEntryName : entryVersion;
    }

    private static void AddCandidate(ICollection<string> candidates, string path)
    {
        if (!string.IsNullOrWhiteSpace(path)
            && !candidates.Contains(path, StringComparer.OrdinalIgnoreCase))
        {
            candidates.Add(path);
        }
    }

    private static ArchivePreparationState ReadyState(
        string filename,
        string path,
        ArchivePreparationStageStatus stageStatus)
    {
        return new ArchivePreparationState
        {
            Filename = filename,
            ArchivePath = path,
            Ready = true,
            Downloading = false,
            StageStatus = stageStatus
        };
    }

    private static ArchivePreparationState MissingMetadataState(
        string label,
        string filename,
        ArchivePreparationStageStatus stageStatus)
    {
        var safeLabel = string.IsNullOrWhiteSpace(label) ? "archive" : label.Trim();
        return FailedState(filename, $"Missing {safeLabel} download metadata in sheet.", stageStatus);
    }

    private static ArchivePreparationState FailedState(
        string filename,
        string error,
        ArchivePreparationStageStatus stageStatus)
    {
        return new ArchivePreparationState
        {
            Filename = filename,
            Ready = false,
            Downloading = false,
            ErrorMessage = error,
            StageStatus = stageStatus
        };
    }

    private static ArchivePreparationStageStatus CachedStatus()
    {
        return StageStatus("cache", "cached", "cached", "cached", "ok");
    }

    private static ArchivePreparationStageStatus FallbackStatus(ArchivePreparationStageStatus status)
    {
        return status with
        {
            Source = "fallback",
            Folder = string.IsNullOrWhiteSpace(status.Folder) || string.Equals(status.Folder, "skipped", StringComparison.OrdinalIgnoreCase)
                ? "fallback"
                : status.Folder,
            Json = string.IsNullOrWhiteSpace(status.Json) ? "skipped" : status.Json
        };
    }

    private static ArchivePreparationStageStatus EnsureFailureStatus(
        ArchivePreparationStageStatus status,
        string errorCode)
    {
        if (string.IsNullOrWhiteSpace(status.Source))
        {
            return StageStatus("unknown", "skipped", "skipped", Failed(errorCode), "skipped");
        }

        if (string.Equals(status.Json, "pending", StringComparison.OrdinalIgnoreCase))
        {
            return status with { Json = Failed(errorCode) };
        }

        if (string.Equals(status.Folder, "pending", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(status.Folder))
        {
            return status with { Folder = Failed(errorCode), Json = string.IsNullOrWhiteSpace(status.Json) ? "skipped" : status.Json };
        }

        return status;
    }

    private static ArchivePreparationStageStatus StageStatus(
        string source,
        string download,
        string sha,
        string folder,
        string json)
    {
        return new ArchivePreparationStageStatus
        {
            Source = source,
            Download = download,
            Sha = sha,
            Folder = folder,
            Json = json
        };
    }

    private static string Failed(string code)
    {
        return string.IsNullOrWhiteSpace(code) ? "failed" : $"failed:{code.Trim()}";
    }

    private static string ResolveShaStatus(ArchiveDownloadResult download)
    {
        if (download is null || string.IsNullOrWhiteSpace(download.VerificationSource))
        {
            return "ok";
        }

        return string.Equals(download.VerificationSource, "not_configured", StringComparison.OrdinalIgnoreCase)
               || string.Equals(download.VerificationSource, "github_digest_unavailable", StringComparison.OrdinalIgnoreCase)
            ? "not_configured"
            : "ok";
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

    private static class InstallerArchiveExtensions
    {
        public static bool IsArchive(string extension)
        {
            return string.Equals(extension, ".zip", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(extension, ".7z", StringComparison.OrdinalIgnoreCase);
        }
    }
}
