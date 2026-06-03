using OptiClick.Core.Models;
using OptiClick.Core.Runtime;

namespace OptiClick.Wpf.Shell.Games.Support;

public static class GpuVendorDetector
{
    private static readonly string[] NvidiaKeywords = ["nvidia", "geforce", "rtx"];
    private static readonly string[] AmdKeywords = ["amd", "radeon"];
    private static readonly string[] IntelKeywords = ["intel", "arc"];

    public static GpuVendor DetectFromText(string text)
    {
        var normalized = Normalize(text);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return GpuVendor.Unknown;
        }

        if (ContainsAny(normalized, NvidiaKeywords))
        {
            return GpuVendor.Nvidia;
        }

        if (ContainsAny(normalized, AmdKeywords))
        {
            return GpuVendor.Amd;
        }

        if (ContainsAny(normalized, IntelKeywords))
        {
            return GpuVendor.Intel;
        }

        return GpuVendor.Unknown;
    }

    public static GpuVendor DetectFromRuntimeContext(RuntimeContext? runtimeContext)
    {
        var selected = runtimeContext?.SelectedGpu;
        if (selected is not null)
        {
            var selectedDetected = DetectFromGpuInfo(selected);
            if (selectedDetected != GpuVendor.Unknown)
            {
                return selectedDetected;
            }
        }

        var gpus = BuildDistinctGpuCandidates(runtimeContext?.Gpus);
        if (gpus.Count != 1)
        {
            return GpuVendor.Unknown;
        }

        return DetectFromGpuInfo(gpus[0]);
    }

    private static GpuVendor DetectFromGpuInfo(GpuInfo gpu)
    {
        var fromVendor = DetectFromText(gpu.Vendor);
        if (fromVendor != GpuVendor.Unknown)
        {
            return fromVendor;
        }

        return DetectFromText(gpu.Name);
    }

    private static string Normalize(string text)
    {
        return string.Join(" ", (text ?? "").Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)).Trim().ToLowerInvariant();
    }

    private static bool ContainsAny(string source, IEnumerable<string> keywords)
    {
        foreach (var keyword in keywords)
        {
            if (source.Contains(keyword, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static IReadOnlyList<GpuInfo> BuildDistinctGpuCandidates(IReadOnlyList<GpuInfo>? gpus)
    {
        if (gpus is null || gpus.Count == 0)
        {
            return [];
        }

        var list = new List<GpuInfo>(gpus.Count);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var gpu in gpus)
        {
            var name = Normalize(gpu.Name);
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            var vendor = Normalize(gpu.Vendor);
            var key = $"{vendor}|{name}";
            if (seen.Add(key))
            {
                list.Add(gpu);
            }
        }

        return list;
    }
}
