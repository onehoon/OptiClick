using OptiClick.Wpf.Shell.RuntimeData;

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
        return await PrepareAsync(entry, cacheDirectory, "", cancellationToken);
    }

    public async Task<ArchivePreparationState> PrepareAsync(
        RemoteArchiveEntry entry,
        string cacheDirectory,
        string variant,
        CancellationToken cancellationToken = default)
    {
        var normalizedVariant = Fsr4VariantCatalogBuilder.NormalizeVariant(variant);
        var assetRuntimeDataKey = string.IsNullOrWhiteSpace(normalizedVariant)
            ? ArchiveAssetRuntimeDataKeys.Fsr4
            : ArchiveAssetRuntimeDataKeys.ToFsr4VariantKey(normalizedVariant);
        var assetLabel = string.IsNullOrWhiteSpace(normalizedVariant)
            ? "FSR4 archive"
            : $"FSR4 archive ({normalizedVariant})";
        var cacheFallback = string.IsNullOrWhiteSpace(normalizedVariant)
            ? "FSR4"
            : $"FSR4-{normalizedVariant}";

        return await _payloadCacheService.PrepareAsync(
            new ExtractedArchivePayloadCacheRequest
            {
                AssetKey = ArchiveAssetKey.Fsr4,
                AssetRuntimeDataKey = assetRuntimeDataKey,
                AssetLabel = assetLabel,
                Entry = entry,
                CacheRoot = cacheDirectory,
                CacheEntryName = ArchivePayloadCacheEntryNames.ResolveVersionedEntryName(entry, cacheFallback),
                Validator = new SingleExtensionPayloadValidator(".dll"),
                EnableRetentionCleanup = true,
                RetentionCount = 4
            },
            cancellationToken);
    }
}
