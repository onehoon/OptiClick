using OptiClick.Core.Runtime;
using OptiClick.Core.Abstractions;

namespace OptiClick.Infrastructure.Windows;

public sealed class WindowsDeviceInfoProvider : IDeviceInfoProvider
{
    private readonly Func<DeviceInfo> _query;

    public WindowsDeviceInfoProvider()
        : this(QueryDeviceInfo)
    {
    }

    public WindowsDeviceInfoProvider(Func<DeviceInfo> query)
    {
        _query = query ?? throw new ArgumentNullException(nameof(query));
    }

    public DeviceInfo GetDeviceInfo()
    {
        try
        {
            var device = _query();
            return Normalize(device);
        }
        catch
        {
            return CreateFallback();
        }
    }

    private static DeviceInfo QueryDeviceInfo()
    {
        if (!OperatingSystem.IsWindows())
        {
            return CreateFallback();
        }

        var row = WindowsWmiQueryHelper.Query(
                "SELECT Manufacturer, Model, Name FROM Win32_ComputerSystem",
                static item =>
                new WmiDeviceRow
                {
                    Manufacturer = WindowsWmiQueryHelper.ReadString(item, "Manufacturer"),
                    Model = WindowsWmiQueryHelper.ReadString(item, "Model"),
                    DeviceName = WindowsWmiQueryHelper.ReadString(item, "Name")
                })
            .FirstOrDefault();

        if (row is null)
        {
            return CreateFallback();
        }

        return new DeviceInfo
        {
            Manufacturer = row.Manufacturer,
            Model = row.Model,
            DeviceName = row.DeviceName
        };
    }

    private static DeviceInfo Normalize(DeviceInfo? device)
    {
        if (device is null)
        {
            return CreateFallback();
        }

        var manufacturer = (device.Manufacturer ?? "").Trim();
        var model = (device.Model ?? "").Trim();
        var deviceName = (device.DeviceName ?? "").Trim();

        if (string.IsNullOrWhiteSpace(deviceName))
        {
            deviceName = Environment.MachineName ?? "";
        }

        return new DeviceInfo
        {
            Manufacturer = manufacturer,
            Model = model,
            DeviceName = deviceName
        };
    }

    private static DeviceInfo CreateFallback()
    {
        return new DeviceInfo
        {
            Manufacturer = "",
            Model = "",
            DeviceName = Environment.MachineName ?? ""
        };
    }

    private sealed record WmiDeviceRow
    {
        public string Manufacturer { get; init; } = "";
        public string Model { get; init; } = "";
        public string DeviceName { get; init; } = "";
    }
}
