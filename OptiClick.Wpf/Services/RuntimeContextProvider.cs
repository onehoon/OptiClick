using OptiClick.Core.Runtime;
using OptiClick.Core.Abstractions;
using OptiClick.Wpf.Logging;

namespace OptiClick.Wpf.Services;

public sealed class RuntimeContextProvider : IRuntimeContextProvider
{
    private const string LogCategory = "runtime";
    private static readonly IReadOnlyList<GpuInfo> UnknownGpuFallback =
    [
        new GpuInfo
        {
            Name = "Unknown GPU",
            Vendor = "Unknown",
            AdapterId = "",
            IsPrimary = true
        }
    ];

    private readonly IGpuInfoProvider _gpuProvider;
    private readonly IDeviceInfoProvider _deviceProvider;
    private readonly IAppLanguageProvider _languageProvider;
    private readonly IRemoteEndpointProvider _remoteEndpointProvider;
    private readonly IAppLogger? _logger;

    public RuntimeContextProvider(
        IGpuInfoProvider gpuProvider,
        IDeviceInfoProvider deviceProvider,
        IAppLanguageProvider languageProvider,
        IRemoteEndpointProvider remoteEndpointProvider,
        IAppLogger? logger = null)
    {
        _gpuProvider = gpuProvider;
        _deviceProvider = deviceProvider;
        _languageProvider = languageProvider;
        _remoteEndpointProvider = remoteEndpointProvider;
        _logger = logger;
    }

    public RuntimeContext GetRuntimeContext()
    {
        var gpuResult = new RuntimeGpuDetectionResult(
            UnknownGpuFallback,
            new RuntimeHardwareDetectionInfo
            {
                GpuInfoSource = "fallback",
                WmiGpuStatus = "exception"
            });
        var deviceResult = new RuntimeDeviceDetectionResult(
            new DeviceInfo(),
            new RuntimeHardwareDetectionInfo
            {
                DeviceInfoSource = "fallback",
                WmiDeviceStatus = "exception"
            });

        Parallel.Invoke(
            () => gpuResult = GetSafeGpus(),
            () => deviceResult = GetSafeDevice());

        var gpus = gpuResult.Gpus;
        var selectedGpu = SelectSingleGpu(gpus);
        return new RuntimeContext
        {
            Gpus = gpus,
            SelectedGpu = selectedGpu,
            Device = deviceResult.Device,
            Language = GetSafeLanguage(),
            RemoteData = GetSafeRemoteData(),
            HardwareDetection = MergeHardwareDetection(deviceResult.Detection, gpuResult.Detection)
        };
    }

    private RuntimeGpuDetectionResult GetSafeGpus()
    {
        try
        {
            var gpus = _gpuProvider.GetGpus();
            var safeGpus = gpus is null || gpus.Count == 0 ? UnknownGpuFallback : gpus;
            var detection = GetProviderDetectionInfo(_gpuProvider);
            if (string.IsNullOrWhiteSpace(detection.GpuInfoSource) && ReferenceEquals(safeGpus, UnknownGpuFallback))
            {
                detection = detection with { GpuInfoSource = "fallback" };
            }

            return new RuntimeGpuDetectionResult(safeGpus, detection);
        }
        catch (Exception ex)
        {
            _logger?.Warning(LogCategory, "gpu provider failed; using unknown fallback");
            _logger?.Error(LogCategory, "gpu provider exception", ex);
            return new RuntimeGpuDetectionResult(
                UnknownGpuFallback,
                new RuntimeHardwareDetectionInfo
                {
                    GpuInfoSource = "fallback",
                    WmiGpuStatus = "exception",
                    WmiGpuErrorType = ex.GetType().Name,
                    GpuDetectionErrorType = $"wmi:{ex.GetType().Name}"
                });
        }
    }

    private RuntimeDeviceDetectionResult GetSafeDevice()
    {
        try
        {
            var device = _deviceProvider.GetDeviceInfo() ?? new DeviceInfo();
            return new RuntimeDeviceDetectionResult(device, GetProviderDetectionInfo(_deviceProvider));
        }
        catch (Exception ex)
        {
            _logger?.Warning(LogCategory, "device provider failed; using empty device info");
            _logger?.Error(LogCategory, "device provider exception", ex);
            return new RuntimeDeviceDetectionResult(
                new DeviceInfo(),
                new RuntimeHardwareDetectionInfo
                {
                    DeviceInfoSource = "fallback",
                    WmiDeviceStatus = "exception"
                });
        }
    }

    private AppLanguage GetSafeLanguage()
    {
        try
        {
            return _languageProvider.CurrentLanguage;
        }
        catch (Exception ex)
        {
            _logger?.Warning(LogCategory, "language provider failed; using english fallback");
            _logger?.Error(LogCategory, "language provider exception", ex);
            return AppLanguage.English;
        }
    }

    private RemoteDataOptions GetSafeRemoteData()
    {
        try
        {
            return _remoteEndpointProvider.GetRemoteDataOptions() ?? new RemoteDataOptions();
        }
        catch (Exception ex)
        {
            _logger?.Warning(LogCategory, "remote endpoint provider failed; using empty remote data options");
            _logger?.Error(LogCategory, "remote endpoint provider exception", ex);
            return new RemoteDataOptions();
        }
    }

    private static GpuInfo? SelectSingleGpu(IReadOnlyList<GpuInfo> gpus)
    {
        return gpus.Count == 1 ? gpus[0] : null;
    }

    private static RuntimeHardwareDetectionInfo GetProviderDetectionInfo(object provider)
    {
        return provider is IRuntimeHardwareDetectionInfoProvider diagnosticsProvider
            ? diagnosticsProvider.GetHardwareDetectionInfo() ?? new RuntimeHardwareDetectionInfo()
            : new RuntimeHardwareDetectionInfo();
    }

    private static RuntimeHardwareDetectionInfo MergeHardwareDetection(
        RuntimeHardwareDetectionInfo device,
        RuntimeHardwareDetectionInfo gpu)
    {
        return new RuntimeHardwareDetectionInfo
        {
            DeviceInfoSource = (device.DeviceInfoSource ?? "").Trim(),
            GpuInfoSource = (gpu.GpuInfoSource ?? "").Trim(),
            WmiDeviceStatus = (device.WmiDeviceStatus ?? "").Trim(),
            WmiGpuStatus = (gpu.WmiGpuStatus ?? "").Trim(),
            WmiGpuErrorType = (gpu.WmiGpuErrorType ?? "").Trim(),
            WmiDeviceAttempts = Math.Max(0, device.WmiDeviceAttempts),
            WmiGpuAttempts = Math.Max(0, gpu.WmiGpuAttempts),
            DxgiGpuStatus = (gpu.DxgiGpuStatus ?? "").Trim(),
            DxgiGpuCount = Math.Max(0, gpu.DxgiGpuCount),
            GpuDetectionErrorType = (gpu.GpuDetectionErrorType ?? "").Trim()
        };
    }

    private sealed record RuntimeGpuDetectionResult(
        IReadOnlyList<GpuInfo> Gpus,
        RuntimeHardwareDetectionInfo Detection);

    private sealed record RuntimeDeviceDetectionResult(
        DeviceInfo Device,
        RuntimeHardwareDetectionInfo Detection);
}
