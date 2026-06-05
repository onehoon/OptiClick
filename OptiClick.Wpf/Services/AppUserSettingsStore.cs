using System.IO;
using System.Text.Json;
using OptiClick.Infrastructure.FileSystem;
using OptiClick.Wpf.Logging;

namespace OptiClick.Wpf.Services;

public sealed class AppUserSettings
{
    public int Version { get; init; } = 1;
    public string LanguagePreference { get; init; } = "auto";
    public string OptiScalerVariantPreference { get; init; } = "stable";
}

public interface IAppUserSettingsStore
{
    AppUserSettings Load();
    void Save(AppUserSettings settings);
}

public sealed class AppUserSettingsStore : IAppUserSettingsStore
{
    private const string SettingsFileName = "settings.json";
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };
    private readonly string _settingsPath;
    private readonly IAppLogger _logger;

    public AppUserSettingsStore(
        IAppLocalDataPathProvider? pathProvider = null,
        IAppLogger? logger = null)
    {
        var provider = pathProvider ?? new AppLocalDataPathProvider();
        Directory.CreateDirectory(provider.ManifestDirectory);
        _settingsPath = Path.Combine(provider.ManifestDirectory, SettingsFileName);
        _logger = logger ?? NullAppLogger.Instance;
    }

    public AppUserSettings Load()
    {
        if (!File.Exists(_settingsPath))
        {
            return new AppUserSettings();
        }

        try
        {
            var json = File.ReadAllText(_settingsPath);
            if (string.IsNullOrWhiteSpace(json))
            {
                return new AppUserSettings();
            }

            var settings = JsonSerializer.Deserialize<AppUserSettings>(json);
            return settings ?? new AppUserSettings();
        }
        catch (JsonException ex)
        {
            var movedPath = MoveCorruptSettingsFile();
            _logger.Warning(
                "Settings",
                $"settings load failed file={Path.GetFileName(_settingsPath)} type={ex.GetType().Name} moved={Path.GetFileName(movedPath)}");
            return new AppUserSettings();
        }
        catch (Exception ex)
        {
            _logger.Warning("Settings", $"settings load failed file={Path.GetFileName(_settingsPath)} type={ex.GetType().Name}");
            return new AppUserSettings();
        }
    }

    public void Save(AppUserSettings settings)
    {
        var safeSettings = settings ?? new AppUserSettings();
        try
        {
            var json = JsonSerializer.Serialize(safeSettings, SerializerOptions);
            AtomicFileWriter.WriteAllTextAtomic(_settingsPath, json);
        }
        catch (Exception ex)
        {
            _logger.Warning("Settings", $"settings save failed file={Path.GetFileName(_settingsPath)} type={ex.GetType().Name}");
        }
    }

    private string MoveCorruptSettingsFile()
    {
        try
        {
            return AtomicFileWriter.MoveCorruptFile(_settingsPath);
        }
        catch
        {
            return "";
        }
    }
}
