using System.Text.RegularExpressions;
using OptiClick.Core.Runtime;

namespace OptiClick.Wpf.Shell.Gpu;

public sealed class GpuSelectionCoordinator
{
    public const int DefaultMaxSupportedGpuCount = 2;

    private readonly int _maxSupportedGpuCount;
    private bool _multiGpuBlockPopupShown;
    private string _multiGpuBlockPopupSignature = "";
    private string _selectedGpuIdentity = "";
    private string _gpuSelectionSignature = "";

    public GpuSelectionCoordinator(int maxSupportedGpuCount)
    {
        _maxSupportedGpuCount = maxSupportedGpuCount;
    }

    public bool MultiGpuBlocked { get; private set; }

    public bool GpuSelectionPending { get; private set; }

    public string SelectedGpuLogSource { get; private set; } = "runtime_context_selected";

    public async Task<RuntimeContext> ResolveAsync(
        GpuSelectionCoordinatorRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var safeContext = request.Context ?? new RuntimeContext();
        var detectedGpuCandidates = BuildDistinctGpuCandidates(safeContext.Gpus);
        var gpuCandidates = await request.ResolveSupportedGpuCandidatesAsync(
            safeContext,
            detectedGpuCandidates,
            cancellationToken);

        request.LogInfo($"gpu_candidate_summary detected={detectedGpuCandidates.Count} supported={gpuCandidates.Count}");

        if (gpuCandidates.Count == 0)
        {
            MultiGpuBlocked = false;
            ResetMultiGpuBlockPopupState();
            GpuSelectionPending = false;
            _selectedGpuIdentity = "";
            _gpuSelectionSignature = "";
            SelectedGpuLogSource = "runtime_context_selected";
            if (detectedGpuCandidates.Count > 0)
            {
                var reason = request.ReadManifestRestartRequired()
                    ? " reason=manifest_restart_required"
                    : "";
                request.LogWarning($"gpu_candidate_summary supported_none detected={detectedGpuCandidates.Count}{reason}");
            }

            return safeContext with { SelectedGpu = null };
        }

        if (gpuCandidates.Count > _maxSupportedGpuCount)
        {
            var blockedSignature = BuildGpuSelectionSignature(gpuCandidates);
            MultiGpuBlocked = true;
            GpuSelectionPending = false;
            _selectedGpuIdentity = "";
            _gpuSelectionSignature = blockedSignature;
            SelectedGpuLogSource = "runtime_context_selected";
            request.LogWarning($"multi_gpu_blocked gpu_count={gpuCandidates.Count} max_supported={_maxSupportedGpuCount}");
            request.ApplyMultiGpuBlockedUiState();
            await ShowMultiGpuBlockedPopupIfNeededAsync(request, blockedSignature, cancellationToken);
            return safeContext with
            {
                SelectedGpu = null
            };
        }

        MultiGpuBlocked = false;
        ResetMultiGpuBlockPopupState();
        if (gpuCandidates.Count == 1)
        {
            var selected = gpuCandidates[0];
            GpuSelectionPending = false;
            _selectedGpuIdentity = BuildGpuIdentity(selected);
            _gpuSelectionSignature = BuildGpuSelectionSignature(gpuCandidates);
            SelectedGpuLogSource = "runtime_context_selected";
            return safeContext with { SelectedGpu = selected };
        }

        var signature = BuildGpuSelectionSignature(gpuCandidates);
        if (string.Equals(signature, _gpuSelectionSignature, StringComparison.OrdinalIgnoreCase)
            && TryResolveGpuByIdentity(gpuCandidates, _selectedGpuIdentity, out var cachedSelection))
        {
            GpuSelectionPending = false;
            SelectedGpuLogSource = "user_selected_cached";
            return safeContext with { SelectedGpu = cachedSelection };
        }

        SelectedGpuLogSource = "gpu_selection_pending";
        GpuSelectionPending = true;
        request.LogInfo($"dual_gpu_selection requested gpu_count={gpuCandidates.Count}");

        GpuInfo? selectedGpu = null;
        selectedGpu = await request.PromptDualGpuSelectionAsync(
            gpuCandidates[0],
            gpuCandidates[1],
            cancellationToken);

        if (selectedGpu is null)
        {
            request.LogWarning("dual_gpu_selection aborted reason=no_selection");
            GpuSelectionPending = true;
            SelectedGpuLogSource = "gpu_selection_pending";
            return safeContext with { SelectedGpu = null };
        }

        GpuSelectionPending = false;
        _gpuSelectionSignature = signature;
        _selectedGpuIdentity = BuildGpuIdentity(selectedGpu);
        SelectedGpuLogSource = "user_selected_popup";
        request.LogInfo(
            $"dual_gpu_selection applied vendor={NormalizeStatusCode(selectedGpu.Vendor, "unknown")} name=\"{NormalizeStatusCode(selectedGpu.Name, "unknown")}\"");
        return safeContext with { SelectedGpu = selectedGpu };
    }

    private async Task ShowMultiGpuBlockedPopupIfNeededAsync(
        GpuSelectionCoordinatorRequest request,
        string signature,
        CancellationToken cancellationToken)
    {
        var normalizedSignature = (signature ?? "").Trim();
        if (!string.Equals(_multiGpuBlockPopupSignature, normalizedSignature, StringComparison.OrdinalIgnoreCase))
        {
            _multiGpuBlockPopupSignature = normalizedSignature;
            _multiGpuBlockPopupShown = false;
        }

        if (_multiGpuBlockPopupShown)
        {
            return;
        }

        _multiGpuBlockPopupShown = true;
        await request.ShowMultiGpuBlockedPopupAsync(cancellationToken);
    }

    private void ResetMultiGpuBlockPopupState()
    {
        _multiGpuBlockPopupShown = false;
        _multiGpuBlockPopupSignature = "";
    }

    public static IReadOnlyList<GpuInfo> BuildDistinctGpuCandidates(IReadOnlyList<GpuInfo>? gpus)
    {
        if (gpus is null || gpus.Count == 0)
        {
            return Array.Empty<GpuInfo>();
        }

        var list = new List<GpuInfo>(gpus.Count);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var gpu in gpus)
        {
            var name = NormalizeWhitespace(gpu.Name);
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            var vendor = NormalizeWhitespace(gpu.Vendor);
            var adapterId = NormalizeWhitespace(gpu.AdapterId);
            var key = $"{vendor}|{name}";
            if (!seen.Add(key))
            {
                continue;
            }

            list.Add(gpu with
            {
                Name = name,
                Vendor = vendor,
                AdapterId = adapterId
            });
        }

        return list;
    }

    public static string NormalizeVendorForManifestRequest(string? vendor, string? gpuName)
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

    public static string BuildGpuSelectionButtonText(GpuInfo gpu, int index)
    {
        var vendorLabel = NormalizeVendorLabel(gpu.Vendor);
        var shortModel = ShortenGpuModelName(gpu.Name, vendorLabel);
        if (string.IsNullOrWhiteSpace(shortModel))
        {
            return $"GPU {index}";
        }

        if (string.IsNullOrWhiteSpace(vendorLabel))
        {
            return shortModel;
        }

        return shortModel.StartsWith(vendorLabel, StringComparison.OrdinalIgnoreCase)
            ? shortModel
            : $"{vendorLabel} {shortModel}";
    }

    public static string NormalizeWhitespace(string value)
    {
        return string.Join(" ", (value ?? "").Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)).Trim();
    }

    private static string BuildGpuSelectionSignature(IReadOnlyList<GpuInfo> gpus)
    {
        if (gpus.Count == 0)
        {
            return "";
        }

        return string.Join("||", gpus.Select(BuildGpuIdentity));
    }

    private static bool TryResolveGpuByIdentity(
        IReadOnlyList<GpuInfo> gpus,
        string identity,
        out GpuInfo selected)
    {
        var normalizedIdentity = NormalizeWhitespace(identity);
        if (string.IsNullOrWhiteSpace(normalizedIdentity))
        {
            selected = new GpuInfo();
            return false;
        }

        foreach (var gpu in gpus)
        {
            if (!string.Equals(BuildGpuIdentity(gpu), normalizedIdentity, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            selected = gpu;
            return true;
        }

        selected = new GpuInfo();
        return false;
    }

    private static string BuildGpuIdentity(GpuInfo gpu)
    {
        var adapterId = NormalizeWhitespace(gpu.AdapterId);
        if (!string.IsNullOrWhiteSpace(adapterId))
        {
            return adapterId;
        }

        return $"{NormalizeWhitespace(gpu.Vendor)}|{NormalizeWhitespace(gpu.Name)}";
    }

    private static string ShortenGpuModelName(string modelName, string vendorLabel)
    {
        var text = NormalizeWhitespace(modelName);
        if (string.IsNullOrWhiteSpace(text))
        {
            return "";
        }

        text = text.Replace("\u2122", "", StringComparison.Ordinal).Replace("\u00AE", "", StringComparison.Ordinal);
        text = Regex.Replace(text, @"\((?:tm|r)\)", "", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"\bcorporation\b", "", RegexOptions.IgnoreCase);

        if (string.Equals(vendorLabel, "NVIDIA", StringComparison.OrdinalIgnoreCase))
        {
            text = Regex.Replace(text, @"^nvidia\s+", "", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"^geforce\s+", "", RegexOptions.IgnoreCase);
        }
        else if (string.Equals(vendorLabel, "AMD", StringComparison.OrdinalIgnoreCase))
        {
            text = Regex.Replace(text, @"^amd\s+", "", RegexOptions.IgnoreCase);
        }
        else if (string.Equals(vendorLabel, "Intel", StringComparison.OrdinalIgnoreCase))
        {
            text = Regex.Replace(text, @"^intel\s+", "", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"\barc\s+graphics\b", "Arc", RegexOptions.IgnoreCase);
        }

        text = Regex.Replace(text, @"\bgraphics\b$", "", RegexOptions.IgnoreCase);
        return NormalizeWhitespace(text);
    }

    private static string NormalizeVendorLabel(string vendor)
    {
        var normalized = NormalizeWhitespace(vendor);
        if (string.Equals(normalized, "NVIDIA", StringComparison.OrdinalIgnoreCase))
        {
            return "NVIDIA";
        }

        if (string.Equals(normalized, "AMD", StringComparison.OrdinalIgnoreCase))
        {
            return "AMD";
        }

        if (string.Equals(normalized, "Intel", StringComparison.OrdinalIgnoreCase))
        {
            return "Intel";
        }

        return normalized;
    }

    private static string NormalizeStatusCode(string? value, string fallback)
    {
        var normalized = (value ?? "").Trim();
        return string.IsNullOrWhiteSpace(normalized) ? fallback : normalized;
    }
}

public sealed record GpuSelectionCoordinatorRequest
{
    public required RuntimeContext Context { get; init; }
    public required Func<RuntimeContext, IReadOnlyList<GpuInfo>, CancellationToken, Task<IReadOnlyList<GpuInfo>>> ResolveSupportedGpuCandidatesAsync { get; init; }
    public required Func<GpuInfo, GpuInfo, CancellationToken, Task<GpuInfo?>> PromptDualGpuSelectionAsync { get; init; }
    public required Func<CancellationToken, Task> ShowMultiGpuBlockedPopupAsync { get; init; }
    public required Action ApplyMultiGpuBlockedUiState { get; init; }
    public required Action<string> LogInfo { get; init; }
    public required Action<string> LogWarning { get; init; }
    public required Func<bool> ReadManifestRestartRequired { get; init; }
}
