using System.IO;
using System.Text.Json;
using OptiClick.Core.OptiScaler;
using OptiClick.Infrastructure.FileSystem;
using OptiClick.Infrastructure.Logging;

namespace OptiClick.Infrastructure.OptiScaler;

public sealed class OptiScalerCommonIniSettingsJsonStore : IOptiScalerCommonIniSettingsStore
{
    private const string SettingsFileName = "optiscaler_common_ini_settings.json";
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };
    private readonly string _settingsPath;
    private readonly IAppLogger _logger;

    public OptiScalerCommonIniSettingsJsonStore(
        IAppLocalDataPathProvider? pathProvider = null,
        IAppLogger? logger = null)
    {
        var provider = pathProvider ?? new AppLocalDataPathProvider();
        Directory.CreateDirectory(provider.ManifestDirectory);
        _settingsPath = Path.Combine(provider.ManifestDirectory, SettingsFileName);
        _logger = logger ?? NullAppLogger.Instance;
    }

    public OptiScalerCommonIniSettingsDocument Load()
    {
        if (!File.Exists(_settingsPath))
        {
            return new OptiScalerCommonIniSettingsDocument();
        }

        try
        {
            var json = File.ReadAllText(_settingsPath);
            if (string.IsNullOrWhiteSpace(json))
            {
                return new OptiScalerCommonIniSettingsDocument();
            }

            var settings = JsonSerializer.Deserialize<OptiScalerCommonIniSettingsDocument>(json);
            return settings ?? new OptiScalerCommonIniSettingsDocument();
        }
        catch (JsonException ex)
        {
            var movedPath = MoveCorruptSettingsFile();
            _logger.Warning(
                "Settings",
                $"optiscaler common ini settings load failed file={Path.GetFileName(_settingsPath)} type={ex.GetType().Name} moved={Path.GetFileName(movedPath)}");
            return new OptiScalerCommonIniSettingsDocument();
        }
        catch (Exception ex)
        {
            _logger.Warning(
                "Settings",
                $"optiscaler common ini settings load failed file={Path.GetFileName(_settingsPath)} type={ex.GetType().Name}");
            return new OptiScalerCommonIniSettingsDocument();
        }
    }

    public OptiScalerSettingsPersistenceResult Save(OptiScalerCommonIniSettingsDocument settings)
    {
        var safeSettings = OptiScalerCommonIniSettingsMaterializer.NormalizeDocument(settings);
        try
        {
            var json = JsonSerializer.Serialize(safeSettings, SerializerOptions);
            AtomicFileWriter.WriteAllTextAtomic(_settingsPath, json);
            return OptiScalerSettingsPersistenceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.Warning(
                "Settings",
                $"optiscaler common ini settings save failed file={Path.GetFileName(_settingsPath)} type={ex.GetType().Name}");
            return OptiScalerSettingsPersistenceResult.Failed("common_ini_save_failed");
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
