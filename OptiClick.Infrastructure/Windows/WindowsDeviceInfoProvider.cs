using OptiClick.Core.Runtime;
using OptiClick.Core.Abstractions;
using OptiClick.Infrastructure.Logging;
using Microsoft.Win32;

namespace OptiClick.Infrastructure.Windows;

public sealed class WindowsDeviceInfoProvider : IDeviceInfoProvider, IRuntimeHardwareDetectionInfoProvider
{
    private const string LogCategory = "runtime-device";
    private const string RegistryBiosPath = @"HARDWARE\DESCRIPTION\System\BIOS";

    private readonly Func<WindowsDeviceInfoQueryResult> _query;
    private readonly IAppLogger _logger;
    private RuntimeHardwareDetectionInfo _detectionInfo = new();

    public WindowsDeviceInfoProvider()
        : this((IAppLogger?)null)
    {
    }

    public WindowsDeviceInfoProvider(IAppLogger? logger)
        : this(() => QueryDeviceInfo(logger), logger)
    {
    }

    public WindowsDeviceInfoProvider(Func<DeviceInfo> query)
        : this(() => CreateLegacyQueryResult(query), null)
    {
    }

    internal WindowsDeviceInfoProvider(Func<WindowsDeviceInfoQueryResult> query, IAppLogger? logger)
    {
        _query = query ?? throw new ArgumentNullException(nameof(query));
        _logger = logger ?? NullAppLogger.Instance;
    }

    public DeviceInfo GetDeviceInfo()
    {
        try
        {
            var result = _query();
            var device = Normalize(result.Device);
            _detectionInfo = NormalizeDetection(result.Detection);
            LogDetection(_detectionInfo);
            return device;
        }
        catch (Exception exception)
        {
            _detectionInfo = new RuntimeHardwareDetectionInfo
            {
                DeviceInfoSource = "fallback",
                WmiDeviceStatus = WindowsWmiQueryStatuses.Exception,
                WmiDeviceAttempts = 0
            };
            _logger.Error(LogCategory, "device info query failed; using fallback.", exception);
            return CreateFallback();
        }
    }

    public RuntimeHardwareDetectionInfo GetHardwareDetectionInfo()
    {
        return _detectionInfo;
    }

    private static WindowsDeviceInfoQueryResult QueryDeviceInfo(IAppLogger? logger = null)
    {
        if (!OperatingSystem.IsWindows())
        {
            return CreateResult(
                CreateFallback(),
                "fallback",
                WindowsWmiQueryStatuses.NonWindows,
                0);
        }

        var wmiResult = WindowsWmiQueryHelper.QueryWithResult(
            "SELECT Manufacturer, Model, Name FROM Win32_ComputerSystem",
            static item =>
            new WmiDeviceRow
            {
                Manufacturer = WindowsWmiQueryHelper.ReadString(item, "Manufacturer"),
                Model = WindowsWmiQueryHelper.ReadString(item, "Model"),
                DeviceName = WindowsWmiQueryHelper.ReadString(item, "Name")
            },
            new WindowsWmiQueryOptions
            {
                SourceName = "Win32_ComputerSystem",
                LogCategory = LogCategory,
                Logger = logger
            });

        if (wmiResult.Status == WindowsWmiQueryStatuses.Success)
        {
            var row = wmiResult.Rows.FirstOrDefault();
            if (row is not null)
            {
                return CreateResult(
                    new DeviceInfo
                    {
                        Manufacturer = row.Manufacturer,
                        Model = row.Model,
                        DeviceName = row.DeviceName
                    },
                    "wmi",
                    wmiResult.Status,
                    wmiResult.Attempts);
            }
        }

        var registryDevice = TryReadRegistryFallback();
        if (!string.IsNullOrWhiteSpace(registryDevice.Manufacturer)
            || !string.IsNullOrWhiteSpace(registryDevice.Model))
        {
            return CreateResult(
                registryDevice,
                "registry",
                wmiResult.Status,
                wmiResult.Attempts);
        }

        return CreateResult(
            CreateFallback(),
            "fallback",
            wmiResult.Status,
            wmiResult.Attempts);
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

    private static RuntimeHardwareDetectionInfo NormalizeDetection(RuntimeHardwareDetectionInfo detection)
    {
        return new RuntimeHardwareDetectionInfo
        {
            DeviceInfoSource = (detection.DeviceInfoSource ?? "").Trim(),
            WmiDeviceStatus = (detection.WmiDeviceStatus ?? "").Trim(),
            WmiDeviceAttempts = detection.WmiDeviceAttempts
        };
    }

    private static WindowsDeviceInfoQueryResult CreateLegacyQueryResult(Func<DeviceInfo> query)
    {
        ArgumentNullException.ThrowIfNull(query);
        return CreateResult(
            query(),
            "wmi",
            WindowsWmiQueryStatuses.Success,
            1);
    }

    private static WindowsDeviceInfoQueryResult CreateResult(
        DeviceInfo device,
        string source,
        string wmiStatus,
        int attempts)
    {
        return new WindowsDeviceInfoQueryResult
        {
            Device = device ?? new DeviceInfo(),
            Detection = new RuntimeHardwareDetectionInfo
            {
                DeviceInfoSource = (source ?? "").Trim(),
                WmiDeviceStatus = (wmiStatus ?? "").Trim(),
                WmiDeviceAttempts = Math.Max(0, attempts)
            }
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

    private static DeviceInfo TryReadRegistryFallback()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(RegistryBiosPath);
            if (key is null)
            {
                return CreateFallback();
            }

            return CreateDeviceInfoFromBiosRegistryValues(name => ReadRegistryString(key, name));
        }
        catch
        {
            return CreateFallback();
        }
    }

    internal static DeviceInfo CreateDeviceInfoFromBiosRegistryValues(Func<string, string> readValue)
    {
        ArgumentNullException.ThrowIfNull(readValue);

        var manufacturer = FirstMeaningfulValue(
            readValue("SystemManufacturer"),
            readValue("BaseBoardManufacturer"));
        var model = FirstMeaningfulValue(
            readValue("SystemProductName"),
            readValue("BaseBoardProduct"));

        return new DeviceInfo
        {
            Manufacturer = manufacturer,
            Model = model,
            DeviceName = Environment.MachineName ?? ""
        };
    }

    private static string ReadRegistryString(RegistryKey key, string name)
    {
        return key.GetValue(name)?.ToString()?.Trim() ?? "";
    }

    private static string FirstMeaningfulValue(params string[] values)
    {
        foreach (var value in values)
        {
            var normalized = NormalizeRegistryValue(value);
            if (!string.IsNullOrWhiteSpace(normalized))
            {
                return normalized;
            }
        }

        return "";
    }

    private static string NormalizeRegistryValue(string value)
    {
        var normalized = (value ?? "").Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return "";
        }

        return IsPlaceholderRegistryValue(normalized) ? "" : normalized;
    }

    private static bool IsPlaceholderRegistryValue(string value)
    {
        return string.Equals(value, "System Product Name", StringComparison.OrdinalIgnoreCase)
               || string.Equals(value, "To Be Filled By O.E.M.", StringComparison.OrdinalIgnoreCase)
               || string.Equals(value, "Default string", StringComparison.OrdinalIgnoreCase);
    }

    private void LogDetection(RuntimeHardwareDetectionInfo detection)
    {
        _logger.Info(
            LogCategory,
            $"device info source={NormalizeLogValue(detection.DeviceInfoSource, "none")} wmi_status={NormalizeLogValue(detection.WmiDeviceStatus, "none")} attempts={detection.WmiDeviceAttempts}");
    }

    private static string NormalizeLogValue(string value, string fallback)
    {
        var normalized = (value ?? "").Trim();
        return string.IsNullOrWhiteSpace(normalized) ? fallback : normalized;
    }

    private sealed record WmiDeviceRow
    {
        public string Manufacturer { get; init; } = "";
        public string Model { get; init; } = "";
        public string DeviceName { get; init; } = "";
    }
}

internal sealed record WindowsDeviceInfoQueryResult
{
    public DeviceInfo Device { get; init; } = new();
    public RuntimeHardwareDetectionInfo Detection { get; init; } = new();
}
