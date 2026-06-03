using Microsoft.Win32;

namespace OptiClick.Infrastructure.Registry;

public interface IRegistryAccess
{
    IReadableRegistryKey? OpenSubKey(string hiveName, string keyPath);
    IWritableRegistryKey? CreateSubKey(string hiveName, string keyPath);
}

public interface IReadableRegistryKey : IDisposable
{
    object? GetValue(string valueName);
}

public interface IWritableRegistryKey : IReadableRegistryKey
{
    void SetValue(string valueName, object value, RegistryValueKind valueKind);
}

public sealed class WindowsRegistryWriter
{
    private readonly IRegistryAccess _registryAccess;

    public WindowsRegistryWriter()
        : this(new WindowsRegistryAccess())
    {
    }

    public WindowsRegistryWriter(IRegistryAccess registryAccess)
    {
        _registryAccess = registryAccess ?? throw new ArgumentNullException(nameof(registryAccess));
    }

    public void SetValue(string hiveName, string keyPath, string valueName, string valueTypeName, object value)
    {
        var hive = (hiveName ?? "").Trim().ToUpperInvariant();
        var normalizedKeyPath = (keyPath ?? "").Trim();
        var normalizedValueName = (valueName ?? "").Trim();
        var normalizedTypeName = (valueTypeName ?? "").Trim().ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(hive)
            || string.IsNullOrWhiteSpace(normalizedKeyPath)
            || string.IsNullOrWhiteSpace(normalizedValueName)
            || string.IsNullOrWhiteSpace(normalizedTypeName))
        {
            throw new InvalidOperationException("Invalid registry write arguments.");
        }

        if (!IsSupportedHive(hive))
        {
            throw new InvalidOperationException($"Unsupported registry hive: {hiveName}");
        }

        var valueKind = normalizedTypeName switch
        {
            "REG_SZ" => RegistryValueKind.String,
            "REG_EXPAND_SZ" => RegistryValueKind.ExpandString,
            "REG_MULTI_SZ" => RegistryValueKind.MultiString,
            "REG_DWORD" => RegistryValueKind.DWord,
            "REG_QWORD" => RegistryValueKind.QWord,
            _ => throw new InvalidOperationException($"Unsupported registry value type: {valueTypeName}")
        };

        using var key = _registryAccess.CreateSubKey(hive, normalizedKeyPath)
            ?? throw new InvalidOperationException($"Failed to open registry key: {normalizedKeyPath}");
        key.SetValue(normalizedValueName, value, valueKind);
    }

    public object? GetValue(string hiveName, string keyPath, string valueName)
    {
        var hive = (hiveName ?? "").Trim().ToUpperInvariant();
        var normalizedKeyPath = (keyPath ?? "").Trim();
        var normalizedValueName = (valueName ?? "").Trim();

        if (string.IsNullOrWhiteSpace(hive)
            || string.IsNullOrWhiteSpace(normalizedKeyPath)
            || string.IsNullOrWhiteSpace(normalizedValueName))
        {
            throw new InvalidOperationException("Invalid registry read arguments.");
        }

        if (!IsSupportedHive(hive))
        {
            throw new InvalidOperationException($"Unsupported registry hive: {hiveName}");
        }

        using var key = _registryAccess.OpenSubKey(hive, normalizedKeyPath);
        if (key is null)
        {
            return null;
        }

        return key.GetValue(normalizedValueName);
    }

    private static bool IsSupportedHive(string hiveName)
    {
        return hiveName is "HKEY_CURRENT_USER" or "HKEY_LOCAL_MACHINE" or "HKEY_CLASSES_ROOT" or "HKEY_USERS";
    }

    private sealed class WindowsRegistryAccess : IRegistryAccess
    {
        public IReadableRegistryKey? OpenSubKey(string hiveName, string keyPath)
        {
            var root = ResolveRoot(hiveName);
            if (root is null)
            {
                return null;
            }

            var key = root.OpenSubKey(keyPath, writable: false);
            return key is null ? null : new ReadableRegistryKeyAdapter(key);
        }

        public IWritableRegistryKey? CreateSubKey(string hiveName, string keyPath)
        {
            var root = ResolveRoot(hiveName);
            if (root is null)
            {
                return null;
            }

            var key = root.CreateSubKey(keyPath, writable: true);
            return key is null ? null : new WritableRegistryKeyAdapter(key);
        }

        private static RegistryKey? ResolveRoot(string hiveName)
        {
            return hiveName switch
            {
                "HKEY_CURRENT_USER" => global::Microsoft.Win32.Registry.CurrentUser,
                "HKEY_LOCAL_MACHINE" => global::Microsoft.Win32.Registry.LocalMachine,
                "HKEY_CLASSES_ROOT" => global::Microsoft.Win32.Registry.ClassesRoot,
                "HKEY_USERS" => global::Microsoft.Win32.Registry.Users,
                _ => null
            };
        }
    }

    private class ReadableRegistryKeyAdapter : IReadableRegistryKey
    {
        protected readonly RegistryKey Key;

        public ReadableRegistryKeyAdapter(RegistryKey key)
        {
            Key = key ?? throw new ArgumentNullException(nameof(key));
        }

        public object? GetValue(string valueName)
        {
            return Key.GetValue(valueName);
        }

        public void Dispose()
        {
            Key.Dispose();
        }
    }

    private sealed class WritableRegistryKeyAdapter : ReadableRegistryKeyAdapter, IWritableRegistryKey
    {
        public WritableRegistryKeyAdapter(RegistryKey key)
            : base(key)
        {
        }

        public void SetValue(string valueName, object value, RegistryValueKind valueKind)
        {
            Key.SetValue(valueName, value, valueKind);
        }
    }
}
