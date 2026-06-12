using OptiClick.Core.Runtime;

namespace OptiClick.Wpf.Shell.Games.GpuBundle;

public static class GpuBundleManifestFetchRequestFactory
{
    public static GpuBundleManifestFetchRequest Create(
        RuntimeContext runtimeContext,
        GpuInfo? selectedGpu = null,
        string appVersion = "")
    {
        var safeRuntimeContext = runtimeContext ?? new RuntimeContext();
        var gpu = selectedGpu
                  ?? safeRuntimeContext.SelectedGpu
                  ?? safeRuntimeContext.Gpus?.FirstOrDefault(static candidate => candidate.IsPrimary)
                  ?? safeRuntimeContext.Gpus?.FirstOrDefault()
                  ?? new GpuInfo();

        return new GpuBundleManifestFetchRequest
        {
            Vendor = NormalizeVendor(gpu.Vendor, gpu.Name),
            GpuRaw = NormalizeWhitespace(gpu.Name),
            DeviceManufacturer = NormalizeWhitespace(safeRuntimeContext.Device?.Manufacturer ?? ""),
            DeviceModel = NormalizeWhitespace(safeRuntimeContext.Device?.Model ?? ""),
            RequestSource = "app",
            AppVersion = (appVersion ?? "").Trim()
        };
    }

    public static string NormalizeVendor(string? vendor, string? gpuName)
    {
        var candidate = $"{vendor} {gpuName}".Trim().ToLowerInvariant();
        if (candidate.Contains("nvidia", StringComparison.Ordinal))
        {
            return "nvidia";
        }

        if (candidate.Contains("intel", StringComparison.Ordinal))
        {
            return "intel";
        }

        if (candidate.Contains("amd", StringComparison.Ordinal)
            || candidate.Contains("radeon", StringComparison.Ordinal))
        {
            return "amd";
        }

        return "";
    }

    public static string NormalizeWhitespace(string? value)
    {
        return string.Join(" ", (value ?? "").Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)).Trim();
    }
}

public sealed record GpuBundleManifestSupportedGpuCandidateResult
{
    public IReadOnlyList<GpuInfo> SupportedCandidates { get; init; } = [];
    public IReadOnlyList<GpuBundleManifestUnsupportedGpuCandidate> UnsupportedCandidates { get; init; } = [];
}

public sealed record GpuBundleManifestUnsupportedGpuCandidate
{
    public GpuInfo Candidate { get; init; } = new();
    public string ErrorCode { get; init; } = "";
}

public static class GpuBundleManifestSupportedGpuCandidateResolver
{
    public static GpuBundleManifestSupportedGpuCandidateResult Resolve(
        RemoteGpuBundleManifest manifest,
        RuntimeContext runtimeContext,
        IReadOnlyList<GpuInfo> detectedCandidates,
        IGpuBundleManifestRuleResolver ruleResolver)
    {
        ArgumentNullException.ThrowIfNull(ruleResolver);

        if (detectedCandidates is null || detectedCandidates.Count == 0)
        {
            return new GpuBundleManifestSupportedGpuCandidateResult();
        }

        var supported = new List<GpuInfo>(detectedCandidates.Count);
        var unsupported = new List<GpuBundleManifestUnsupportedGpuCandidate>();
        foreach (var candidate in detectedCandidates)
        {
            var match = ruleResolver.Resolve(
                manifest,
                (runtimeContext ?? new RuntimeContext()) with { SelectedGpu = candidate });
            if (match.IsMatched && !match.IsUnsupported)
            {
                supported.Add(candidate);
                continue;
            }

            unsupported.Add(new GpuBundleManifestUnsupportedGpuCandidate
            {
                Candidate = candidate,
                ErrorCode = NormalizeErrorCode(match.ErrorCode)
            });
        }

        return new GpuBundleManifestSupportedGpuCandidateResult
        {
            SupportedCandidates = supported,
            UnsupportedCandidates = unsupported
        };
    }

    private static string NormalizeErrorCode(string? value)
    {
        var normalized = (value ?? "").Trim();
        return string.IsNullOrWhiteSpace(normalized) ? "bundle_rule_not_matched" : normalized;
    }
}
