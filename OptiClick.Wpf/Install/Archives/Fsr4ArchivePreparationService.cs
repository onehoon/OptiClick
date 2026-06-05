namespace OptiClick.Wpf.Install.Archives;

public sealed class Fsr4ArchivePreparationService
{
    private readonly ExtractedArchivePayloadCacheService _payloadCacheService;

    public Fsr4ArchivePreparationService(
        IArchiveDownloader downloader,
        IArchiveExtractor extractor,
        IArchiveDownloadManifestStore manifestStore,
        ArchivePreparationOptions? options = null)
    {
        _payloadCacheService = new ExtractedArchivePayloadCacheService(
            downloader,
            extractor,
            manifestStore,
            options);
    }

    public async Task<ArchivePreparationState> PrepareAsync(
        RemoteArchiveEntry entry,
        string cacheDirectory,
        CancellationToken cancellationToken = default)
    {
        return await _payloadCacheService.PrepareAsync(
            new ExtractedArchivePayloadCacheRequest
            {
                AssetKey = ArchiveAssetKey.Fsr4,
                AssetRuntimeDataKey = ArchiveAssetRuntimeDataKeys.Fsr4,
                AssetLabel = "FSR4 archive",
                Entry = entry,
                CacheRoot = cacheDirectory,
                CacheEntryName = ArchivePayloadCacheEntryNames.ResolveVersionedEntryName(entry, "FSR4"),
                Validator = new SingleExtensionPayloadValidator(".dll"),
                EnableRetentionCleanup = true,
                RetentionCount = 2
            },
            cancellationToken);
    }
}
