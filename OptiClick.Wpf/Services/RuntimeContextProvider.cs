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
        var gpus = GetSafeGpus();
        var selectedGpu = SelectSingleGpu(gpus);
        return new RuntimeContext
        {
            Gpus = gpus,
            SelectedGpu = selectedGpu,
            Device = GetSafeDevice(),
            Language = GetSafeLanguage(),
            RemoteData = GetSafeRemoteData()
        };
    }

    private IReadOnlyList<GpuInfo> GetSafeGpus()
    {
        try
        {
            var gpus = _gpuProvider.GetGpus();
            return gpus is null || gpus.Count == 0 ? UnknownGpuFallback : gpus;
        }
        catch (Exception ex)
        {
            _logger?.Warning(LogCategory, "gpu provider failed; using unknown fallback");
            _logger?.Error(LogCategory, "gpu provider exception", ex);
            return UnknownGpuFallback;
        }
    }

    private DeviceInfo GetSafeDevice()
    {
        try
        {
            return _deviceProvider.GetDeviceInfo() ?? new DeviceInfo();
        }
        catch (Exception ex)
        {
            _logger?.Warning(LogCategory, "device provider failed; using empty device info");
            _logger?.Error(LogCategory, "device provider exception", ex);
            return new DeviceInfo();
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
}
