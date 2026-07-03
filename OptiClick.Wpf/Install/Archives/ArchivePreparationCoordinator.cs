using System.Diagnostics;
using OptiClick.Wpf.Install.Flow;

namespace OptiClick.Wpf.Install.Archives;

public interface IArchivePreparationCoordinator
{
    Task<ArchivePreparationSnapshot> PrepareStartupArchivesAsync(
        ModuleDownloadLinkContext moduleDownloadLinks,
        CancellationToken cancellationToken = default);
}

public sealed class ArchivePreparationCoordinator : IArchivePreparationCoordinator
{
    private readonly ArchiveCachePaths _cachePaths;
    private readonly IVersionedArchivePreparationService _versionedService;
    private readonly OptiPatcherArchivePreparationService _optiPatcherService;

    public ArchivePreparationCoordinator(
        ArchiveCachePaths cachePaths,
        IVersionedArchivePreparationService versionedService,
        OptiPatcherArchivePreparationService optiPatcherService)
    {
        _cachePaths = cachePaths;
        _versionedService = versionedService;
        _optiPatcherService = optiPatcherService;
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
            ArchiveAssetKey.Unreal5 => "Unreal5 patch archive",
            ArchiveAssetKey.Fsr4 => "FSR4 archive",
            ArchiveAssetKey.Amdxc64 => "amdxc64 archive",
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
