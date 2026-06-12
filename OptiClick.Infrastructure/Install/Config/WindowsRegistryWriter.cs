namespace OptiClick.Infrastructure.Install.Config;

public sealed class WindowsRegistryWriter : IRegistryWriter
{
    private readonly OptiClick.Infrastructure.Registry.WindowsRegistryWriter _inner;

    public WindowsRegistryWriter()
        : this(new OptiClick.Infrastructure.Registry.WindowsRegistryWriter())
    {
    }

    internal WindowsRegistryWriter(OptiClick.Infrastructure.Registry.WindowsRegistryWriter inner)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    public object? GetValue(string hiveName, string keyPath, string valueName)
    {
        return _inner.GetValue(hiveName, keyPath, valueName);
    }

    public void SetValue(string hiveName, string keyPath, string valueName, string valueTypeName, object value)
    {
        // Keep the config writer contract separate from the low-level registry implementation.
        _inner.SetValue(hiveName, keyPath, valueName, valueTypeName, value);
    }
}
