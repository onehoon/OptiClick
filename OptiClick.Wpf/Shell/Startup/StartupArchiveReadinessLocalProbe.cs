using System.IO;
using OptiClick.Infrastructure.FileSystem;
using OptiClick.Wpf.Install.Archives;
using OptiClick.Wpf.Install.Planning;

namespace OptiClick.Wpf.Shell.Startup;

internal static class StartupArchiveReadinessLocalProbe
{
    public static bool TryBuildReadySnapshot(
        IAppLocalDataPathProvider pathProvider,
        IReadOnlyDictionary<string, object?> moduleDownloadLinks,
        out ArchiveReadinessSnapshot readiness)
    {
        readiness = ArchiveReadinessSnapshot.NotReady;
        if (pathProvider is null || moduleDownloadLinks.Count == 0)
        {
            return false;
        }

        try
        {
            var cachePaths = ArchiveCachePaths.CreateDefault(pathProvider);
            var manifestStore = new ArchiveDownloadManifestStore(cachePaths.ManifestRoot);
            var validator = new OptiScalerPayloadValidator();
            var states = new Dictionary<ArchiveAssetKey, ArchivePreparationState>
            {
                [ArchiveAssetKey.OptiScaler] = ResolveOptiScalerPayloadState(
                    cachePaths,
                    manifestStore,
                    validator,
                    GetEntry(moduleDownloadLinks, ArchiveAssetRuntimeDataKeys.OptiScaler))
            };

            foreach (var key in ArchivePreparationSequence.DefaultStartupOrder)
            {
                states[key] = ResolveArchiveFileState(
                    cachePaths,
                    GetEntry(moduleDownloadLinks, ArchiveAssetRuntimeDataKeys.ToRuntimeDataEntryKey(key)),
                    key);
            }

            var snapshot = new ArchivePreparationSnapshot
            {
                States = states
            };
            readiness = ArchivePreparationSnapshotMapper.ToInstallPlanSnapshot(snapshot);
            return states.Values.All(static state => state.Ready);
        }
        catch
        {
            readiness = ArchiveReadinessSnapshot.NotReady;
            return false;
        }
    }

    private static ArchivePreparationState ResolveOptiScalerPayloadState(
        ArchiveCachePaths cachePaths,
        IArchiveDownloadManifestStore manifestStore,
        OptiScalerPayloadValidator validator,
        object? rawEntry)
    {
        var entry = ArchiveEntryNormalizer.Normalize(rawEntry);
        var candidates = new List<string>();
        var manifestEntry = manifestStore.TryGetEntry(ArchiveAssetRuntimeDataKeys.OptiScaler);
        if (manifestEntry is not null
            && string.Equals((manifestEntry.CacheKind ?? "").Trim(), "payload_dir", StringComparison.Ordinal))
        {
            AddCandidate(candidates, Path.Combine(cachePaths.OptiScalerPayloadCacheRoot, (manifestEntry.CacheEntry ?? "").Trim()));
        }

        AddCandidate(
            candidates,
            Path.Combine(
                cachePaths.OptiScalerPayloadCacheRoot,
                ArchiveEntryNormalizer.ResolveOptiScalerCacheEntryName(entry)));

        if (Directory.Exists(cachePaths.OptiScalerPayloadCacheRoot))
        {
            foreach (var directory in Directory.EnumerateDirectories(cachePaths.OptiScalerPayloadCacheRoot))
            {
                AddCandidate(candidates, directory);
            }
        }

        foreach (var candidate in candidates)
        {
            if (validator.IsValid(candidate, out _))
            {
                return ReadyState(entry.Filename, candidate);
            }
        }

        return MissingState(entry.Filename);
    }

    private static ArchivePreparationState ResolveArchiveFileState(
        ArchiveCachePaths cachePaths,
        object? rawEntry,
        ArchiveAssetKey key)
    {
        var entry = ArchiveEntryNormalizer.Normalize(rawEntry);
        var filename = (entry.Filename ?? "").Trim();
        if (string.IsNullOrWhiteSpace(filename))
        {
            return MissingState(filename);
        }

        var path = Path.Combine(cachePaths.ResolveCacheDirectory(key), filename);
        return IsExistingArchiveFileReady(path)
            ? ReadyState(filename, path)
            : MissingState(filename);
    }

    private static bool IsExistingArchiveFileReady(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return false;
        }

        return !string.Equals(Path.GetExtension(path), ".zip", StringComparison.OrdinalIgnoreCase)
               || ArchivePreparationHelpers.IsValidZipFile(path);
    }

    private static object? GetEntry(IReadOnlyDictionary<string, object?> moduleDownloadLinks, string key)
    {
        return moduleDownloadLinks.TryGetValue(key, out var entry) ? entry : null;
    }

    private static void AddCandidate(ICollection<string> candidates, string path)
    {
        if (!string.IsNullOrWhiteSpace(path)
            && !candidates.Contains(path, StringComparer.OrdinalIgnoreCase))
        {
            candidates.Add(path);
        }
    }

    private static ArchivePreparationState ReadyState(string filename, string path)
    {
        return new ArchivePreparationState
        {
            Filename = filename,
            ArchivePath = path,
            Ready = true
        };
    }

    private static ArchivePreparationState MissingState(string filename)
    {
        return new ArchivePreparationState
        {
            Filename = filename,
            Ready = false
        };
    }
}
