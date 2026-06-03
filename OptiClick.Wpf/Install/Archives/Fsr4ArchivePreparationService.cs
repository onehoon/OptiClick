using System.IO;

namespace OptiClick.Wpf.Install.Archives;

public sealed class Fsr4ArchivePreparationService
{
    private readonly IArchiveDownloader _downloader;
    private readonly ArchivePreparationOptions _options;

    public Fsr4ArchivePreparationService(IArchiveDownloader downloader, ArchivePreparationOptions? options = null)
    {
        _downloader = downloader;
        _options = options ?? new ArchivePreparationOptions();
    }

    public async Task<ArchivePreparationState> PrepareAsync(
        RemoteArchiveEntry entry,
        string cacheDirectory,
        bool enabled,
        CancellationToken cancellationToken = default)
    {
        if (!enabled)
        {
            return new ArchivePreparationState
            {
                Ready = false,
                Downloading = false
            };
        }

        var filename = (entry.Filename ?? "").Trim();
        var url = (entry.Url ?? "").Trim();
        if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(filename))
        {
            return new ArchivePreparationState
            {
                Filename = filename,
                Ready = false,
                Downloading = false,
                ErrorMessage = "Missing FSR4 download metadata in sheet."
            };
        }

        Directory.CreateDirectory(cacheDirectory);
        var cachePath = Path.Combine(cacheDirectory, filename);
        if (File.Exists(cachePath))
        {
            if (string.Equals(Path.GetExtension(cachePath), ".zip", StringComparison.OrdinalIgnoreCase)
                && !ArchivePreparationHelpers.IsValidZipFile(cachePath))
            {
                ArchivePreparationHelpers.TryDeleteFile(cachePath);
            }
            else
            {
                return new ArchivePreparationState
                {
                    Filename = filename,
                    ArchivePath = cachePath,
                    Ready = true
                };
            }
        }

        if (_options.CleanupStaleFiles)
        {
            ArchivePreparationHelpers.CleanupStaleArchives(cacheDirectory, filename);
        }

        var result = await _downloader.DownloadAsync(url, cachePath, _options.DownloadTimeout, cancellationToken);
        if (!result.IsSuccess)
        {
            return new ArchivePreparationState
            {
                Filename = filename,
                Ready = false,
                ErrorMessage = result.ErrorCode
            };
        }

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

        return new ArchivePreparationState
        {
            Filename = filename,
            ArchivePath = cachePath,
            Ready = true
        };
    }
}
