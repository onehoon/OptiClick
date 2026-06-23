using OptiClick.Core.Runtime;
using OptiClick.Core.Abstractions;

namespace OptiClick.Wpf.Services;

public sealed class MockDeviceInfoProvider : IDeviceInfoProvider, IRuntimeHardwareDetectionInfoProvider
{
    private readonly DeviceInfo _device;
    private readonly RuntimeHardwareDetectionInfo _detectionInfo;

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
        : this(
            device,
            new RuntimeHardwareDetectionInfo
            {
                DeviceInfoSource = "mock",
                WmiDeviceStatus = "success"
            })
    {
    }

    public MockDeviceInfoProvider(DeviceInfo device, RuntimeHardwareDetectionInfo detectionInfo)
    {
        _device = device ?? new DeviceInfo();
        _detectionInfo = NormalizeDetectionInfo(detectionInfo);
    }

    public DeviceInfo GetDeviceInfo()
    {
        return _device;
    }

    public RuntimeHardwareDetectionInfo GetHardwareDetectionInfo()
    {
        return _detectionInfo;
    }

    private static RuntimeHardwareDetectionInfo NormalizeDetectionInfo(RuntimeHardwareDetectionInfo? detectionInfo)
    {
        var detection = detectionInfo ?? new RuntimeHardwareDetectionInfo();
        return new RuntimeHardwareDetectionInfo
        {
            DeviceInfoSource = (detection.DeviceInfoSource ?? "").Trim(),
            WmiDeviceStatus = (detection.WmiDeviceStatus ?? "").Trim(),
            WmiDeviceAttempts = Math.Max(0, detection.WmiDeviceAttempts)
        };
    }
}
