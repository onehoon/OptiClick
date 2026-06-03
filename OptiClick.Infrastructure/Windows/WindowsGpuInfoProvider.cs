using OptiClick.Core.Runtime;
using OptiClick.Core.Abstractions;
using OptiClick.Infrastructure.Logging;

namespace OptiClick.Infrastructure.Windows;

public sealed class WindowsGpuInfoProvider : IGpuInfoProvider
{
    private readonly Func<IReadOnlyList<GpuInfo>> _query;
    private readonly IAppLogger _logger;

    public WindowsGpuInfoProvider()
        : this(QueryGpuInfos, null)
    {
    }

    public WindowsGpuInfoProvider(IAppLogger? logger)
        : this(QueryGpuInfos, logger)
    {
    }

    public WindowsGpuInfoProvider(Func<IReadOnlyList<GpuInfo>> query)
        : this(query, null)
    {
    }

    public WindowsGpuInfoProvider(Func<IReadOnlyList<GpuInfo>> query, IAppLogger? logger)
    {
        _query = query ?? throw new ArgumentNullException(nameof(query));
        _logger = logger ?? NullAppLogger.Instance;
    }

    public IReadOnlyList<GpuInfo> GetGpus()
    {
        try
        {
            var gpus = _query();
            if (gpus is null || gpus.Count == 0)
            {
                _logger.Warning("runtime-gpu", "gpu query returned empty result; using Unknown GPU fallback.");
                return CreateUnknownGpuFallback();
            }

            var supportedGpus = FilterSupportedGpus(gpus);
            if (supportedGpus.Count == 0)
            {
                _logger.Warning("runtime-gpu", "gpu query returned only unsupported vendors; using Unknown GPU fallback.");
                return CreateUnknownGpuFallback();
            }

            return EnsurePrimaryGpu(supportedGpus);
        }
        catch (Exception exception)
        {
            _logger.Error("runtime-gpu", "gpu query failed; using Unknown GPU fallback.", exception);
            return CreateUnknownGpuFallback();
        }
    }

    private static IReadOnlyList<GpuInfo> QueryGpuInfos()
    {
        if (!OperatingSystem.IsWindows())
        {
            return CreateUnknownGpuFallback();
        }

        var results = new List<GpuInfo>();
        var dedupe = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var rows = WindowsWmiQueryHelper.Query(
            "SELECT Name, PNPDeviceID, AdapterCompatibility, VideoProcessor FROM Win32_VideoController",
            static item =>
            new WmiGpuRow
            {
                Name = WindowsWmiQueryHelper.ReadString(item, "Name"),
                AdapterId = WindowsWmiQueryHelper.ReadString(item, "PNPDeviceID"),
                AdapterCompatibility = WindowsWmiQueryHelper.ReadString(item, "AdapterCompatibility"),
                VideoProcessor = WindowsWmiQueryHelper.ReadString(item, "VideoProcessor")
            });

        foreach (var row in rows)
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

        return results.Count == 0 ? CreateUnknownGpuFallback() : EnsurePrimaryGpu(results);
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

    private sealed record WmiGpuRow
    {
        public string Name { get; init; } = "";
        public string AdapterId { get; init; } = "";
        public string AdapterCompatibility { get; init; } = "";
        public string VideoProcessor { get; init; } = "";
    }
}
