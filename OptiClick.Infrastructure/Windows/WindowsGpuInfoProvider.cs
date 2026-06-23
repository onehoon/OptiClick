using OptiClick.Core.Runtime;
using OptiClick.Core.Abstractions;
using OptiClick.Infrastructure.Logging;

namespace OptiClick.Infrastructure.Windows;

public sealed class WindowsGpuInfoProvider : IGpuInfoProvider, IRuntimeHardwareDetectionInfoProvider
{
    private const string LogCategory = "runtime-gpu";

    private readonly Func<WindowsGpuInfoQueryResult> _query;
    private readonly IAppLogger _logger;
    private RuntimeHardwareDetectionInfo _detectionInfo = new();

    public WindowsGpuInfoProvider()
        : this(QueryGpuInfos, null, true)
    {
    }

    public WindowsGpuInfoProvider(IAppLogger? logger)
        : this(QueryGpuInfos, logger, true)
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
    {
        _query = query ?? throw new ArgumentNullException(nameof(query));
        _logger = logger ?? NullAppLogger.Instance;
    }

    public IReadOnlyList<GpuInfo> GetGpus()
    {
        try
        {
            var queryResult = _query();
            var gpus = queryResult.Gpus;
            _detectionInfo = NormalizeDetection(queryResult.Detection);
            if (gpus is null || gpus.Count == 0)
            {
                _detectionInfo = _detectionInfo with { GpuInfoSource = "fallback" };
                _logger.Warning(LogCategory, "gpu query returned empty result; using Unknown GPU fallback.");
                LogDetection(_detectionInfo);
                return CreateUnknownGpuFallback();
            }

            var supportedGpus = FilterSupportedGpus(gpus);
            if (supportedGpus.Count == 0)
            {
                _detectionInfo = _detectionInfo with
                {
                    GpuInfoSource = "fallback",
                    WmiGpuStatus = string.IsNullOrWhiteSpace(_detectionInfo.WmiGpuStatus)
                        ? WindowsWmiQueryStatuses.Success
                        : _detectionInfo.WmiGpuStatus
                };
                _logger.Warning(LogCategory, "gpu query returned only unsupported vendors; using Unknown GPU fallback.");
                LogDetection(_detectionInfo);
                return CreateUnknownGpuFallback();
            }

            _detectionInfo = _detectionInfo with
            {
                GpuInfoSource = string.IsNullOrWhiteSpace(_detectionInfo.GpuInfoSource)
                    ? "wmi"
                    : _detectionInfo.GpuInfoSource
            };
            LogDetection(_detectionInfo);
            return EnsurePrimaryGpu(supportedGpus);
        }
        catch (Exception exception)
        {
            _detectionInfo = new RuntimeHardwareDetectionInfo
            {
                GpuInfoSource = "fallback",
                WmiGpuStatus = WindowsWmiQueryStatuses.Exception,
                WmiGpuAttempts = 0
            };
            _logger.Error(LogCategory, "gpu query failed; using Unknown GPU fallback.", exception);
            return CreateUnknownGpuFallback();
        }
    }

    public RuntimeHardwareDetectionInfo GetHardwareDetectionInfo()
    {
        return _detectionInfo;
    }

    private static WindowsGpuInfoQueryResult QueryGpuInfos()
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
            new WindowsWmiQueryOptions { SourceName = "Win32_VideoController" });

        if (wmiResult.Status != WindowsWmiQueryStatuses.Success)
        {
            return CreateResult(
                [],
                "fallback",
                wmiResult.Status,
                wmiResult.Attempts);
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
        int attempts)
    {
        return new WindowsGpuInfoQueryResult
        {
            Gpus = gpus ?? [],
            Detection = new RuntimeHardwareDetectionInfo
            {
                GpuInfoSource = (source ?? "").Trim(),
                WmiGpuStatus = (wmiStatus ?? "").Trim(),
                WmiGpuAttempts = Math.Max(0, attempts)
            }
        };
    }

    private static RuntimeHardwareDetectionInfo NormalizeDetection(RuntimeHardwareDetectionInfo detection)
    {
        return new RuntimeHardwareDetectionInfo
        {
            GpuInfoSource = (detection.GpuInfoSource ?? "").Trim(),
            WmiGpuStatus = (detection.WmiGpuStatus ?? "").Trim(),
            WmiGpuAttempts = detection.WmiGpuAttempts
        };
    }

    private void LogDetection(RuntimeHardwareDetectionInfo detection)
    {
        _logger.Info(
            LogCategory,
            $"gpu info source={NormalizeLogValue(detection.GpuInfoSource, "none")} wmi_status={NormalizeLogValue(detection.WmiGpuStatus, "none")} attempts={detection.WmiGpuAttempts}");
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
