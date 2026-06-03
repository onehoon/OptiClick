using System.IO;
using OptiClick.Infrastructure.FileSystem;
using OptiClick.Wpf.Logging;

namespace OptiClick.Wpf.Shell.Runtime.DeviceIdentity;

public interface IDeviceIdentityRulesCacheStore
{
    string? TryReadContent();
    void TryWriteContent(string content);
}

public sealed class DeviceIdentityRulesCacheStore : IDeviceIdentityRulesCacheStore
{
    private const string CacheFileName = "device_identity_rules_cache.json";
    private readonly string _cachePath;
    private readonly IAppLogger _logger;

    public DeviceIdentityRulesCacheStore(
        IAppLocalDataPathProvider pathProvider,
        IAppLogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(pathProvider);

        Directory.CreateDirectory(pathProvider.ManifestDirectory);
        _cachePath = Path.Combine(pathProvider.ManifestDirectory, CacheFileName);
        _logger = logger ?? NullAppLogger.Instance;
    }

    public string? TryReadContent()
    {
        try
        {
            if (!File.Exists(_cachePath))
            {
                return null;
            }

            var content = File.ReadAllText(_cachePath);
            return string.IsNullOrWhiteSpace(content) ? null : content;
        }
        catch (Exception ex)
        {
            _logger.Warning(
                "device-rules",
                $"device identity rules cache read failed file={Path.GetFileName(_cachePath)} type={ex.GetType().Name}");
            return null;
        }
    }

    public void TryWriteContent(string content)
    {
        var safeContent = (content ?? "").Trim();
        if (string.IsNullOrWhiteSpace(safeContent))
        {
            return;
        }

        try
        {
            AtomicFileWriter.WriteAllTextAtomic(_cachePath, safeContent);
        }
        catch (Exception ex)
        {
            _logger.Warning(
                "device-rules",
                $"device identity rules cache write failed file={Path.GetFileName(_cachePath)} type={ex.GetType().Name}");
        }
    }
}
