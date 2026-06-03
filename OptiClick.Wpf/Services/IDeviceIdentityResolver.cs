using OptiClick.Core.Runtime;

namespace OptiClick.Wpf.Services;

public interface IDeviceIdentityResolver
{
    DeviceInfo Resolve(DeviceInfo rawDeviceInfo);
}
