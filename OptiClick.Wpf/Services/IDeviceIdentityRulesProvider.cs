namespace OptiClick.Wpf.Services;

public interface IDeviceIdentityRulesProvider
{
    DeviceIdentityRules Current { get; }
    void Update(DeviceIdentityRules rules);
}
