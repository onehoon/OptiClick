using System.Diagnostics;
using OptiClick.Wpf.Install.Flow;

namespace OptiClick.Wpf.Install.Archives;

public interface IArchivePreparationCoordinator
{
    Task<ArchivePreparationSnapshot> PrepareOptiScalerAsync(
        ModuleDownloadLinkContext moduleDownloadLinks,
        CancellationToken cancellationToken = default);

    Task<ArchivePreparationSnapshot> PrepareStartupArchivesAsync(
        ModuleDownloadLinkContext moduleDownloadLinks,
        CancellationToken cancellationToken = default);
}

public sealed class ArchivePreparationCoordinator : IArchivePreparationCoordinator
{
    private readonly ArchiveCachePaths _cachePaths;
    private readonly IVersionedArchivePreparationService _versionedService;
    private readonly OptiPatcherArchivePreparationService _optiPatcherService;
    private readonly Fsr4ArchivePreparationService _fsr4Service;
    private readonly IOptiScalerPayloadCacheService _optiScalerPayloadCacheService;

    public ArchivePreparationCoordinator(
        ArchiveCachePaths cachePaths,
        IVersionedArchivePreparationService versionedService,
        OptiPatcherArchivePreparationService optiPatcherService,
        Fsr4ArchivePreparationService fsr4Service,
        IOptiScalerPayloadCacheService optiScalerPayloadCacheService)
    {
        _cachePaths = cachePaths;
        _versionedService = versionedService;
        _optiPatcherService = optiPatcherService;
        _fsr4Service = fsr4Service;
        _optiScalerPayloadCacheService = optiScalerPayloadCacheService;
    }

    public async Task<ArchivePreparationSnapshot> PrepareOptiScalerAsync(
        ModuleDownloadLinkContext moduleDownloadLinks,
        CancellationToken cancellationToken = default)
    {
        _cachePaths.EnsureDirectories();
        var linkContext = moduleDownloadLinks ?? ModuleDownloadLinkContext.Empty;
        var entry = ArchiveEntryNormalizer.Normalize(GetEntry(linkContext, ArchiveAssetRuntimeDataKeys.OptiScaler));
        var stopwatch = Stopwatch.StartNew();
        var payloadResult = await _optiScalerPayloadCacheService.PrepareAsync(entry, _cachePaths.OptiScalerPayloadCacheRoot, cancellationToken);
        return new ArchivePreparationSnapshot
        {
            States = new Dictionary<ArchiveAssetKey, ArchivePreparationState>
            {
                [ArchiveAssetKey.OptiScaler] = new ArchivePreparationState
                {
                    Filename = entry.Filename,
                    ArchivePath = payloadResult.PayloadDirectory,
                    Ready = payloadResult.IsSuccess,
                    Downloading = false,
                    ErrorMessage = payloadResult.ErrorCode,
                    StageStatus = WithDuration(payloadResult.StageStatus, stopwatch.ElapsedMilliseconds)
                }
            }
        };
    }

    public async Task<ArchivePreparationSnapshot> PrepareStartupArchivesAsync(
        ModuleDownloadLinkContext moduleDownloadLinks,
        CancellationToken cancellationToken = default)
    {
        _cachePaths.EnsureDirectories();
        var linkContext = moduleDownloadLinks ?? ModuleDownloadLinkContext.Empty;
        var states = new Dictionary<ArchiveAssetKey, ArchivePreparationState>();

        foreach (var key in ArchivePreparationSequence.DefaultStartupOrder)
        {
            var entry = ArchiveEntryNormalizer.Normalize(GetEntry(linkContext, ArchiveAssetRuntimeDataKeys.ToRuntimeDataEntryKey(key)));
            var stopwatch = Stopwatch.StartNew();
            ArchivePreparationState state = key switch
            {
                ArchiveAssetKey.Fsr4 => await _fsr4Service.PrepareAsync(entry, _cachePaths.Fsr4CacheDir, cancellationToken),
                ArchiveAssetKey.OptiPatcher => await _optiPatcherService.PrepareAsync(entry, _cachePaths.OptiPatcherCacheDir, cancellationToken),
                _ => await _versionedService.PrepareAsync(
                    key,
                    ToAssetLabel(key),
                    entry,
                    _cachePaths.ResolveCacheDirectory(key),
                    cancellationToken)
            };
            states[key] = state with { StageStatus = WithDuration(state.StageStatus, stopwatch.ElapsedMilliseconds) };
        }

        return new ArchivePreparationSnapshot
        {
            States = states
        };
    }

    private static ModuleDownloadLinkEntry? GetEntry(ModuleDownloadLinkContext moduleDownloadLinks, string key)
    {
        return moduleDownloadLinks.TryResolveLink(key, out var entry) ? entry : null;
    }

    private static string ToAssetLabel(ArchiveAssetKey key)
    {
        return key switch
        {
            ArchiveAssetKey.SpecialK => "Special K archive",
            ArchiveAssetKey.ReFramework => "REFramework archive",
            ArchiveAssetKey.UltimateAsiLoader => "Ultimate ASI Loader archive",
            ArchiveAssetKey.Unreal5 => "Unreal5 patch archive",
            _ => "archive"
        };
    }

    private static ArchivePreparationStageStatus WithDuration(
        ArchivePreparationStageStatus status,
        long durationMs)
    {
        return (status ?? ArchivePreparationStageStatus.Unknown) with
        {
            DurationMs = durationMs
        };
    }
}
