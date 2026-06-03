using OptiClick.Core.Runtime;
using OptiClick.Core.Abstractions;

namespace OptiClick.Wpf.Services;

public sealed class MockDeviceInfoProvider : IDeviceInfoProvider
{
    private readonly DeviceInfo _device;

    public MockDeviceInfoProvider()
        : this(
            new DeviceInfo
            {
                Manufacturer = "ASUS",
                Model = "ROG Ally",
                DeviceName = "ROG Ally"
            })
    {
    }

    public MockDeviceInfoProvider(DeviceInfo device)
    {
        _device = device ?? new DeviceInfo();
    }

    public DeviceInfo GetDeviceInfo()
    {
        return _device;
    }
}
