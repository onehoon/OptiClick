namespace OptiClick.Wpf.Services;

public sealed class InMemoryDeviceIdentityRulesProvider : IDeviceIdentityRulesProvider
{
    private readonly object _sync = new();
    private DeviceIdentityRules _current = DeviceIdentityRules.Empty;

    public DeviceIdentityRules Current
    {
        get
        {
            lock (_sync)
            {
                return _current;
            }
        }
    }

    public void Update(DeviceIdentityRules rules)
    {
        lock (_sync)
        {
            _current = rules ?? DeviceIdentityRules.Empty;
        }
    }
}
