using OptiClick.Core.Runtime;

namespace OptiClick.Core.Games.GpuBundle;

public sealed class GpuBundleRuleMatchResult
{
    public bool IsMatched { get; init; }
    public bool IsUnsupported { get; init; }
    public string ErrorCode { get; init; } = "";
    public string Vendor { get; init; } = "";
    public string BundleKey { get; init; } = "";
    public string GpuGroup { get; init; } = "";
    public string GpuRaw { get; init; } = "";
    public Fsr4ManifestPolicy Fsr4 { get; init; } = Fsr4ManifestPolicy.Disabled;
}

public interface IGpuBundleManifestRuleResolver
{
    GpuBundleRuleMatchResult Resolve(RemoteGpuBundleManifest manifest, RuntimeContext runtimeContext);
}

public sealed class GpuBundleManifestRuleResolver : IGpuBundleManifestRuleResolver
{
    public GpuBundleRuleMatchResult Resolve(RemoteGpuBundleManifest manifest, RuntimeContext runtimeContext)
    {
        if (manifest is null)
        {
            return new GpuBundleRuleMatchResult { ErrorCode = "manifest_missing" };
        }

        var selectedGpu = ResolveSelectedGpu(runtimeContext);
        if (selectedGpu is null)
        {
            return new GpuBundleRuleMatchResult
            {
                ErrorCode = HasMultipleGpuCandidates(runtimeContext?.Gpus)
                    ? "gpu_selection_pending"
                    : "gpu_not_found"
            };
        }

        var normalizedVendor = NormalizeVendor(selectedGpu.Vendor, selectedGpu.Name);
        var normalizedGpuRaw = NormalizeGpuMatchText(selectedGpu.Name);
        if (string.IsNullOrWhiteSpace(normalizedVendor) || string.IsNullOrWhiteSpace(normalizedGpuRaw))
        {
            return new GpuBundleRuleMatchResult { ErrorCode = "gpu_unsupported" };
        }

        var matches = new List<(int Priority, int MatchLengthNegative, int Index, RemoteGpuBundleManifestRule Rule)>();
        foreach (var rule in manifest.Rules)
        {
            if (rule is null || !rule.Enabled)
            {
                continue;
            }

            if (!string.Equals(rule.Vendor, normalizedVendor, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var normalizedMatchValue = NormalizeGpuMatchText(rule.MatchValue);
            if (string.IsNullOrWhiteSpace(normalizedMatchValue))
            {
                continue;
            }

            var matched = false;
            if (string.Equals(rule.MatchMode, "exact", StringComparison.OrdinalIgnoreCase))
            {
                matched = string.Equals(normalizedGpuRaw, normalizedMatchValue, StringComparison.Ordinal);
            }
            else if (string.Equals(rule.MatchMode, "contains", StringComparison.OrdinalIgnoreCase))
            {
                matched = normalizedGpuRaw.Contains(normalizedMatchValue, StringComparison.Ordinal);
            }

            if (!matched)
            {
                continue;
            }

            matches.Add((rule.Priority, -normalizedMatchValue.Length, rule.SourceIndex, rule));
        }

        if (matches.Count > 0)
        {
            var selected = matches
                .OrderBy(static item => item.Priority)
                .ThenBy(static item => item.MatchLengthNegative)
                .ThenBy(static item => item.Index)
                .First().Rule;

            return new GpuBundleRuleMatchResult
            {
                IsMatched = true,
                Vendor = normalizedVendor,
                BundleKey = (selected.BundleKey ?? "").Trim(),
                GpuGroup = (selected.GpuGroup ?? "").Trim().ToLowerInvariant(),
                GpuRaw = NormalizeSpace(selectedGpu.Name),
                Fsr4 = selected.Fsr4 ?? Fsr4ManifestPolicy.Disabled
            };
        }

        var fallback = manifest.Fallback;
        if (fallback is null || !fallback.Enabled)
        {
            return new GpuBundleRuleMatchResult
            {
                IsUnsupported = true,
                ErrorCode = "bundle_rule_not_matched",
                Vendor = normalizedVendor,
                GpuRaw = NormalizeSpace(selectedGpu.Name)
            };
        }

        var fallbackBundleKey = (fallback.BundleKey ?? "").Trim();
        var fallbackGroup = (fallback.GpuGroup ?? "").Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(fallbackBundleKey) || string.IsNullOrWhiteSpace(fallbackGroup))
        {
            return new GpuBundleRuleMatchResult
            {
                IsUnsupported = true,
                ErrorCode = "fallback_invalid",
                Vendor = normalizedVendor,
                GpuRaw = NormalizeSpace(selectedGpu.Name)
            };
        }

        if (string.Equals(fallbackBundleKey, "unknown", StringComparison.OrdinalIgnoreCase)
            || string.Equals(fallbackGroup, "unknown", StringComparison.OrdinalIgnoreCase))
        {
            return new GpuBundleRuleMatchResult
            {
                IsUnsupported = true,
                ErrorCode = "bundle_rule_not_matched",
                Vendor = normalizedVendor,
                GpuRaw = NormalizeSpace(selectedGpu.Name)
            };
        }

        return new GpuBundleRuleMatchResult
        {
            IsMatched = true,
            Vendor = normalizedVendor,
            BundleKey = fallbackBundleKey,
            GpuGroup = fallbackGroup,
            GpuRaw = NormalizeSpace(selectedGpu.Name),
            Fsr4 = Fsr4ManifestPolicy.Disabled
        };
    }

    private static GpuInfo? ResolveSelectedGpu(RuntimeContext runtimeContext)
    {
        if (runtimeContext?.SelectedGpu is not null)
        {
            return runtimeContext.SelectedGpu;
        }

        var gpus = BuildDistinctGpuCandidates(runtimeContext?.Gpus);
        return gpus.Count == 1 ? gpus[0] : null;
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
            var name = NormalizeSpace(gpu.Name);
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            var vendor = NormalizeSpace(gpu.Vendor);
            var key = $"{vendor}|{name}";
            if (seen.Add(key))
            {
                list.Add(gpu);
            }
        }

        return list;
    }

    private static bool HasMultipleGpuCandidates(IReadOnlyList<GpuInfo>? gpus)
    {
        return BuildDistinctGpuCandidates(gpus).Count > 1;
    }

    private static string NormalizeVendor(string vendor, string gpuName)
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

        if (candidate.Contains("amd", StringComparison.Ordinal) || candidate.Contains("radeon", StringComparison.Ordinal))
        {
            return "amd";
        }

        return "";
    }

    private static string NormalizeSpace(string value)
    {
        return string.Join(" ", (value ?? "").Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)).Trim();
    }

    private static string NormalizeGpuMatchText(string value)
    {
        var text = NormalizeSpace(value);
        if (string.IsNullOrWhiteSpace(text))
        {
            return "";
        }

        text = System.Text.RegularExpressions.Regex.Replace(
            text,
            @"\((?:tm|r)\)",
            "",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        text = text.Replace("\u2122", "").Replace("\u00AE", "");
        return NormalizeSpace(text).ToLowerInvariant();
    }
}
