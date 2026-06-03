using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OptiClick.Wpf.Shell.RuntimeData;

namespace OptiClick.Wpf.Services;

public sealed class GameMasterCoverPrefetchService : IGameMasterCoverPrefetchService
{
    private const int DefaultMaxDegreeOfParallelism = 2;

    public async Task<GameMasterCoverPrefetchSummary> PrefetchAsync(
        IReadOnlyList<RuntimeDataGameProfile>? gameMaster,
        CancellationToken cancellationToken = default)
    {
        var targets = CollectTargets(gameMaster);
        if (targets.Count == 0)
        {
            return new GameMasterCoverPrefetchSummary(0, 0, 0, 0, 0);
        }

        return await PrefetchAsync(
            [],
            targets,
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<GameMasterCoverPrefetchSummary> PrefetchAsync(
        IReadOnlyList<GameMasterCoverPrefetchTarget> prioritizedTargets,
        IReadOnlyList<GameMasterCoverPrefetchTarget> backgroundTargets,
        CancellationToken cancellationToken = default)
    {
        var primary = NormalizeTargets(prioritizedTargets);
        var secondary = RemoveKnownTargets(primary, backgroundTargets);

        var total = primary.Count + secondary.Count;
        if (total == 0)
        {
            return new GameMasterCoverPrefetchSummary(0, 0, 0, 0, 0);
        }

        var primarySummary = await PrefetchTargetsAsync(primary, cancellationToken).ConfigureAwait(false);
        var secondarySummary = await PrefetchTargetsAsync(secondary, cancellationToken).ConfigureAwait(false);

        return new GameMasterCoverPrefetchSummary(
            total,
            primarySummary.Cached + secondarySummary.Cached,
            primarySummary.Downloaded + secondarySummary.Downloaded,
            primarySummary.Skipped + secondarySummary.Skipped,
            primarySummary.Failed + secondarySummary.Failed);
    }

    private static IReadOnlyList<GameMasterCoverPrefetchTarget> NormalizeTargets(IReadOnlyList<GameMasterCoverPrefetchTarget>? targets)
    {
        if (targets is null or { Count: 0 })
        {
            return [];
        }

        var normalized = new List<GameMasterCoverPrefetchTarget>(targets.Count);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var target in targets)
        {
            if (target is null)
            {
                continue;
            }

            var sourceUrl = (target.SourceUrl ?? "").Trim();
            var steamAppId = (target.SteamAppId ?? "").Trim();
            if (string.IsNullOrWhiteSpace(sourceUrl))
            {
                continue;
            }

            var dedupeKey = string.IsNullOrWhiteSpace(steamAppId)
                ? $"url::{sourceUrl}"
                : $"steam::{steamAppId}";
            if (!seen.Add(dedupeKey))
            {
                continue;
            }

            normalized.Add(new GameMasterCoverPrefetchTarget
            {
                GameId = target.GameId ?? "",
                SourceUrl = sourceUrl,
                SteamAppId = steamAppId
            });
        }

        return normalized;
    }

    private static IReadOnlyList<GameMasterCoverPrefetchTarget> RemoveKnownTargets(
        IReadOnlyList<GameMasterCoverPrefetchTarget> existingTargets,
        IReadOnlyList<GameMasterCoverPrefetchTarget>? candidates)
    {
        if (candidates is null or { Count: 0 })
        {
            return [];
        }

        if (existingTargets.Count == 0)
        {
            return NormalizeTargets(candidates);
        }

        var excluded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var existing in existingTargets)
        {
            if (string.IsNullOrWhiteSpace(existing.SourceUrl))
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(existing.SteamAppId))
            {
                excluded.Add($"steam::{existing.SteamAppId}");
            }

            excluded.Add($"url::{existing.SourceUrl}");
        }

        return [..NormalizeTargets(candidates)
            .Where(
                candidate =>
                {
                    if (string.IsNullOrWhiteSpace(candidate.SourceUrl))
                    {
                        return false;
                    }

                    var steamIdKey = !string.IsNullOrWhiteSpace(candidate.SteamAppId)
                        ? $"steam::{candidate.SteamAppId}"
                        : string.Empty;
                    return !excluded.Contains($"url::{candidate.SourceUrl}") &&
                           (string.IsNullOrWhiteSpace(steamIdKey) || !excluded.Contains(steamIdKey));
                })];
    }

    private async Task<GameMasterCoverPrefetchSummary> PrefetchTargetsAsync(
        IReadOnlyList<GameMasterCoverPrefetchTarget> targets,
        CancellationToken cancellationToken)
    {
        var total = targets.Count;
        if (total == 0)
        {
            return new GameMasterCoverPrefetchSummary(0, 0, 0, 0, 0);
        }

        var cached = 0;
        var downloaded = 0;
        var skipped = 0;
        var failed = 0;

        await Parallel.ForEachAsync<GameMasterCoverPrefetchTarget>(
            targets,
            new ParallelOptions
            {
                MaxDegreeOfParallelism = DefaultMaxDegreeOfParallelism,
                CancellationToken = cancellationToken
            },
            async (target, ct) => await PrefetchTargetAsync(target, ct).ConfigureAwait(false)).ConfigureAwait(false);

        return new GameMasterCoverPrefetchSummary(
            total,
            cached,
            downloaded,
            skipped,
            failed);

        async Task PrefetchTargetAsync(
            GameMasterCoverPrefetchTarget target,
            CancellationToken ct)
        {
            try
            {
                if (CoverImageCacheService.IsCached(target.SourceUrl, target.SteamAppId))
                {
                    Interlocked.Increment(ref cached);
                    return;
                }

                var cachedPath = await CoverImageCacheService.EnsureCachedAsync(target.SourceUrl, target.SteamAppId, ct).ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(cachedPath))
                {
                    Interlocked.Increment(ref downloaded);
                    return;
                }

                Interlocked.Increment(ref failed);
            }
            catch (OperationCanceledException)
            {
                Interlocked.Increment(ref skipped);
            }
            catch
            {
                Interlocked.Increment(ref failed);
            }
        }
    }

    internal static IReadOnlyList<GameMasterCoverPrefetchTarget> CollectTargets(IReadOnlyList<RuntimeDataGameProfile>? gameMaster)
    {
        if (gameMaster is null or { Count: 0 })
        {
            return [];
        }

        var selected = new List<GameMasterCoverPrefetchTarget>(gameMaster.Count);
        var seenSteamIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var seenUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var profile in gameMaster)
        {
            if (profile is null)
            {
                continue;
            }

            var coverUrl = (profile.CoverUrl ?? "").Trim();
            var steamAppId = (profile.CoverSteamAppId ?? "").Trim();
            if (!string.IsNullOrWhiteSpace(steamAppId) && !seenSteamIds.Add(steamAppId))
            {
                continue;
            }

            var sourceUrl = GetCoverSourceUrl(coverUrl, steamAppId);
            if (!IsHttpUrl(sourceUrl))
            {
                continue;
            }

            if (!seenUrls.Add(sourceUrl))
            {
                continue;
            }

            selected.Add(new GameMasterCoverPrefetchTarget
            {
                GameId = (profile.GameId ?? "").Trim(),
                SourceUrl = sourceUrl,
                SteamAppId = steamAppId
            });
        }

        return selected;
    }

    private static string GetCoverSourceUrl(string coverUrl, string steamAppId)
    {
        if (IsHttpUrl(coverUrl))
        {
            return coverUrl;
        }

        return string.IsNullOrWhiteSpace(steamAppId)
            ? ""
            : CoverImageCacheService.BuildSteamCoverUrl(steamAppId);
    }

    private static bool IsHttpUrl(string value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out var uri)
               && (uri.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase)
                   || uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase));
    }
}
