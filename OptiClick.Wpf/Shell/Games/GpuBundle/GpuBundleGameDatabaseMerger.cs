using OptiClick.Wpf.Shell.RuntimeData;
using OptiClick.Wpf.Shell.Games;

namespace OptiClick.Wpf.Shell.Games.GpuBundle;

public sealed class GpuBundleGameDatabaseMerger : IGpuBundleGameDatabaseMerger
{
    private readonly RuntimeDataProfileCatalogBuilder _profileCatalogBuilder = new();
    private readonly RuntimeDataProfileCatalogAttacher _profileCatalogAttacher = new();

    public GpuBundleMergeResult Merge(RemoteRuntimeData runtimeData, RemoteGpuBundle? bundle)
    {
        var metadata = new Dictionary<string, MergedGameInstallMetadata>(StringComparer.OrdinalIgnoreCase);
        if (runtimeData is null || runtimeData.GameMaster.Count == 0)
        {
            var bundleOnlyIds = bundle?.GamesByGameId?.Keys
                .Select(static key => (key ?? "").Trim())
                .Where(static key => !string.IsNullOrWhiteSpace(key))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray() ?? [];
            return new GpuBundleMergeResult
            {
                MetadataByGameId = metadata,
                RuntimeGameCount = 0,
                BundleGameCount = bundleOnlyIds.Length,
                MatchedGameCount = 0,
                SupportedGameCount = 0,
                UnmatchedRuntimeGameIds = [],
                UnmatchedBundleGameIds = bundleOnlyIds
            };
        }

        var runtimeGameIds = runtimeData.GameMaster
            .Select(static game => (game?.GameId ?? "").Trim())
            .Where(static gameId => !string.IsNullOrWhiteSpace(gameId))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var bundleGameIds = bundle?.GamesByGameId?.Keys
            .Select(static key => (key ?? "").Trim())
            .Where(static key => !string.IsNullOrWhiteSpace(key))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];
        var matchedGameIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var supportedGameCount = 0;
        var profileCatalogs = _profileCatalogBuilder.Build(runtimeData);
        foreach (var game in runtimeData.GameMaster)
        {
            var gameId = (game?.GameId ?? "").Trim();
            if (string.IsNullOrWhiteSpace(gameId))
            {
                continue;
            }

            if (bundle?.GamesByGameId is null
                || !bundle.GamesByGameId.TryGetValue(gameId, out var entry))
            {
                continue;
            }

            matchedGameIds.Add(gameId);
            var layeredRows = ResolveLayeredOptiScalerIniRows(entry, bundle.SharedOptiScalerIniRows);
            var merged = new MergedGameInstallMetadata
            {
                GpuBundleLoaded = true,
                GpuBundleSupported = entry.InstallProfile.Enabled,
                GpuProfileId = (entry.ProfileId ?? "").Trim(),
                GpuBundleVendor = (entry.BundleGpuVendor ?? "").Trim().ToLowerInvariant(),
                GpuBundleKey = (entry.BundleKey ?? "").Trim(),
                GpuGroup = (entry.BundleGpuGroup ?? "").Trim().ToLowerInvariant(),
                OptiScalerDllName = (entry.InstallProfile.OptiScalerDllName ?? "").Trim(),
                ReFrameworkUrl = (entry.InstallProfile.ReFrameworkUrl ?? "").Trim(),
                SpecialK = (entry.InstallProfile.SpecialK ?? "").Trim(),
                ExtraBundle = (entry.InstallProfile.ExtraBundle ?? "").Trim(),
                ExcludeListRaw = (entry.InstallProfile.ExcludeListRaw ?? "").Trim(),
                ExcludeListPatterns = entry.InstallProfile.ExcludeListPatterns ?? [],
                UltimateAsiLoader = entry.InstallProfile.UltimateAsiLoader,
                OptiPatcher = entry.InstallProfile.OptiPatcher,
                Unreal5 = entry.InstallProfile.Unreal5,
                RtssOverlay = entry.InstallProfile.RtssOverlay,
                IniSettings = MaterializeIniSettings(layeredRows),
                GameIniProfileRows = [],
                GameUnrealIniProfileRows = [],
                EngineIniProfileRows = [],
                GameXmlProfileRows = [],
                RegistryProfileRows = [],
                GameJsonProfileRows = []
            };

            var attachedProfiles = _profileCatalogAttacher.Attach(profileCatalogs, merged.GpuProfileId);
            merged = new MergedGameInstallMetadata
            {
                GpuBundleLoaded = merged.GpuBundleLoaded,
                GpuBundleSupported = merged.GpuBundleSupported,
                GpuProfileId = merged.GpuProfileId,
                GpuBundleVendor = merged.GpuBundleVendor,
                GpuBundleKey = merged.GpuBundleKey,
                GpuGroup = merged.GpuGroup,
                OptiScalerDllName = merged.OptiScalerDllName,
                ReFrameworkUrl = merged.ReFrameworkUrl,
                SpecialK = merged.SpecialK,
                ExtraBundle = merged.ExtraBundle,
                ExcludeListRaw = merged.ExcludeListRaw,
                ExcludeListPatterns = merged.ExcludeListPatterns,
                UltimateAsiLoader = merged.UltimateAsiLoader,
                OptiPatcher = merged.OptiPatcher,
                Unreal5 = merged.Unreal5,
                RtssOverlay = merged.RtssOverlay,
                IniSettings = merged.IniSettings,
                GameIniProfileRows = attachedProfiles.GameIniProfileRows,
                GameUnrealIniProfileRows = attachedProfiles.GameUnrealIniProfileRows,
                EngineIniProfileRows = attachedProfiles.EngineIniProfileRows,
                GameXmlProfileRows = attachedProfiles.GameXmlProfileRows,
                RegistryProfileRows = attachedProfiles.RegistryProfileRows,
                GameJsonProfileRows = attachedProfiles.GameJsonProfileRows
            };

            if (merged.GpuBundleSupported)
            {
                supportedGameCount++;
            }

            metadata[gameId] = merged;
        }

        var unmatchedRuntimeGameIds = runtimeGameIds
            .Where(gameId => !matchedGameIds.Contains(gameId))
            .ToArray();
        var unmatchedBundleGameIds = bundleGameIds
            .Where(gameId => !runtimeGameIds.Contains(gameId, StringComparer.OrdinalIgnoreCase))
            .ToArray();

        return new GpuBundleMergeResult
        {
            MetadataByGameId = metadata,
            RuntimeGameCount = runtimeGameIds.Length,
            BundleGameCount = bundleGameIds.Length,
            MatchedGameCount = matchedGameIds.Count,
            SupportedGameCount = supportedGameCount,
            UnmatchedRuntimeGameIds = unmatchedRuntimeGameIds,
            UnmatchedBundleGameIds = unmatchedBundleGameIds
        };
    }

    public static IReadOnlyList<RuntimeDataRawRow> ResolveLayeredOptiScalerIniRows(
        RemoteGpuBundleGameEntry entry,
        IReadOnlyList<RuntimeDataRawRow> sharedRows)
    {
        var resolved = new List<RuntimeDataRawRow>();
        if (entry is null)
        {
            return resolved;
        }

        var activeProfileIds = BuildActiveProfileIds(entry);
        foreach (var row in sharedRows ?? [])
        {
            var profileId = RuntimeDataRowReader.GetString(row, "profile_id").Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(profileId))
            {
                continue;
            }

            if (activeProfileIds.Contains(profileId))
            {
                resolved.Add(row);
            }
        }

        foreach (var row in entry.LocalOptiScalerIniRows ?? [])
        {
            resolved.Add(row);
        }

        return resolved;
    }

    public static IReadOnlyDictionary<string, string> MaterializeIniSettings(IReadOnlyList<RuntimeDataRawRow> rows)
    {
        var selected = new Dictionary<string, (int Priority, string Value)>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows ?? [])
        {
            var section = RuntimeDataRowReader.GetString(row, "section");
            var key = RuntimeDataRowReader.GetString(row, "key");
            if (string.IsNullOrWhiteSpace(section) || string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            var value = RuntimeDataRowReader.GetString(row, "value");
            var composite = $"{section}:{key}";
            var priority = ParsePriority(RuntimeDataRowReader.GetString(row, "priority"));
            if (!selected.TryGetValue(composite, out var current))
            {
                selected[composite] = (priority, value);
                continue;
            }

            if (priority < current.Priority)
            {
                selected[composite] = (priority, value);
            }
        }

        return selected.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.Value,
            StringComparer.OrdinalIgnoreCase);
    }

    private static HashSet<string> BuildActiveProfileIds(RemoteGpuBundleGameEntry entry)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "global_all"
        };

        var vendor = (entry.BundleGpuVendor ?? "").Trim().ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(vendor)
            && !string.Equals(vendor, "all", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(vendor, "default", StringComparison.OrdinalIgnoreCase))
        {
            set.Add($"global_{vendor}");
        }

        var gameId = (entry.GameId ?? "").Trim().ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(gameId))
        {
            set.Add($"{gameId}_all");
        }

        var profileId = (entry.ProfileId ?? "").Trim().ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(profileId))
        {
            set.Add(profileId);
        }

        return set;
    }

    private static int ParsePriority(string raw)
    {
        if (int.TryParse((raw ?? "").Trim(), out var parsed))
        {
            return parsed;
        }

        return 100;
    }
}
