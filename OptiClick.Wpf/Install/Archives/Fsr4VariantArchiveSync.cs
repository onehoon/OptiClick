using System.IO;
using System.Text.Json;
using OptiClick.Infrastructure.FileSystem;
using OptiClick.Wpf.Install.Flow;
using OptiClick.Wpf.Logging;
using OptiClick.Wpf.Shell.RuntimeData;

namespace OptiClick.Wpf.Install.Archives;

public static class Fsr4VariantArchiveStates
{
    public const string Ready = "ready";
    public const string Failed = "failed";
    public const string Missing = "missing";
}

public sealed record Fsr4VariantManifest
{
    public int SchemaVersion { get; init; } = 1;
    public string LastSyncedAtUtc { get; init; } = "";
    public IReadOnlyDictionary<string, Fsr4VariantManifestEntry> Variants { get; init; } =
        new Dictionary<string, Fsr4VariantManifestEntry>(StringComparer.OrdinalIgnoreCase);
}

public sealed record Fsr4VariantManifestEntry
{
    public string Variant { get; init; } = "";
    public string Version { get; init; } = "";
    public string DisplayVersion { get; init; } = "";
    public string Filename { get; init; } = "";
    public string Url { get; init; } = "";
    public string Sha256 { get; init; } = "";
    public string PayloadDirectory { get; init; } = "";
    public bool Ready { get; init; }
    public string State { get; init; } = Fsr4VariantArchiveStates.Missing;
    public string ErrorCode { get; init; } = "";
    public string UpdatedAtUtc { get; init; } = "";
}

public sealed record Fsr4VariantSyncResult
{
    public Fsr4VariantManifest Manifest { get; init; } = new();
    public ArchivePreparationState AggregateState { get; init; } = new();
    public IReadOnlyDictionary<string, ArchivePreparationState> StatesByVariant { get; init; } =
        new Dictionary<string, ArchivePreparationState>(StringComparer.OrdinalIgnoreCase);
    public IReadOnlyList<InstallFlowLogEntry> Logs { get; init; } = [];
}

public interface IFsr4VariantManifestStore
{
    Fsr4VariantManifest Load();
    void Save(Fsr4VariantManifest manifest);
}

public sealed class Fsr4VariantManifestStore : IFsr4VariantManifestStore
{
    private const string ManifestFileName = "fsr4_variants_manifest.json";
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _manifestPath;
    private readonly IAppLogger _logger;

    public Fsr4VariantManifestStore(string manifestRoot, IAppLogger? logger = null)
    {
        var root = string.IsNullOrWhiteSpace(manifestRoot)
            ? new AppLocalDataPathProvider().ManifestDirectory
            : manifestRoot;
        Directory.CreateDirectory(root);
        _manifestPath = Path.Combine(root, ManifestFileName);
        _logger = logger ?? NullAppLogger.Instance;
    }

    public Fsr4VariantManifest Load()
    {
        if (!File.Exists(_manifestPath))
        {
            return new Fsr4VariantManifest();
        }

        try
        {
            var json = File.ReadAllText(_manifestPath);
            return string.IsNullOrWhiteSpace(json)
                ? new Fsr4VariantManifest()
                : JsonSerializer.Deserialize<Fsr4VariantManifest>(json) ?? new Fsr4VariantManifest();
        }
        catch (JsonException ex)
        {
            var movedPath = TryMoveCorruptManifest();
            _logger.Warning(
                "Archives",
                $"fsr4 variants manifest corrupt type={ex.GetType().Name} moved={Path.GetFileName(movedPath)}");
            return new Fsr4VariantManifest();
        }
        catch (Exception ex)
        {
            _logger.Warning("Archives", $"fsr4 variants manifest load failed type={ex.GetType().Name}");
            return new Fsr4VariantManifest();
        }
    }

    public void Save(Fsr4VariantManifest manifest)
    {
        try
        {
            var json = JsonSerializer.Serialize(manifest ?? new Fsr4VariantManifest(), SerializerOptions);
            AtomicFileWriter.WriteAllTextAtomic(_manifestPath, json);
        }
        catch (Exception ex)
        {
            _logger.Warning("Archives", $"fsr4 variants manifest save failed type={ex.GetType().Name}");
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

public interface IFsr4VariantArchiveSyncService
{
    Task<Fsr4VariantSyncResult> SyncAsync(
        Fsr4VariantCatalog catalog,
        CancellationToken cancellationToken = default);
}

public sealed class Fsr4VariantArchiveSyncService : IFsr4VariantArchiveSyncService
{
    private readonly ArchiveCachePaths _cachePaths;
    private readonly Fsr4ArchivePreparationService _fsr4Service;
    private readonly IFsr4VariantManifestStore _manifestStore;

    public Fsr4VariantArchiveSyncService(
        ArchiveCachePaths cachePaths,
        Fsr4ArchivePreparationService fsr4Service,
        IFsr4VariantManifestStore manifestStore)
    {
        _cachePaths = cachePaths ?? throw new ArgumentNullException(nameof(cachePaths));
        _fsr4Service = fsr4Service ?? throw new ArgumentNullException(nameof(fsr4Service));
        _manifestStore = manifestStore ?? throw new ArgumentNullException(nameof(manifestStore));
    }

    public async Task<Fsr4VariantSyncResult> SyncAsync(
        Fsr4VariantCatalog catalog,
        CancellationToken cancellationToken = default)
    {
        var safeCatalog = catalog ?? Fsr4VariantCatalog.Empty;
        var logs = new List<InstallFlowLogEntry>();
        var statesByVariant = new Dictionary<string, ArchivePreparationState>(StringComparer.OrdinalIgnoreCase);
        var manifestEntries = new Dictionary<string, Fsr4VariantManifestEntry>(StringComparer.OrdinalIgnoreCase);
        var now = DateTime.UtcNow.ToString("O");

        _cachePaths.EnsureDirectories();
        _manifestStore.Load();

        foreach (var option in safeCatalog.Options)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var state = await _fsr4Service.PrepareAsync(
                option.ToRemoteArchiveEntry(),
                _cachePaths.Fsr4CacheDir,
                option.Variant,
                cancellationToken);
            statesByVariant[option.Variant] = state;
            manifestEntries[option.Variant] = CreateManifestEntry(option, state, now);

            if (!state.Ready)
            {
                logs.Add(InstallFlowLogEntryFactory.Warning(
                    "archive",
                    $"fsr4 variant sync failed variant={option.Variant} version={Normalize(option.Version, "-")} error={Normalize(state.ErrorMessage, "-")}"));
            }
        }

        var sourceOrderByVariant = safeCatalog.Options.ToDictionary(
            static option => option.Variant,
            static option => option.SourceOrder,
            StringComparer.OrdinalIgnoreCase);
        var manifest = new Fsr4VariantManifest
        {
            SchemaVersion = 1,
            LastSyncedAtUtc = now,
            Variants = manifestEntries
                .OrderBy(pair => sourceOrderByVariant.TryGetValue(pair.Key, out var order) ? order : int.MaxValue)
                .ThenBy(static pair => pair.Key, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(static pair => pair.Key, static pair => pair.Value, StringComparer.OrdinalIgnoreCase)
        };
        _manifestStore.Save(manifest);

        return new Fsr4VariantSyncResult
        {
            Manifest = manifest,
            AggregateState = BuildAggregateState(statesByVariant),
            StatesByVariant = statesByVariant,
            Logs = logs
        };
    }

    private static Fsr4VariantManifestEntry CreateManifestEntry(
        Fsr4VariantOption option,
        ArchivePreparationState state,
        string updatedAtUtc)
    {
        var ready = state.Ready;
        return new Fsr4VariantManifestEntry
        {
            Variant = option.Variant,
            Version = option.Version,
            DisplayVersion = option.DisplayVersion,
            Filename = option.Filename,
            Url = option.Url,
            Sha256 = option.Sha256,
            PayloadDirectory = state.ArchivePath,
            Ready = ready,
            State = ready ? Fsr4VariantArchiveStates.Ready : Fsr4VariantArchiveStates.Failed,
            ErrorCode = state.ErrorMessage,
            UpdatedAtUtc = updatedAtUtc
        };
    }

    private static ArchivePreparationState BuildAggregateState(
        IReadOnlyDictionary<string, ArchivePreparationState> statesByVariant)
    {
        if (statesByVariant.Count == 0)
        {
            return new ArchivePreparationState
            {
                Ready = false,
                ErrorMessage = "missing_resource"
            };
        }

        if (statesByVariant.Values.All(static state => state.Ready))
        {
            return new ArchivePreparationState
            {
                Filename = "fsr4_variants",
                Ready = true,
                StageStatus = ArchivePreparationStageStatuses.CachedStatus()
            };
        }

        var error = statesByVariant.Values
            .Select(static state => (state.ErrorMessage ?? "").Trim())
            .FirstOrDefault(static value => !string.IsNullOrWhiteSpace(value));
        return new ArchivePreparationState
        {
            Filename = "fsr4_variants",
            Ready = false,
            ErrorMessage = string.IsNullOrWhiteSpace(error) ? "fsr4_variant_not_ready" : error
        };
    }

    private static string Normalize(string value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }
}
