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
    private readonly ExtractedArchivePayloadCacheService _payloadCacheService;

    public VersionedArchivePreparationService(
        IArchiveDownloader downloader,
        IArchiveDownloadManifestStore manifestStore,
        IArchiveExtractor extractor,
        ArchivePreparationOptions? options = null)
    {
        _payloadCacheService = new ExtractedArchivePayloadCacheService(
            downloader,
            extractor,
            manifestStore,
            options);
    }

    public async Task<ArchivePreparationState> PrepareAsync(
        ArchiveAssetKey assetKey,
        string assetLabel,
        RemoteArchiveEntry entry,
        string cacheDirectory,
        CancellationToken cancellationToken = default)
    {
        return await _payloadCacheService.PrepareAsync(
            new ExtractedArchivePayloadCacheRequest
            {
                AssetKey = assetKey,
                AssetRuntimeDataKey = ArchiveAssetRuntimeDataKeys.ToRuntimeDataEntryKey(assetKey),
                AssetLabel = string.IsNullOrWhiteSpace(assetLabel) ? "archive" : assetLabel.Trim(),
                Entry = entry,
                CacheRoot = cacheDirectory,
                CacheEntryName = ArchivePayloadCacheEntryNames.ResolveVersionedEntryName(
                    entry,
                    ArchiveAssetRuntimeDataKeys.ToStateKey(assetKey)),
                Validator = CreateValidator(assetKey),
                EnableRetentionCleanup = true,
                RetentionCount = 2
            },
            cancellationToken);
    }

    private static IArchivePayloadValidator CreateValidator(ArchiveAssetKey assetKey)
    {
        return assetKey switch
        {
            ArchiveAssetKey.SpecialK => new RequiredFilesPayloadValidator(["SpecialK64.dll"]),
            ArchiveAssetKey.ReFramework => new RequiredFilesPayloadValidator(["dinput8.dll"]),
            ArchiveAssetKey.UltimateAsiLoader => new RequiredFilesPayloadValidator(["dinput8.dll"]),
            ArchiveAssetKey.Unreal5 => new NonEmptyPayloadValidator(),
            _ => new NonEmptyPayloadValidator()
        };
    }
}
