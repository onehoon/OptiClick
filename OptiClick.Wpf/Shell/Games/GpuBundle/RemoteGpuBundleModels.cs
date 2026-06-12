using OptiClick.Core.Games.GpuBundle;

namespace OptiClick.Wpf.Shell.Games.GpuBundle;

public sealed class GpuBundleMergeResult
{
    public IReadOnlyDictionary<string, MergedGameInstallMetadata> MetadataByGameId { get; init; } =
        new Dictionary<string, MergedGameInstallMetadata>(StringComparer.OrdinalIgnoreCase);
    public int RuntimeGameCount { get; init; }
    public int BundleGameCount { get; init; }
    public int MatchedGameCount { get; init; }
    public int SupportedGameCount { get; init; }
    public IReadOnlyList<string> UnmatchedRuntimeGameIds { get; init; } = [];
    public IReadOnlyList<string> UnmatchedBundleGameIds { get; init; } = [];
}

public interface IGpuBundleGameDatabaseMerger
{
    GpuBundleMergeResult Merge(RemoteRuntimeData runtimeData, RemoteGpuBundle? bundle);
}
