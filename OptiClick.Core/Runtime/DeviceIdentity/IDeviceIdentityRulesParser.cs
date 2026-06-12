namespace OptiClick.Core.Runtime.DeviceIdentity;

public interface IDeviceIdentityRulesParser
{
    DeviceIdentityRulesParseResult Parse(string json);
}
