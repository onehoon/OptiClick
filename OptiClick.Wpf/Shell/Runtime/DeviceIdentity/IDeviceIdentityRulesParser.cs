using OptiClick.Wpf.Services;

namespace OptiClick.Wpf.Shell.Runtime.DeviceIdentity;

public interface IDeviceIdentityRulesParser
{
    DeviceIdentityRulesParseResult Parse(string json);
}
