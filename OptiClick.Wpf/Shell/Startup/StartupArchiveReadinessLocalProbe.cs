using System.IO;
using OptiClick.Infrastructure.FileSystem;
using OptiClick.Wpf.Install.Archives;
using OptiClick.Wpf.Install.Flow;
using OptiClick.Wpf.Install.Planning;
using OptiClick.Wpf.Shell.RuntimeData;

namespace OptiClick.Wpf.Shell.Startup;

internal static class StartupArchiveReadinessLocalProbe
{
    public static bool TryBuildReadySnapshot(
        IAppLocalDataPathProvider pathProvider,
        ModuleDownloadLinkContext moduleDownloadLinks,
        out ArchiveReadinessSnapshot readiness)
    {
        return TryBuildReadySnapshot(
            pathProvider,
            moduleDownloadLinks,
            Fsr4VariantCatalog.Empty,
            out readiness);
    }

    public static bool TryBuildReadySnapshot(
        IAppLocalDataPathProvider pathProvider,
        ModuleDownloadLinkContext moduleDownloadLinks,
        Fsr4VariantCatalog fsr4VariantCatalog,
        out ArchiveReadinessSnapshot readiness)
    {
        readiness = ArchiveReadinessSnapshot.NotReady;
        var linkContext = moduleDownloadLinks ?? ModuleDownloadLinkContext.Empty;
        if (pathProvider is null || !linkContext.Catalog.HasLinks)
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
                    GetEntry(linkContext, ArchiveAssetRuntimeDataKeys.OptiScaler))
            };

            foreach (var key in ArchivePreparationSequence.DefaultStartupOrder)
            {
                states[key] = ResolvePayloadState(
                    cachePaths,
                    manifestStore,
                    GetEntry(linkContext, ArchiveAssetRuntimeDataKeys.ToRuntimeDataEntryKey(key)),
                    key);
            }

            var snapshot = new ArchivePreparationSnapshot
            {
                States = states,
                Fsr4VariantStates = ResolveFsr4VariantStates(
                    cachePaths,
                    manifestStore,
                    fsr4VariantCatalog)
            };
            states[ArchiveAssetKey.Fsr4] = BuildFsr4AggregateState(snapshot.Fsr4VariantStates);
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
        ModuleDownloadLinkEntry? rawEntry)
    {
        var entry = ArchiveEntryNormalizer.Normalize(rawEntry);
        var candidates = new List<string>();
        var expectedEntryName = ArchivePayloadCacheEntryNames.ResolveOptiScalerEntryName(entry);
        var expectedVersion = ArchiveEntryNormalizer.ResolveOptiScalerCacheVersion(entry);
        var manifestEntry = manifestStore.TryGetEntry(ArchiveAssetRuntimeDataKeys.OptiScaler);
        if (IsCurrentPayloadManifestEntry(manifestEntry, expectedVersion, expectedEntryName))
        {
            AddCandidate(candidates, Path.Combine(cachePaths.OptiScalerPayloadCacheRoot, manifestEntry!.CacheEntry.Trim()));
        }

        AddCandidate(
            candidates,
            Path.Combine(
                cachePaths.OptiScalerPayloadCacheRoot,
                expectedEntryName));

        foreach (var candidate in candidates)
        {
            if (validator.IsValid(candidate, out _))
            {
                return ReadyState(entry.Filename, candidate);
            }
        }

        return MissingState(entry.Filename);
    }

    private static ArchivePreparationState ResolvePayloadState(
        ArchiveCachePaths cachePaths,
        IArchiveDownloadManifestStore manifestStore,
        ModuleDownloadLinkEntry? rawEntry,
        ArchiveAssetKey key)
    {
        var entry = ArchiveEntryNormalizer.Normalize(rawEntry);
        var cacheRoot = cachePaths.ResolveCacheDirectory(key);
        var validator = CreateValidator(key);
        var candidates = new List<string>();
        var expectedEntryName = ResolveExpectedEntryName(entry, key);
        var expectedVersion = ResolveExpectedManifestVersion(entry, key, expectedEntryName);
        var manifestEntry = manifestStore.TryGetEntry(ArchiveAssetRuntimeDataKeys.ToRuntimeDataEntryKey(key));
        if (IsCurrentPayloadManifestEntry(manifestEntry, expectedVersion, expectedEntryName))
        {
            AddCandidate(candidates, Path.Combine(cacheRoot, manifestEntry!.CacheEntry.Trim()));
        }

        AddCandidate(candidates, Path.Combine(cacheRoot, expectedEntryName));

        foreach (var candidate in candidates)
        {
            if (validator.IsValid(candidate, out _))
            {
                return ReadyState(entry.Filename, candidate);
            }
        }

        return MissingState(entry.Filename);
    }

    private static IReadOnlyDictionary<string, ArchivePreparationState> ResolveFsr4VariantStates(
        ArchiveCachePaths cachePaths,
        IArchiveDownloadManifestStore manifestStore,
        Fsr4VariantCatalog? fsr4VariantCatalog)
    {
        var catalog = fsr4VariantCatalog ?? Fsr4VariantCatalog.Empty;
        var states = new Dictionary<string, ArchivePreparationState>(StringComparer.OrdinalIgnoreCase);
        foreach (var option in catalog.Options)
        {
            states[option.Variant] = ResolveFsr4VariantPayloadState(
                cachePaths,
                manifestStore,
                option,
                option.Variant);
        }

        return states;
    }

    private static ArchivePreparationState ResolveFsr4VariantPayloadState(
        ArchiveCachePaths cachePaths,
        IArchiveDownloadManifestStore manifestStore,
        Fsr4VariantOption? option,
        string variant)
    {
        if (option is null)
        {
            return MissingState("");
        }

        var entry = option.ToRemoteArchiveEntry();
        var cacheRoot = cachePaths.Fsr4CacheDir;
        var validator = new SingleExtensionPayloadValidator(".dll");
        var candidates = new List<string>();
        var expectedEntryName = ArchivePayloadCacheEntryNames.ResolveVersionedEntryName(entry, $"FSR4-{variant}");
        var expectedVersion = ResolveExpectedManifestVersion(entry, ArchiveAssetKey.Fsr4, expectedEntryName);
        var manifestEntry = manifestStore.TryGetEntry(ArchiveAssetRuntimeDataKeys.ToFsr4VariantKey(variant));
        if (IsCurrentPayloadManifestEntry(manifestEntry, expectedVersion, expectedEntryName))
        {
            AddCandidate(candidates, Path.Combine(cacheRoot, manifestEntry!.CacheEntry.Trim()));
        }

        AddCandidate(candidates, Path.Combine(cacheRoot, expectedEntryName));

        foreach (var candidate in candidates)
        {
            if (validator.IsValid(candidate, out _))
            {
                return ReadyState(entry.Filename, candidate);
            }
        }

        return MissingState(entry.Filename);
    }

    private static ArchivePreparationState BuildFsr4AggregateState(
        IReadOnlyDictionary<string, ArchivePreparationState> states)
    {
        return states.Count > 0 && states.Values.All(static state => state.Ready)
            ? ReadyState("fsr4_variants", "")
            : MissingState("fsr4_variants");
    }

    private static string ResolveExpectedEntryName(RemoteArchiveEntry entry, ArchiveAssetKey key)
    {
        return key == ArchiveAssetKey.OptiPatcher
            ? ArchivePayloadCacheEntryNames.OptiPatcherRolling
            : ArchivePayloadCacheEntryNames.ResolveVersionedEntryName(entry, ArchiveAssetRuntimeDataKeys.ToStateKey(key));
    }

    private static string ResolveExpectedManifestVersion(RemoteArchiveEntry entry, ArchiveAssetKey key, string expectedEntryName)
    {
        if (key == ArchiveAssetKey.OptiPatcher)
        {
            return ArchivePayloadCacheEntryNames.OptiPatcherRolling;
        }

        var entryVersion = (entry.Version ?? "").Trim();
        return string.IsNullOrWhiteSpace(entryVersion) ? expectedEntryName : entryVersion;
    }

    private static bool IsCurrentPayloadManifestEntry(
        ArchiveManifestEntry? manifestEntry,
        string expectedVersion,
        string expectedEntryName)
    {
        if (manifestEntry is null
            || !string.Equals((manifestEntry.CacheKind ?? "").Trim(), "payload_dir", StringComparison.Ordinal))
        {
            return false;
        }

        return string.Equals((manifestEntry.Version ?? "").Trim(), (expectedVersion ?? "").Trim(), StringComparison.Ordinal)
               && string.Equals((manifestEntry.CacheEntry ?? "").Trim(), (expectedEntryName ?? "").Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static IArchivePayloadValidator CreateValidator(ArchiveAssetKey key)
    {
        return key switch
        {
            ArchiveAssetKey.Fsr4 => new SingleExtensionPayloadValidator(".dll"),
            ArchiveAssetKey.OptiPatcher => new OptiPatcherPayloadValidator(),
            ArchiveAssetKey.SpecialK => new RequiredFilesPayloadValidator(["SpecialK64.dll"]),
            ArchiveAssetKey.ReFramework => new RequiredFilesPayloadValidator(["dinput8.dll"]),
            ArchiveAssetKey.UltimateAsiLoader => new RequiredFilesPayloadValidator(["dinput8.dll"]),
            ArchiveAssetKey.Unreal5 => new NonEmptyPayloadValidator(),
            _ => new NonEmptyPayloadValidator()
        };
    }

    private static ModuleDownloadLinkEntry? GetEntry(ModuleDownloadLinkContext moduleDownloadLinks, string key)
    {
        return moduleDownloadLinks.TryResolveLink(key, out var entry) ? entry : null;
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
