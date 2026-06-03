using System.IO;

namespace OptiClick.Wpf.Install.Archives;

public sealed class OptiPatcherArchivePreparationService
{
    private readonly IArchiveDownloader _downloader;
    private readonly IArchiveDownloadManifestStore _manifestStore;
    private readonly ArchivePreparationOptions _options;

    public OptiPatcherArchivePreparationService(
        IArchiveDownloader downloader,
        IArchiveDownloadManifestStore manifestStore,
        ArchivePreparationOptions? options = null)
    {
        _downloader = downloader;
        _manifestStore = manifestStore;
        _options = options ?? new ArchivePreparationOptions();
    }

    public async Task<ArchivePreparationState> PrepareAsync(
        RemoteArchiveEntry entry,
        string cacheDirectory,
        CancellationToken cancellationToken = default)
    {
        var filename = (entry.Filename ?? "").Trim();
        var url = (entry.Url ?? "").Trim();
        var version = (entry.Version ?? "").Trim();
        Directory.CreateDirectory(cacheDirectory);

        var cachePath = string.IsNullOrWhiteSpace(filename)
            ? ""
            : Path.Combine(cacheDirectory, filename);

        if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(filename))
        {
            if (!string.IsNullOrWhiteSpace(cachePath) && File.Exists(cachePath))
            {
                return new ArchivePreparationState
                {
                    Filename = filename,
                    ArchivePath = cachePath,
                    Ready = true
                };
            }

            return new ArchivePreparationState
            {
                Filename = filename,
                Ready = false,
                ErrorMessage = "Missing OptiPatcher archive download metadata in sheet."
            };
        }

        var downloadResult = await _downloader.DownloadAsync(url, cachePath, _options.DownloadTimeout, cancellationToken);
        if (downloadResult.IsSuccess)
        {
            if (string.Equals(Path.GetExtension(cachePath), ".zip", StringComparison.OrdinalIgnoreCase)
                && !ArchivePreparationHelpers.IsValidZipFile(cachePath))
            {
                ArchivePreparationHelpers.TryDeleteFile(cachePath);
                return new ArchivePreparationState
                {
                    Filename = filename,
                    Ready = false,
                    ErrorMessage = "invalid_zip"
                };
            }

            if (!string.IsNullOrWhiteSpace(version))
            {
                _manifestStore.WriteEntry(
                    ArchiveAssetRuntimeDataKeys.OptiPatcher,
                    version,
                    filename);
            }

            return new ArchivePreparationState
            {
                Filename = filename,
                ArchivePath = cachePath,
                Ready = true
            };
        }

        if (File.Exists(cachePath))
        {
            return new ArchivePreparationState
            {
                Filename = filename,
                ArchivePath = cachePath,
                Ready = true
            };
        }

        return new ArchivePreparationState
        {
            Filename = filename,
            Ready = false,
            ErrorMessage = downloadResult.ErrorCode
        };
    }
}
