namespace OptiClick.Wpf.Install.Archives;

public sealed class OptiPatcherArchivePreparationService
{
    private readonly ExtractedArchivePayloadCacheService _payloadCacheService;

    public OptiPatcherArchivePreparationService(
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
                AssetKey = ArchiveAssetKey.OptiPatcher,
                AssetRuntimeDataKey = ArchiveAssetRuntimeDataKeys.OptiPatcher,
                AssetLabel = "OptiPatcher archive",
                Entry = entry,
                CacheRoot = cacheDirectory,
                CacheEntryName = ArchivePayloadCacheEntryNames.OptiPatcherRolling,
                Validator = new OptiPatcherPayloadValidator(),
                ForceRefresh = true,
                AllowExistingFallback = true,
                AllowDirectPayloadFile = true,
                EnableRetentionCleanup = false,
                ManifestVersion = ArchivePayloadCacheEntryNames.OptiPatcherRolling
            },
            cancellationToken);
    }
}
