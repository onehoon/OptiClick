using System.IO;
using System.Text.Json;
using OptiClick.Infrastructure.FileSystem;
using OptiClick.Wpf.Logging;

namespace OptiClick.Wpf.Shell.Startup;

public sealed record FirstRunState
{
    public bool FirstStartupCompleted { get; init; }
    public bool ArchivePreparedOnce { get; init; }
    public bool CoverCacheBootstrapAttempted { get; init; }
    public string CoverCacheBootstrapState { get; init; } = "";
    public DateTimeOffset? CreatedAt { get; init; }
}

public interface IFirstRunStateStore
{
    Task<FirstRunState> LoadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(FirstRunState state, CancellationToken cancellationToken = default);
}

public sealed class FirstRunStateStore : IFirstRunStateStore
{
    private const string StateFileName = "startup_state.json";
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private readonly string _statePath;
    private readonly IAppLogger _logger;

    public FirstRunStateStore(
        IAppLocalDataPathProvider? pathProvider = null,
        IAppLogger? logger = null)
    {
        var provider = pathProvider ?? new AppLocalDataPathProvider();
        Directory.CreateDirectory(provider.ManifestDirectory);
        _statePath = Path.Combine(provider.ManifestDirectory, StateFileName);
        _logger = logger ?? NullAppLogger.Instance;
    }

    public async Task<FirstRunState> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_statePath))
        {
            return new FirstRunState();
        }

        try
        {
            var json = await File.ReadAllTextAsync(_statePath, cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(json))
            {
                return new FirstRunState();
            }

            return JsonSerializer.Deserialize<FirstRunState>(json, SerializerOptions)
                   ?? new FirstRunState();
        }
        catch (JsonException ex)
        {
            var movedPath = MoveCorruptStateFile();
            _logger.Warning(
                "Startup",
                $"first-run state load failed file={Path.GetFileName(_statePath)} type={ex.GetType().Name} moved={Path.GetFileName(movedPath)}");
            return new FirstRunState();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.Warning("Startup", $"first-run state load failed file={Path.GetFileName(_statePath)} type={ex.GetType().Name}");
            return new FirstRunState();
        }
    }

    public Task SaveAsync(FirstRunState state, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var safeState = state ?? new FirstRunState();
        try
        {
            var json = JsonSerializer.Serialize(safeState, SerializerOptions);
            AtomicFileWriter.WriteAllTextAtomic(_statePath, json);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.Warning("Startup", $"first-run state save failed file={Path.GetFileName(_statePath)} type={ex.GetType().Name}");
        }

        return Task.CompletedTask;
    }

    private string MoveCorruptStateFile()
    {
        try
        {
            return AtomicFileWriter.MoveCorruptFile(_statePath);
        }
        catch
        {
            return "";
        }
    }
}
