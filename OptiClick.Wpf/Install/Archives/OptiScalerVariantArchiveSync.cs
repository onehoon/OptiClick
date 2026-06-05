using System.IO;
using System.Text.Json;
using OptiClick.Infrastructure.FileSystem;
using OptiClick.Wpf.Logging;
using OptiClick.Wpf.Shell.RuntimeData;

namespace OptiClick.Wpf.Install.Archives;

public static class OptiScalerVariantArchiveStates
{
    public const string Ready = "ready";
    public const string Failed = "failed";
    public const string NotReady = "not_ready";
}

public sealed record OptiScalerVariantSelectionOption
{
    public string Variant { get; init; } = "";
    public string DisplayLabel { get; init; } = "";
    public string Version { get; init; } = "";
    public string DisplayVersion { get; init; } = "";
}

public sealed record OptiScalerVariantManifest
{
    public int SchemaVersion { get; init; } = 1;
    public string SelectedVariant { get; init; } = OptiScalerVariantCatalogBuilder.StableVariant;
    public string LastSyncedAtUtc { get; init; } = "";
    public IReadOnlyDictionary<string, OptiScalerVariantManifestEntry> Variants { get; init; } =
        new Dictionary<string, OptiScalerVariantManifestEntry>(StringComparer.OrdinalIgnoreCase);
}

public sealed record OptiScalerVariantManifestEntry
{
    public string Variant { get; init; } = "";
    public string Version { get; init; } = "";
    public string DisplayVersion { get; init; } = "";
    public string Filename { get; init; } = "";
    public string Url { get; init; } = "";
    public string Sha256 { get; init; } = "";
    public string CacheEntry { get; init; } = "";
    public string PayloadDirectory { get; init; } = "";
    public bool Ready { get; init; }
    public string State { get; init; } = OptiScalerVariantArchiveStates.NotReady;
    public string ErrorCode { get; init; } = "";
    public string UpdatedAtUtc { get; init; } = "";
}

public sealed record OptiScalerVariantSyncResult
{
    public OptiScalerVariantManifest Manifest { get; init; } = new();
    public IReadOnlyList<OptiScalerVariantSelectionOption> SelectionOptions { get; init; } = [];
    public string EffectiveVariant { get; init; } = OptiScalerVariantCatalogBuilder.StableVariant;
    public string EffectiveVersion { get; init; } = "";
    public string EffectiveDisplayVersion { get; init; } = "";
    public OptiScalerVariantManifestEntry? EffectiveEntry { get; init; }
    public ArchivePreparationState OptiScalerState { get; init; } = new();
    public bool UsedCanonicalFallback { get; init; }
    public bool ShouldPersistEffectiveVariant { get; init; }
    public IReadOnlyList<Install.Flow.InstallFlowLogEntry> Logs { get; init; } = [];
}

public interface IOptiScalerVariantManifestStore
{
    OptiScalerVariantManifest Load();
    void Save(OptiScalerVariantManifest manifest);
}

public sealed class OptiScalerVariantManifestStore : IOptiScalerVariantManifestStore
{
    private const string ManifestFileName = "optiscaler_variants_manifest.json";
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };
    private readonly string _manifestPath;
    private readonly IAppLogger _logger;

    public OptiScalerVariantManifestStore(string manifestRoot, IAppLogger? logger = null)
    {
        var root = string.IsNullOrWhiteSpace(manifestRoot)
            ? new AppLocalDataPathProvider().ManifestDirectory
            : manifestRoot;
        Directory.CreateDirectory(root);
        _manifestPath = Path.Combine(root, ManifestFileName);
        _logger = logger ?? NullAppLogger.Instance;
    }

    public OptiScalerVariantManifest Load()
    {
        if (!File.Exists(_manifestPath))
        {
            return new OptiScalerVariantManifest();
        }

        try
        {
            var json = File.ReadAllText(_manifestPath);
            if (string.IsNullOrWhiteSpace(json))
            {
                return new OptiScalerVariantManifest();
            }

            return JsonSerializer.Deserialize<OptiScalerVariantManifest>(json) ?? new OptiScalerVariantManifest();
        }
        catch (JsonException ex)
        {
            var movedPath = TryMoveCorruptManifest();
            _logger.Warning(
                "Archives",
                $"optiscaler variants manifest corrupt type={ex.GetType().Name} moved={Path.GetFileName(movedPath)}");
            return new OptiScalerVariantManifest();
        }
        catch (Exception ex)
        {
            _logger.Warning("Archives", $"optiscaler variants manifest load failed type={ex.GetType().Name}");
            return new OptiScalerVariantManifest();
        }
    }

    public void Save(OptiScalerVariantManifest manifest)
    {
        try
        {
            var json = JsonSerializer.Serialize(manifest ?? new OptiScalerVariantManifest(), SerializerOptions);
            AtomicFileWriter.WriteAllTextAtomic(_manifestPath, json);
        }
        catch (Exception ex)
        {
            _logger.Warning("Archives", $"optiscaler variants manifest save failed type={ex.GetType().Name}");
        }
    }

    private string TryMoveCorruptManifest()
    {
        try
        {
            return AtomicFileWriter.MoveCorruptFile(_manifestPath);
        }
        catch
        {
            return "";
        }
    }
}

public interface IOptiScalerVariantArchiveSyncService
{
    Task<OptiScalerVariantSyncResult> SyncAsync(
        OptiScalerVariantCatalog catalog,
        string preferredVariant,
        CancellationToken cancellationToken = default);
}

public sealed class OptiScalerVariantArchiveSyncService : IOptiScalerVariantArchiveSyncService
{
    private readonly ArchiveCachePaths _cachePaths;
    private readonly IOptiScalerPayloadCacheService _payloadCacheService;
    private readonly IOptiScalerVariantManifestStore _manifestStore;
    private readonly IArchiveDownloadManifestStore? _archiveManifestStore;
    private readonly OptiScalerPayloadValidator _validator;
    private readonly IAppLogger _logger;

    public OptiScalerVariantArchiveSyncService(
        ArchiveCachePaths cachePaths,
        IOptiScalerPayloadCacheService payloadCacheService,
        IOptiScalerVariantManifestStore manifestStore,
        IArchiveDownloadManifestStore? archiveManifestStore = null,
        OptiScalerPayloadValidator? validator = null,
        IAppLogger? logger = null)
    {
        _cachePaths = cachePaths;
        _payloadCacheService = payloadCacheService;
        _manifestStore = manifestStore;
        _archiveManifestStore = archiveManifestStore;
        _validator = validator ?? new OptiScalerPayloadValidator();
        _logger = logger ?? NullAppLogger.Instance;
    }

    public async Task<OptiScalerVariantSyncResult> SyncAsync(
        OptiScalerVariantCatalog catalog,
        string preferredVariant,
        CancellationToken cancellationToken = default)
    {
        var safeCatalog = catalog ?? OptiScalerVariantCatalog.Empty;
        var preferred = NormalizeVariant(preferredVariant);
        var logs = new List<Install.Flow.InstallFlowLogEntry>
        {
            Info("archive", $"optiscaler variants sync start variants={safeCatalog.Options.Count} preferred={preferred}")
        };

        if (!safeCatalog.HasRuntimeVariants)
        {
            return new OptiScalerVariantSyncResult
            {
                Logs = logs
            };
        }

        _cachePaths.EnsureDirectories();
        var previousManifest = _manifestStore.Load();
        var now = DateTime.UtcNow.ToString("O");
        var runtimeByVariant = safeCatalog.Options.ToDictionary(static option => option.Variant, StringComparer.OrdinalIgnoreCase);
        var allowedCacheEntries = BuildAllowedCacheEntries(safeCatalog);
        var nextEntries = new Dictionary<string, OptiScalerVariantManifestEntry>(StringComparer.OrdinalIgnoreCase);

        foreach (var option in safeCatalog.Options)
        {
            cancellationToken.ThrowIfCancellationRequested();
            previousManifest.Variants.TryGetValue(option.Variant, out var existing);
            var desiredCacheEntry = ResolveCacheEntryName(option);
            var desiredPayloadDirectory = Path.Combine(_cachePaths.OptiScalerPayloadCacheRoot, desiredCacheEntry);
            var metadataChanged = IsMetadataChanged(existing, option);
            var forceRebuild = metadataChanged;

            if (existing is not null
                && existing.Ready
                && !_validator.IsValid(existing.PayloadDirectory, out _))
            {
                forceRebuild = true;
                logs.Add(Warning("archive", $"optiscaler variant cache invalid variant={option.Variant}"));
            }

            var result = await _payloadCacheService.PrepareAsync(
                option.ToRemoteArchiveEntry(),
                _cachePaths.OptiScalerPayloadCacheRoot,
                cancellationToken,
                forceRebuild,
                allowedCacheEntries);

            var entry = CreateManifestEntry(option, desiredCacheEntry, desiredPayloadDirectory, result, now);
            nextEntries[option.Variant] = entry;
            logs.Add(Info(
                "archive",
                $"optiscaler variant sync variant={option.Variant} version={Normalize(option.Version, "-")} state={entry.State} ready={entry.Ready.ToString().ToLowerInvariant()} cache_entry={Normalize(entry.CacheEntry, "-")} error={Normalize(entry.ErrorCode, "-")} force_rebuild={forceRebuild.ToString().ToLowerInvariant()}"));
        }

        foreach (var removed in previousManifest.Variants.Values
                     .Where(entry => !runtimeByVariant.ContainsKey(entry.Variant)))
        {
            logs.Add(Info("archive", $"optiscaler variant removed variant={Normalize(removed.Variant, "-")} cache_entry={Normalize(removed.CacheEntry, "-")}"));
        }

        CleanupPayloadDirectories(allowedCacheEntries, logs);
        _archiveManifestStore?.PruneVersionEntriesByCacheEntry(
            ArchiveAssetRuntimeDataKeys.OptiScaler,
            allowedCacheEntries);

        var selectedVariant = ResolveRuntimeSelectionVariant(preferred, runtimeByVariant);
        var manifest = new OptiScalerVariantManifest
        {
            SchemaVersion = 1,
            SelectedVariant = selectedVariant,
            LastSyncedAtUtc = now,
            Variants = nextEntries
                .OrderBy(pair => safeCatalog.Find(pair.Key)?.SortOrder ?? int.MaxValue)
                .ThenBy(pair => safeCatalog.Find(pair.Key)?.SourceOrder ?? int.MaxValue)
                .ToDictionary(static pair => pair.Key, static pair => pair.Value, StringComparer.OrdinalIgnoreCase)
        };
        _manifestStore.Save(manifest);

        var readyEntries = nextEntries
            .Where(static pair => pair.Value.Ready)
            .ToDictionary(static pair => pair.Key, static pair => pair.Value, StringComparer.OrdinalIgnoreCase);
        var selectionOptions = safeCatalog.Options
            .Where(option => readyEntries.ContainsKey(option.Variant))
            .Select(static option => new OptiScalerVariantSelectionOption
            {
                Variant = option.Variant,
                DisplayLabel = option.DisplayLabel,
                Version = option.Version,
                DisplayVersion = option.DisplayVersion
            })
            .ToArray();

        var effectiveEntry = ResolveEffectiveEntry(selectedVariant, readyEntries, out var effectiveVariant);
        var usedCanonicalFallback = false;
        ArchivePreparationState optiScalerState;
        if (effectiveEntry is not null)
        {
            optiScalerState = ReadyState(effectiveEntry);
        }
        else
        {
            var fallback = await TryPrepareCanonicalFallbackAsync(safeCatalog, allowedCacheEntries, logs, cancellationToken);
            usedCanonicalFallback = fallback.Result is not null && fallback.Result.IsSuccess;
            optiScalerState = fallback.State;
            if (usedCanonicalFallback)
            {
                effectiveVariant = OptiScalerVariantCatalogBuilder.StableVariant;
            }
        }

        return new OptiScalerVariantSyncResult
        {
            Manifest = manifest,
            SelectionOptions = selectionOptions,
            EffectiveVariant = effectiveVariant,
            EffectiveVersion = effectiveEntry?.Version ?? (usedCanonicalFallback ? safeCatalog.CanonicalFallback?.Version ?? "" : ""),
            EffectiveDisplayVersion = effectiveEntry?.DisplayVersion ?? (usedCanonicalFallback ? safeCatalog.CanonicalFallback?.DisplayVersion ?? "" : ""),
            EffectiveEntry = effectiveEntry,
            OptiScalerState = optiScalerState,
            UsedCanonicalFallback = usedCanonicalFallback,
            ShouldPersistEffectiveVariant = ShouldPersistEffectiveVariant(preferred, runtimeByVariant, effectiveVariant),
            Logs = logs
        };
    }

    private static HashSet<string> BuildAllowedCacheEntries(OptiScalerVariantCatalog catalog)
    {
        var keep = catalog.Options
            .Select(ResolveCacheEntryName)
            .Where(static entry => !string.IsNullOrWhiteSpace(entry))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (catalog.Find(OptiScalerVariantCatalogBuilder.StableVariant) is null
            && catalog.CanonicalFallback is not null)
        {
            keep.Add(ResolveCacheEntryName(catalog.CanonicalFallback));
        }

        return keep;
    }

    private static bool IsMetadataChanged(OptiScalerVariantManifestEntry? existing, OptiScalerVariantOption option)
    {
        if (existing is null)
        {
            return false;
        }

        return !string.Equals(existing.Version, option.Version, StringComparison.Ordinal)
               || !string.Equals(existing.Filename, option.Filename, StringComparison.Ordinal)
               || !string.Equals(existing.Url, option.Url, StringComparison.Ordinal)
               || !string.Equals(existing.Sha256, option.Sha256, StringComparison.OrdinalIgnoreCase);
    }

    private static OptiScalerVariantManifestEntry CreateManifestEntry(
        OptiScalerVariantOption option,
        string desiredCacheEntry,
        string desiredPayloadDirectory,
        OptiScalerPayloadCacheResult result,
        string updatedAtUtc)
    {
        var ready = result.IsSuccess;
        return new OptiScalerVariantManifestEntry
        {
            Variant = option.Variant,
            Version = option.Version,
            DisplayVersion = option.DisplayVersion,
            Filename = option.Filename,
            Url = option.Url,
            Sha256 = option.Sha256,
            CacheEntry = string.IsNullOrWhiteSpace(result.CacheEntryName) ? desiredCacheEntry : result.CacheEntryName,
            PayloadDirectory = string.IsNullOrWhiteSpace(result.PayloadDirectory) ? desiredPayloadDirectory : result.PayloadDirectory,
            Ready = ready,
            State = ready ? OptiScalerVariantArchiveStates.Ready : OptiScalerVariantArchiveStates.Failed,
            ErrorCode = result.ErrorCode,
            UpdatedAtUtc = updatedAtUtc
        };
    }

    private void CleanupPayloadDirectories(
        IReadOnlySet<string> allowedCacheEntries,
        ICollection<Install.Flow.InstallFlowLogEntry> logs)
    {
        if (!Directory.Exists(_cachePaths.OptiScalerPayloadCacheRoot))
        {
            return;
        }

        foreach (var directory in Directory.EnumerateDirectories(_cachePaths.OptiScalerPayloadCacheRoot))
        {
            var name = Path.GetFileName(directory);
            if (string.IsNullOrWhiteSpace(name)
                || allowedCacheEntries.Contains(name))
            {
                continue;
            }

            try
            {
                Directory.Delete(directory, recursive: true);
                logs.Add(Info("archive", $"optiscaler variant cache removed cache_entry={name}"));
            }
            catch (Exception ex)
            {
                _logger.Warning("Archives", $"optiscaler variant cache remove failed cache_entry={name} type={ex.GetType().Name}");
                logs.Add(Warning("archive", $"optiscaler variant cache remove failed cache_entry={name} type={ex.GetType().Name}"));
            }
        }
    }

    private async Task<(OptiScalerPayloadCacheResult? Result, ArchivePreparationState State)> TryPrepareCanonicalFallbackAsync(
        OptiScalerVariantCatalog catalog,
        IReadOnlySet<string> allowedCacheEntries,
        ICollection<Install.Flow.InstallFlowLogEntry> logs,
        CancellationToken cancellationToken)
    {
        if (catalog.Find(OptiScalerVariantCatalogBuilder.StableVariant) is not null
            || catalog.CanonicalFallback is null)
        {
            return (null, new ArchivePreparationState());
        }

        logs.Add(Warning("archive", "optiscaler variants stable missing; trying canonical optiscaler fallback"));
        var result = await _payloadCacheService.PrepareAsync(
            catalog.CanonicalFallback.ToRemoteArchiveEntry(),
            _cachePaths.OptiScalerPayloadCacheRoot,
            cancellationToken,
            forceRebuild: false,
            cacheEntriesToKeep: allowedCacheEntries);
        if (!result.IsSuccess)
        {
            return (result, new ArchivePreparationState
            {
                Filename = catalog.CanonicalFallback.Filename,
                Ready = false,
                ErrorMessage = result.ErrorCode,
                StageStatus = result.StageStatus
            });
        }

        return (result, new ArchivePreparationState
        {
            Filename = catalog.CanonicalFallback.Filename,
            ArchivePath = result.PayloadDirectory,
            Ready = true,
            StageStatus = result.StageStatus
        });
    }

    private static string ResolveRuntimeSelectionVariant(
        string preferred,
        IReadOnlyDictionary<string, OptiScalerVariantOption> runtimeByVariant)
    {
        if (runtimeByVariant.ContainsKey(preferred))
        {
            return preferred;
        }

        return runtimeByVariant.ContainsKey(OptiScalerVariantCatalogBuilder.StableVariant)
            ? OptiScalerVariantCatalogBuilder.StableVariant
            : preferred;
    }

    private static OptiScalerVariantManifestEntry? ResolveEffectiveEntry(
        string preferred,
        IReadOnlyDictionary<string, OptiScalerVariantManifestEntry> readyEntries,
        out string effectiveVariant)
    {
        if (readyEntries.TryGetValue(preferred, out var preferredEntry))
        {
            effectiveVariant = preferred;
            return preferredEntry;
        }

        if (readyEntries.TryGetValue(OptiScalerVariantCatalogBuilder.StableVariant, out var stableEntry))
        {
            effectiveVariant = OptiScalerVariantCatalogBuilder.StableVariant;
            return stableEntry;
        }

        effectiveVariant = preferred;
        return null;
    }

    private static bool ShouldPersistEffectiveVariant(
        string preferred,
        IReadOnlyDictionary<string, OptiScalerVariantOption> runtimeByVariant,
        string effectiveVariant)
    {
        return !runtimeByVariant.ContainsKey(preferred)
               && string.Equals(effectiveVariant, OptiScalerVariantCatalogBuilder.StableVariant, StringComparison.OrdinalIgnoreCase);
    }

    private static ArchivePreparationState ReadyState(OptiScalerVariantManifestEntry entry)
    {
        return new ArchivePreparationState
        {
            Filename = entry.Filename,
            ArchivePath = entry.PayloadDirectory,
            Ready = true,
            StageStatus = ArchivePreparationStageStatuses.CachedStatus()
        };
    }

    private static string ResolveCacheEntryName(OptiScalerVariantOption option)
    {
        return ArchivePayloadCacheEntryNames.ResolveOptiScalerEntryName(option.ToRemoteArchiveEntry());
    }

    private static string NormalizeVariant(string? variant)
    {
        var normalized = OptiScalerVariantCatalogBuilder.NormalizeVariant(variant);
        return string.IsNullOrWhiteSpace(normalized)
            ? OptiScalerVariantCatalogBuilder.StableVariant
            : normalized;
    }

    private static string Normalize(string value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }

    private static Install.Flow.InstallFlowLogEntry Info(string category, string message)
    {
        return new Install.Flow.InstallFlowLogEntry
        {
            Level = "info",
            Category = category,
            Message = message
        };
    }

    private static Install.Flow.InstallFlowLogEntry Warning(string category, string message)
    {
        return new Install.Flow.InstallFlowLogEntry
        {
            Level = "warning",
            Category = category,
            Message = message
        };
    }
}
