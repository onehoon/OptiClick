using OptiClick.Core.Runtime;

namespace OptiClick.Wpf.Services;

public sealed class PassthroughDeviceIdentityResolver : IDeviceIdentityResolver
{
    public DeviceInfo Resolve(DeviceInfo rawDeviceInfo)
    {
        return rawDeviceInfo ?? new DeviceInfo();
    }
}
