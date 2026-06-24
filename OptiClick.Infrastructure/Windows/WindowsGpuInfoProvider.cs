using OptiClick.Core.Runtime;
using OptiClick.Core.Abstractions;
using OptiClick.Infrastructure.Logging;

namespace OptiClick.Infrastructure.Windows;

public sealed class WindowsGpuInfoProvider : IGpuInfoProvider, IRuntimeHardwareDetectionInfoProvider
{
    private const string LogCategory = "runtime-gpu";

    private readonly Func<WindowsGpuInfoQueryResult> _wmiQuery;
    private readonly Func<WindowsDxgiGpuQueryResult> _dxgiQuery;
    private readonly IAppLogger _logger;
    private RuntimeHardwareDetectionInfo _detectionInfo = new();

    public WindowsGpuInfoProvider()
        : this((IAppLogger?)null)
    {
    }

    public WindowsGpuInfoProvider(IAppLogger? logger)
        : this(
            () => QueryWmiGpuInfos(logger),
            () => new WindowsDxgiGpuInfoProvider(logger).Query(),
            logger)
    {
    }

    public WindowsGpuInfoProvider(Func<IReadOnlyList<GpuInfo>> query)
        : this(() => CreateLegacyQueryResult(query), null, true)
    {
    }

    public WindowsGpuInfoProvider(Func<IReadOnlyList<GpuInfo>> query, IAppLogger? logger)
        : this(() => CreateLegacyQueryResult(query), logger, true)
    {
    }

    internal WindowsGpuInfoProvider(Func<WindowsGpuInfoQueryResult> query, IAppLogger? logger, bool useQueryResult)
        : this(
            query,
            static () => new WindowsDxgiGpuQueryResult { Status = WindowsDxgiQueryStatuses.NotAttempted },
            logger)
    {
    }

    internal WindowsGpuInfoProvider(
        Func<WindowsGpuInfoQueryResult> wmiQuery,
        Func<WindowsDxgiGpuQueryResult> dxgiQuery,
        IAppLogger? logger)
    {
        _wmiQuery = wmiQuery ?? throw new ArgumentNullException(nameof(wmiQuery));
        _dxgiQuery = dxgiQuery ?? throw new ArgumentNullException(nameof(dxgiQuery));
        _logger = logger ?? NullAppLogger.Instance;
    }

    public IReadOnlyList<GpuInfo> GetGpus()
    {
        try
        {
            var wmiResult = QueryWmiSafely();
            var wmiDetection = NormalizeDetection(wmiResult.Detection);
            var wmiSupportedGpus = FilterSupportedGpus(wmiResult.Gpus);
            if (wmiSupportedGpus.Count > 0)
            {
                _detectionInfo = wmiDetection with
                {
                    GpuInfoSource = string.IsNullOrWhiteSpace(wmiDetection.GpuInfoSource)
                        ? "wmi"
                        : wmiDetection.GpuInfoSource,
                    DxgiGpuStatus = WindowsDxgiQueryStatuses.NotAttempted,
                    DxgiGpuCount = 0,
                    GpuDetectionErrorType = ""
                };
                LogDetection(_detectionInfo, wmiSupportedGpus.Count);
                return EnsurePrimaryGpu(wmiSupportedGpus);
            }

            var dxgiResult = QueryDxgiSafely();
            var dxgiStatus = NormalizeDxgiStatus(dxgiResult);
            var dxgiSupportedGpus = FilterSupportedGpus(dxgiResult.Gpus);
            if (dxgiSupportedGpus.Count > 0)
            {
                _detectionInfo = wmiDetection with
                {
                    GpuInfoSource = "dxgi",
                    DxgiGpuStatus = dxgiStatus,
                    DxgiGpuCount = Math.Max(0, dxgiResult.AdapterCount),
                    GpuDetectionErrorType = ResolveGpuDetectionErrorType(wmiDetection, dxgiStatus, dxgiResult.ErrorType)
                };
                LogDetection(_detectionInfo, dxgiSupportedGpus.Count);
                return EnsurePrimaryGpu(dxgiSupportedGpus);
            }

            _detectionInfo = wmiDetection with
            {
                GpuInfoSource = "fallback",
                WmiGpuStatus = string.IsNullOrWhiteSpace(wmiDetection.WmiGpuStatus)
                    ? WindowsWmiQueryStatuses.Exception
                    : wmiDetection.WmiGpuStatus,
                DxgiGpuStatus = dxgiStatus,
                DxgiGpuCount = Math.Max(0, dxgiResult.AdapterCount),
                GpuDetectionErrorType = ResolveGpuDetectionErrorType(wmiDetection, dxgiStatus, dxgiResult.ErrorType)
            };
            _logger.Warning(LogCategory, "gpu detection failed; using Unknown GPU fallback.");
            LogDetection(_detectionInfo, 0);
            return CreateUnknownGpuFallback();
        }
        catch (Exception exception)
        {
            _detectionInfo = new RuntimeHardwareDetectionInfo
            {
                GpuInfoSource = "fallback",
                WmiGpuStatus = WindowsWmiQueryStatuses.Exception,
                WmiGpuAttempts = 0,
                WmiGpuErrorType = exception.GetType().Name,
                DxgiGpuStatus = WindowsDxgiQueryStatuses.NotAttempted,
                GpuDetectionErrorType = PrefixErrorType("wmi", exception.GetType().Name)
            };
            _logger.Error(LogCategory, "gpu query failed; using Unknown GPU fallback.", exception);
            return CreateUnknownGpuFallback();
        }
    }

    public RuntimeHardwareDetectionInfo GetHardwareDetectionInfo()
    {
        return _detectionInfo;
    }

    private static WindowsGpuInfoQueryResult QueryWmiGpuInfos(IAppLogger? logger = null)
    {
        if (!OperatingSystem.IsWindows())
        {
            return CreateResult(
                [],
                "fallback",
                WindowsWmiQueryStatuses.NonWindows,
                0);
        }

        var results = new List<GpuInfo>();
        var dedupe = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var wmiResult = WindowsWmiQueryHelper.QueryWithResult(
            "SELECT Name, PNPDeviceID, AdapterCompatibility, VideoProcessor FROM Win32_VideoController",
            static item =>
            new WmiGpuRow
            {
                Name = WindowsWmiQueryHelper.ReadString(item, "Name"),
                AdapterId = WindowsWmiQueryHelper.ReadString(item, "PNPDeviceID"),
                AdapterCompatibility = WindowsWmiQueryHelper.ReadString(item, "AdapterCompatibility"),
                VideoProcessor = WindowsWmiQueryHelper.ReadString(item, "VideoProcessor")
            },
            new WindowsWmiQueryOptions
            {
                SourceName = "Win32_VideoController",
                LogCategory = LogCategory,
                Logger = logger
            });

        if (wmiResult.Status != WindowsWmiQueryStatuses.Success)
        {
            return CreateResult(
                [],
                "fallback",
                wmiResult.Status,
                wmiResult.Attempts,
                wmiResult.ErrorType);
        }

        foreach (var row in wmiResult.Rows)
        {
            var vendor = ResolveVendor(row.AdapterCompatibility, row.Name, row.VideoProcessor);
            if (!IsSupportedVendor(vendor))
            {
                continue;
            }

            var normalizedName = string.IsNullOrWhiteSpace(row.Name) ? "Unknown GPU" : row.Name.Trim();
            var dedupeKey = string.IsNullOrWhiteSpace(row.AdapterId)
                ? normalizedName
                : row.AdapterId.Trim();

            if (!dedupe.Add(dedupeKey))
            {
                continue;
            }

            results.Add(new GpuInfo
            {
                Name = normalizedName,
                Vendor = vendor,
                AdapterId = row.AdapterId.Trim(),
                IsPrimary = false
            });
        }

        return CreateResult(
            results,
            results.Count == 0 ? "fallback" : "wmi",
            wmiResult.Status,
            wmiResult.Attempts);
    }

    private static IReadOnlyList<GpuInfo> FilterSupportedGpus(IReadOnlyList<GpuInfo> gpus)
    {
        return gpus.Where(static gpu => IsSupportedVendor(gpu.Vendor)).ToArray();
    }

    private WindowsGpuInfoQueryResult QueryWmiSafely()
    {
        try
        {
            return _wmiQuery();
        }
        catch (Exception exception)
        {
            return new WindowsGpuInfoQueryResult
            {
                Gpus = [],
                Detection = new RuntimeHardwareDetectionInfo
                {
                    GpuInfoSource = "fallback",
                    WmiGpuStatus = WindowsWmiQueryStatuses.Exception,
                    WmiGpuAttempts = 0,
                    WmiGpuErrorType = exception.GetType().Name,
                    GpuDetectionErrorType = PrefixErrorType("wmi", exception.GetType().Name)
                }
            };
        }
    }

    private WindowsDxgiGpuQueryResult QueryDxgiSafely()
    {
        try
        {
            return _dxgiQuery();
        }
        catch (Exception exception)
        {
            return new WindowsDxgiGpuQueryResult
            {
                Status = WindowsDxgiQueryStatuses.Exception,
                ErrorType = exception.GetType().Name
            };
        }
    }

    private static IReadOnlyList<GpuInfo> EnsurePrimaryGpu(IReadOnlyList<GpuInfo> gpus)
    {
        if (gpus.Count == 0)
        {
            return CreateUnknownGpuFallback();
        }

        if (gpus.Any(static gpu => gpu.IsPrimary))
        {
            return gpus;
        }

        var normalized = new List<GpuInfo>(gpus.Count);
        for (var index = 0; index < gpus.Count; index++)
        {
            var gpu = gpus[index];
            normalized.Add(gpu with { IsPrimary = index == 0 });
        }

        return normalized;
    }

    private static IReadOnlyList<GpuInfo> CreateUnknownGpuFallback()
    {
        return
        [
            new GpuInfo
            {
                Name = "Unknown GPU",
                Vendor = "Unknown",
                AdapterId = "",
                IsPrimary = true
            }
        ];
    }

    private static WindowsGpuInfoQueryResult CreateLegacyQueryResult(Func<IReadOnlyList<GpuInfo>> query)
    {
        ArgumentNullException.ThrowIfNull(query);
        return CreateResult(
            query(),
            "wmi",
            WindowsWmiQueryStatuses.Success,
            1);
    }

    private static WindowsGpuInfoQueryResult CreateResult(
        IReadOnlyList<GpuInfo> gpus,
        string source,
        string wmiStatus,
        int attempts,
        string wmiErrorType = "")
    {
        return new WindowsGpuInfoQueryResult
        {
            Gpus = gpus ?? [],
            Detection = new RuntimeHardwareDetectionInfo
            {
                GpuInfoSource = (source ?? "").Trim(),
                WmiGpuStatus = (wmiStatus ?? "").Trim(),
                WmiGpuErrorType = (wmiErrorType ?? "").Trim(),
                WmiGpuAttempts = Math.Max(0, attempts),
                GpuDetectionErrorType = PrefixErrorType("wmi", wmiErrorType)
            }
        };
    }

    private static RuntimeHardwareDetectionInfo NormalizeDetection(RuntimeHardwareDetectionInfo detection)
    {
        return new RuntimeHardwareDetectionInfo
        {
            GpuInfoSource = (detection.GpuInfoSource ?? "").Trim(),
            WmiGpuStatus = (detection.WmiGpuStatus ?? "").Trim(),
            WmiGpuErrorType = (detection.WmiGpuErrorType ?? "").Trim(),
            WmiGpuAttempts = detection.WmiGpuAttempts,
            DxgiGpuStatus = (detection.DxgiGpuStatus ?? "").Trim(),
            DxgiGpuCount = Math.Max(0, detection.DxgiGpuCount),
            GpuDetectionErrorType = (detection.GpuDetectionErrorType ?? "").Trim()
        };
    }

    private static string NormalizeDxgiStatus(WindowsDxgiGpuQueryResult result)
    {
        var status = (result.Status ?? "").Trim();
        if (string.IsNullOrWhiteSpace(status))
        {
            return WindowsDxgiQueryStatuses.Exception;
        }

        return string.Equals(status, WindowsDxgiQueryStatuses.Empty, StringComparison.OrdinalIgnoreCase)
               && result.AdapterCount > 0
            ? WindowsDxgiQueryStatuses.UnsupportedOnly
            : status;
    }

    private static string ResolveGpuDetectionErrorType(
        RuntimeHardwareDetectionInfo wmiDetection,
        string dxgiStatus,
        string dxgiErrorType)
    {
        if (!string.IsNullOrWhiteSpace(dxgiErrorType))
        {
            return PrefixErrorType("dxgi", dxgiErrorType);
        }

        if (!string.IsNullOrWhiteSpace(wmiDetection.WmiGpuErrorType)
            && !string.Equals(wmiDetection.WmiGpuStatus, WindowsWmiQueryStatuses.Success, StringComparison.OrdinalIgnoreCase))
        {
            return PrefixErrorType("wmi", wmiDetection.WmiGpuErrorType);
        }

        if (!string.IsNullOrWhiteSpace(wmiDetection.GpuDetectionErrorType)
            && !string.Equals(wmiDetection.WmiGpuStatus, WindowsWmiQueryStatuses.Success, StringComparison.OrdinalIgnoreCase))
        {
            return PrefixErrorType("wmi", wmiDetection.GpuDetectionErrorType);
        }

        if (!string.IsNullOrWhiteSpace(wmiDetection.WmiGpuStatus)
            && !string.Equals(wmiDetection.WmiGpuStatus, WindowsWmiQueryStatuses.Success, StringComparison.OrdinalIgnoreCase))
        {
            return PrefixErrorType("wmi", wmiDetection.WmiGpuStatus);
        }

        if (!string.IsNullOrWhiteSpace(dxgiStatus)
            && !string.Equals(dxgiStatus, WindowsDxgiQueryStatuses.Success, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(dxgiStatus, WindowsDxgiQueryStatuses.NotAttempted, StringComparison.OrdinalIgnoreCase))
        {
            return PrefixErrorType("dxgi", dxgiStatus);
        }

        return "";
    }

    private void LogDetection(RuntimeHardwareDetectionInfo detection, int gpuCount)
    {
        _logger.Info(
            LogCategory,
            $"gpu detection completed source={NormalizeLogValue(detection.GpuInfoSource, "none")} wmi_status={NormalizeLogValue(detection.WmiGpuStatus, "none")} wmi_error_type={NormalizeLogValue(detection.WmiGpuErrorType, "none")} wmi_attempts={detection.WmiGpuAttempts} dxgi_status={NormalizeLogValue(detection.DxgiGpuStatus, "none")} dxgi_count={detection.DxgiGpuCount} gpu_detection_error_type={NormalizeLogValue(detection.GpuDetectionErrorType, "none")} gpu_count={gpuCount}");
    }

    private static string ResolveVendor(string adapterCompatibility, string name, string videoProcessor)
    {
        var source = string.Join(
            " ",
            new[] { adapterCompatibility, name, videoProcessor }
                .Where(static value => !string.IsNullOrWhiteSpace(value)));

        if (Contains(source, "NVIDIA"))
        {
            return "NVIDIA";
        }

        if (Contains(source, "AMD") || Contains(source, "RADEON") || Contains(source, "ADVANCED MICRO DEVICES"))
        {
            return "AMD";
        }

        if (Contains(source, "INTEL"))
        {
            return "Intel";
        }

        return "Unknown";
    }

    private static bool IsSupportedVendor(string vendor)
    {
        return string.Equals(vendor, "NVIDIA", StringComparison.OrdinalIgnoreCase)
               || string.Equals(vendor, "AMD", StringComparison.OrdinalIgnoreCase)
               || string.Equals(vendor, "Intel", StringComparison.OrdinalIgnoreCase);
    }

    private static bool Contains(string source, string value)
    {
        return source.Contains(value, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeLogValue(string? value, string fallback)
    {
        var normalized = (value ?? "").Trim();
        return string.IsNullOrWhiteSpace(normalized) ? fallback : normalized;
    }

    private static string PrefixErrorType(string source, string? errorType)
    {
        var normalized = (errorType ?? "").Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return "";
        }

        if (normalized.StartsWith("wmi:", StringComparison.OrdinalIgnoreCase)
            || normalized.StartsWith("dxgi:", StringComparison.OrdinalIgnoreCase))
        {
            return normalized;
        }

        return $"{source}:{normalized}";
    }

    private sealed record WmiGpuRow
    {
        public string Name { get; init; } = "";
        public string AdapterId { get; init; } = "";
        public string AdapterCompatibility { get; init; } = "";
        public string VideoProcessor { get; init; } = "";
    }
}

internal sealed record WindowsGpuInfoQueryResult
{
    public IReadOnlyList<GpuInfo> Gpus { get; init; } = [];
    public RuntimeHardwareDetectionInfo Detection { get; init; } = new();
}
