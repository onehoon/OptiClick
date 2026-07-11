using System.Text.RegularExpressions;
using OptiClick.Core.Runtime;

namespace OptiClick.Wpf.Shell.RemoteV2;

public static class AuthV2GpuCandidateFilter
{
    // This filter only removes permanently unsupported GPU families before Auth V2.
    // It is not a replacement for server-side Resolve V2 manifest matching.
    // Supported GPU family and bundle selection remain server-driven.
    private static readonly Regex XeTokenRegex = new(@"(^|\W)xe(\W|$)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex UhdGraphicsRegex = new(@"(^|\W)uhd\s+graphics(\W|$)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex GtxTokenRegex = new(@"(^|\W)gtx(\W|$)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static IReadOnlyList<GpuInfo> FilterUploadCandidates(IReadOnlyList<GpuInfo>? gpus)
    {
        if (gpus is null || gpus.Count == 0)
        {
            return [];
        }

        var filtered = new List<GpuInfo>(gpus.Count);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var gpu in gpus)
        {
            var name = NormalizeWhitespace(gpu.Name);
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            var vendor = NormalizeVendor(gpu.Vendor, name);
            if (!IsUploadSupported(vendor, name))
            {
                continue;
            }

            var normalized = gpu with
            {
                Name = name,
                Vendor = ToDisplayVendor(vendor),
                AdapterId = NormalizeWhitespace(gpu.AdapterId)
            };
            var key = $"{vendor}|{name}";
            if (seen.Add(key))
            {
                filtered.Add(normalized);
            }
        }

        return EnsurePrimary(filtered);
    }

    public static string NormalizeVendor(string? vendor, string? gpuName)
    {
        var candidate = $"{vendor ?? ""} {gpuName ?? ""}".Trim().ToLowerInvariant();
        if (candidate.Contains("nvidia", StringComparison.Ordinal))
        {
            return "nvidia";
        }

        if (candidate.Contains("intel", StringComparison.Ordinal))
        {
            return "intel";
        }

        if (candidate.Contains("amd", StringComparison.Ordinal) || candidate.Contains("radeon", StringComparison.Ordinal))
        {
            return "amd";
        }

        return "";
    }

    public static bool IsUploadSupported(string vendor, string gpuName)
    {
        var normalizedVendor = (vendor ?? "").Trim().ToLowerInvariant();
        var normalizedName = NormalizeWhitespace(gpuName).ToLowerInvariant();
        if (normalizedVendor is not ("amd" or "intel" or "nvidia"))
        {
            return false;
        }

        if (normalizedVendor == "intel")
        {
            return !normalizedName.Contains("iris", StringComparison.Ordinal)
                   && !XeTokenRegex.IsMatch(normalizedName)
                   && !UhdGraphicsRegex.IsMatch(normalizedName);
        }

        if (normalizedVendor == "nvidia")
        {
            return !GtxTokenRegex.IsMatch(normalizedName);
        }

        return true;
    }

    public static string NormalizeWhitespace(string? value)
    {
        return string.Join(" ", (value ?? "").Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)).Trim();
    }

    private static IReadOnlyList<GpuInfo> EnsurePrimary(IReadOnlyList<GpuInfo> gpus)
    {
        if (gpus.Count == 0 || gpus.Any(static gpu => gpu.IsPrimary))
        {
            return gpus;
        }

        var list = new List<GpuInfo>(gpus.Count);
        for (var index = 0; index < gpus.Count; index++)
        {
            list.Add(gpus[index] with { IsPrimary = index == 0 });
        }

        return list;
    }

    private static string ToDisplayVendor(string vendor)
    {
        return vendor switch
        {
            "amd" => "AMD",
            "intel" => "Intel",
            "nvidia" => "NVIDIA",
            _ => ""
        };
    }
}
