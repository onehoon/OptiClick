using System.Threading;
using System.Threading.Tasks;
using OptiClick.Wpf.Shell.RuntimeData;

namespace OptiClick.Wpf.Services;

public sealed record GameMasterCoverPrefetchTarget
{
    public string GameId { get; init; } = "";
    public string SourceUrl { get; init; } = "";
    public string SteamAppId { get; init; } = "";
}

public sealed record GameMasterCoverPrefetchSummary(
    int Total,
    int Cached,
    int Downloaded,
    int Skipped,
    int Failed);

public interface IGameMasterCoverPrefetchService
{
    Task<GameMasterCoverPrefetchSummary> PrefetchAsync(
        IReadOnlyList<RuntimeDataGameProfile>? gameMaster,
        CancellationToken cancellationToken = default);

    Task<GameMasterCoverPrefetchSummary> PrefetchAsync(
        IReadOnlyList<GameMasterCoverPrefetchTarget> prioritizedTargets,
        IReadOnlyList<GameMasterCoverPrefetchTarget> backgroundTargets,
        CancellationToken cancellationToken = default);
}
