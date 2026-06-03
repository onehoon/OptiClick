using System.IO;

namespace OptiClick.Wpf.Install.Archives;

public interface IVersionedArchivePreparationService
{
    Task<ArchivePreparationState> PrepareAsync(
        ArchiveAssetKey assetKey,
        string assetLabel,
        RemoteArchiveEntry entry,
        string cacheDirectory,
        CancellationToken cancellationToken = default);
}

public sealed class VersionedArchivePreparationService : IVersionedArchivePreparationService
{
    private readonly IArchiveDownloader _downloader;
    private readonly IArchiveDownloadManifestStore _manifestStore;
    private readonly ArchivePreparationOptions _options;

    public VersionedArchivePreparationService(
        IArchiveDownloader downloader,
        IArchiveDownloadManifestStore manifestStore,
        ArchivePreparationOptions? options = null)
    {
        _downloader = downloader;
        _manifestStore = manifestStore;
        _options = options ?? new ArchivePreparationOptions();
    }

    public async Task<ArchivePreparationState> PrepareAsync(
        ArchiveAssetKey assetKey,
        string assetLabel,
        RemoteArchiveEntry entry,
        string cacheDirectory,
        CancellationToken cancellationToken = default)
    {
        var label = string.IsNullOrWhiteSpace(assetLabel) ? "archive" : assetLabel.Trim();
        var filename = (entry.Filename ?? "").Trim();
        var url = (entry.Url ?? "").Trim();
        var version = (entry.Version ?? "").Trim();

        if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(filename))
        {
            return new ArchivePreparationState
            {
                Filename = filename,
                Ready = false,
                Downloading = false,
                ErrorMessage = $"Missing {label} download metadata in sheet."
            };
        }

        Directory.CreateDirectory(cacheDirectory);
        var cachePath = Path.Combine(cacheDirectory, filename);
        if (File.Exists(cachePath))
        {
            var updateNeeded = !string.IsNullOrWhiteSpace(version)
                               && _manifestStore.IsUpdateNeeded(ArchiveAssetRuntimeDataKeys.ToRuntimeDataEntryKey(assetKey), version);
            if (!updateNeeded)
            {
                if (ShouldInvalidateZip(cachePath))
                {
                    ArchivePreparationHelpers.TryDeleteFile(cachePath);
                }
                else
                {
                    return new ArchivePreparationState
                    {
                        Filename = filename,
                        ArchivePath = cachePath,
                        Ready = true,
                        Downloading = false
                    };
                }
            }
            else
            {
                ArchivePreparationHelpers.TryDeleteFile(cachePath);
                if (_options.CleanupStaleFiles)
                {
                    ArchivePreparationHelpers.CleanupStaleArchives(cacheDirectory, filename);
                }
            }
        }
        else if (_options.CleanupStaleFiles)
        {
            ArchivePreparationHelpers.CleanupStaleArchives(cacheDirectory, filename);
        }

        var downloadResult = await _downloader.DownloadAsync(
            url,
            cachePath,
            _options.DownloadTimeout,
            cancellationToken);
        if (!downloadResult.IsSuccess)
        {
            return new ArchivePreparationState
            {
                Filename = filename,
                Ready = false,
                Downloading = false,
                ErrorMessage = downloadResult.ErrorCode
            };
        }

        if (ShouldInvalidateZip(cachePath))
        {
            ArchivePreparationHelpers.TryDeleteFile(cachePath);
            return new ArchivePreparationState
            {
                Filename = filename,
                Ready = false,
                Downloading = false,
                ErrorMessage = "invalid_zip"
            };
        }

        if (!string.IsNullOrWhiteSpace(version))
        {
            _manifestStore.WriteEntry(
                ArchiveAssetRuntimeDataKeys.ToRuntimeDataEntryKey(assetKey),
                version,
                filename);
        }

        return new ArchivePreparationState
        {
            Filename = filename,
            ArchivePath = cachePath,
            Ready = true,
            Downloading = false
        };
    }

    private bool ShouldInvalidateZip(string path)
    {
        if (!_options.ValidateZipFiles)
        {
            return false;
        }

        return string.Equals(Path.GetExtension(path), ".zip", StringComparison.OrdinalIgnoreCase)
               && !ArchivePreparationHelpers.IsValidZipFile(path);
    }
}
