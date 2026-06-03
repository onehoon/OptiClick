using OptiClick.Core.Runtime;
using OptiClick.Core.Abstractions;
using OptiClick.Wpf.Shell.Games.GpuBundle;
using OptiClick.Wpf.Shell.RuntimeData;

namespace OptiClick.Wpf.Diagnostics;

public sealed class RemoteDataContractSmokeOptions
{
    public bool EnableConsoleOutput { get; init; } = true;
}

public sealed class RemoteDataContractSmokeResult
{
    public bool IsSuccess { get; init; }
    public bool IsSkipped { get; init; }
    public string Stage { get; init; } = "";
    public string ErrorCode { get; init; } = "";
    public string Message { get; init; } = "";
    public int ExitCode { get; init; }
    public IReadOnlyList<string> OutputLines { get; init; } = [];
}

public interface IRemoteDataContractSmokeRunner
{
    Task<RemoteDataContractSmokeResult> RunAsync(
        RemoteDataContractSmokeOptions? options = null,
        CancellationToken cancellationToken = default);
}

public sealed class RemoteDataContractSmokeRunner : IRemoteDataContractSmokeRunner
{
    private readonly IRuntimeContextProvider _runtimeContextProvider;
    private readonly IRemoteRuntimeDataLoader _runtimeDataLoader;
    private readonly IRemoteGpuBundleManifestClient _manifestClient;
    private readonly IGpuBundleManifestRuleResolver _ruleResolver;
    private readonly IRemoteGpuBundleClient _bundleClient;
    private readonly IRemoteGpuBundleParser _bundleParser;
    private readonly IGpuBundleGameDatabaseMerger _gpuBundleMerger;

    public RemoteDataContractSmokeRunner(
        IRuntimeContextProvider runtimeContextProvider,
        IRemoteRuntimeDataLoader runtimeDataLoader,
        IRemoteGpuBundleManifestClient manifestClient,
        IGpuBundleManifestRuleResolver ruleResolver,
        IRemoteGpuBundleClient bundleClient,
        IRemoteGpuBundleParser bundleParser,
        IGpuBundleGameDatabaseMerger gpuBundleMerger)
    {
        _runtimeContextProvider = runtimeContextProvider ?? throw new ArgumentNullException(nameof(runtimeContextProvider));
        _runtimeDataLoader = runtimeDataLoader ?? throw new ArgumentNullException(nameof(runtimeDataLoader));
        _manifestClient = manifestClient ?? throw new ArgumentNullException(nameof(manifestClient));
        _ruleResolver = ruleResolver ?? throw new ArgumentNullException(nameof(ruleResolver));
        _bundleClient = bundleClient ?? throw new ArgumentNullException(nameof(bundleClient));
        _bundleParser = bundleParser ?? throw new ArgumentNullException(nameof(bundleParser));
        _gpuBundleMerger = gpuBundleMerger ?? throw new ArgumentNullException(nameof(gpuBundleMerger));
    }

    public async Task<RemoteDataContractSmokeResult> RunAsync(
        RemoteDataContractSmokeOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var lines = new List<string>
        {
            "REMOTE_CONTRACT_SMOKE_START"
        };

        try
        {
            var runtimeContext = _runtimeContextProvider.GetRuntimeContext() ?? new RuntimeContext();
            var remote = runtimeContext.RemoteData ?? new RemoteDataOptions();
            var runtimeDataConfigured = !string.IsNullOrWhiteSpace(remote.GetEffectiveRuntimeDataUrl());
            var manifestConfigured = !string.IsNullOrWhiteSpace(remote.GpuBundleManifestUrl);
            var bundleConfigured = !string.IsNullOrWhiteSpace(remote.GpuBundleUrl);

            lines.Add($"RuntimeDataUrl: {(runtimeDataConfigured ? "configured" : "missing")}");
            lines.Add($"GpuBundleManifestUrl: {(manifestConfigured ? "configured" : "missing")}");
            lines.Add($"GpuBundleUrl: {(bundleConfigured ? "configured" : "missing")}");

            var selectedGpu = SelectGpu(runtimeContext);
            var gpuVendor = NormalizeVendor(selectedGpu?.Vendor, selectedGpu?.Name);
            var gpuName = NormalizeDisplay(selectedGpu?.Name);
            lines.Add($"RuntimeContext: gpu_vendor={gpuVendor}, gpu_name={gpuName}");

            if (!runtimeDataConfigured || !manifestConfigured || !bundleConfigured)
            {
                lines.Add("REMOTE_CONTRACT_SMOKE_SKIPPED reason=endpoint_missing");
                return BuildResult(isSuccess: false, isSkipped: true, "endpoint_config", "endpoint_missing", 2, lines);
            }

            var runtimeDataLoadResult = await _runtimeDataLoader.LoadAsync(cancellationToken);
            if (runtimeDataLoadResult.IsSkipped)
            {
                lines.Add("REMOTE_CONTRACT_SMOKE_SKIPPED reason=runtime_data_skipped");
                return BuildResult(isSuccess: false, isSkipped: true, "runtime_data_fetch", "runtime_data_skipped", 2, lines);
            }

            if (!runtimeDataLoadResult.IsSuccess)
            {
                var runtimeDataFailureStage = ResolveRuntimeDataFailureStage(runtimeDataLoadResult.ErrorCode);
                lines.Add($"REMOTE_CONTRACT_SMOKE_FAIL stage={runtimeDataFailureStage} error={NormalizeCode(runtimeDataLoadResult.ErrorCode)}");
                return BuildResult(
                    isSuccess: false,
                    isSkipped: false,
                    runtimeDataFailureStage,
                    NormalizeCode(runtimeDataLoadResult.ErrorCode),
                    3,
                    lines);
            }

            var runtimeData = runtimeDataLoadResult.RuntimeData ?? RemoteRuntimeData.Empty;
            lines.Add($"RuntimeData: success, games={runtimeData.GameMaster.Count}");

            var manifestRequest = new GpuBundleManifestFetchRequest
            {
                Vendor = gpuVendor == "unknown" ? "" : gpuVendor,
                GpuRaw = selectedGpu?.Name ?? "",
                DeviceManufacturer = runtimeContext.Device?.Manufacturer ?? "",
                DeviceModel = runtimeContext.Device?.Model ?? "",
                RequestSource = "app"
            };

            var manifestResult = await _manifestClient.FetchAsync(
                (remote.GpuBundleManifestUrl ?? "").Trim(),
                manifestRequest,
                cancellationToken);
            if (manifestResult.IsSkipped)
            {
                lines.Add("REMOTE_CONTRACT_SMOKE_SKIPPED reason=gpu_manifest_skipped");
                return BuildResult(isSuccess: false, isSkipped: true, "gpu_manifest_fetch", "gpu_manifest_skipped", 2, lines);
            }

            if (!manifestResult.IsSuccess)
            {
                lines.Add($"REMOTE_CONTRACT_SMOKE_FAIL stage=gpu_manifest_fetch error={NormalizeCode(manifestResult.ErrorCode)}");
                return BuildResult(isSuccess: false, isSkipped: false, "gpu_manifest_fetch", NormalizeCode(manifestResult.ErrorCode), 3, lines);
            }

            lines.Add($"GpuBundleManifest: success, rules={manifestResult.Manifest.Rules.Count}");

            var ruleMatch = _ruleResolver.Resolve(manifestResult.Manifest, runtimeContext);
            if (ruleMatch.IsUnsupported)
            {
                lines.Add($"REMOTE_CONTRACT_SMOKE_FAIL stage=gpu_rule_resolve error={NormalizeCode(ruleMatch.ErrorCode)}");
                return BuildResult(isSuccess: false, isSkipped: false, "gpu_rule_resolve", NormalizeCode(ruleMatch.ErrorCode), 3, lines);
            }

            if (!ruleMatch.IsMatched)
            {
                lines.Add($"REMOTE_CONTRACT_SMOKE_FAIL stage=gpu_rule_resolve error={NormalizeCode(ruleMatch.ErrorCode)}");
                return BuildResult(isSuccess: false, isSkipped: false, "gpu_rule_resolve", NormalizeCode(ruleMatch.ErrorCode), 3, lines);
            }

            lines.Add(
                $"GpuBundleRule: success, vendor={NormalizeCode(ruleMatch.Vendor)}, bundle={NormalizeCode(ruleMatch.BundleKey)}, group={NormalizeCode(ruleMatch.GpuGroup)}");

            var bundleFetchRequest = new GpuBundleFetchRequest
            {
                Vendor = ruleMatch.Vendor,
                BundleKey = ruleMatch.BundleKey,
                GpuRaw = ruleMatch.GpuRaw,
                ManifestVersion = manifestResult.Manifest.ManifestVersion,
                RequestSource = "app",
                DeviceManufacturer = runtimeContext.Device?.Manufacturer ?? "",
                DeviceModel = runtimeContext.Device?.Model ?? ""
            };

            var bundleFetchResult = await _bundleClient.FetchAsync((remote.GpuBundleUrl ?? "").Trim(), bundleFetchRequest, cancellationToken);
            if (bundleFetchResult.IsSkipped)
            {
                lines.Add("REMOTE_CONTRACT_SMOKE_SKIPPED reason=gpu_bundle_skipped");
                return BuildResult(isSuccess: false, isSkipped: true, "gpu_bundle_fetch", "gpu_bundle_skipped", 2, lines);
            }

            if (!bundleFetchResult.IsSuccess)
            {
                lines.Add($"REMOTE_CONTRACT_SMOKE_FAIL stage=gpu_bundle_fetch error={NormalizeCode(bundleFetchResult.ErrorCode)}");
                return BuildResult(isSuccess: false, isSkipped: false, "gpu_bundle_fetch", NormalizeCode(bundleFetchResult.ErrorCode), 3, lines);
            }

            var bundleParseResult = _bundleParser.Parse(
                bundleFetchResult.Content,
                selectedGpuGroup: ruleMatch.GpuGroup,
                requestVendor: ruleMatch.Vendor,
                bundleKey: ruleMatch.BundleKey,
                manifestVersion: manifestResult.Manifest.ManifestVersion);

            if (!bundleParseResult.IsSuccess)
            {
                lines.Add($"REMOTE_CONTRACT_SMOKE_FAIL stage=gpu_bundle_parse error={NormalizeCode(bundleParseResult.ErrorCode)}");
                return BuildResult(isSuccess: false, isSkipped: false, "gpu_bundle_parse", NormalizeCode(bundleParseResult.ErrorCode), 3, lines);
            }

            lines.Add($"GpuBundlePayload: success, games={bundleParseResult.Bundle.GamesByGameId.Count}");

            var mergeResult = _gpuBundleMerger.Merge(runtimeData, bundleParseResult.Bundle);
            lines.Add($"Merge: success, metadata={mergeResult.MetadataByGameId.Count}");

            lines.Add("REMOTE_CONTRACT_SMOKE_OK");
            return BuildResult(isSuccess: true, isSkipped: false, "done", "", 0, lines);
        }
        catch (OperationCanceledException)
        {
            lines.Add("REMOTE_CONTRACT_SMOKE_FAIL stage=canceled error=canceled");
            return BuildResult(isSuccess: false, isSkipped: false, "canceled", "canceled", 4, lines);
        }
        catch
        {
            lines.Add("REMOTE_CONTRACT_SMOKE_FAIL stage=unexpected error=unexpected_error");
            return BuildResult(isSuccess: false, isSkipped: false, "unexpected", "unexpected_error", 4, lines);
        }
    }

    private static RemoteDataContractSmokeResult BuildResult(
        bool isSuccess,
        bool isSkipped,
        string stage,
        string errorCode,
        int exitCode,
        IReadOnlyList<string> lines)
    {
        return new RemoteDataContractSmokeResult
        {
            IsSuccess = isSuccess,
            IsSkipped = isSkipped,
            Stage = NormalizeCode(stage),
            ErrorCode = NormalizeCode(errorCode),
            Message = isSuccess ? "success" : NormalizeCode(errorCode),
            ExitCode = exitCode,
            OutputLines = lines
        };
    }

    private static GpuInfo? SelectGpu(RuntimeContext runtimeContext)
    {
        var gpus = runtimeContext?.Gpus ?? [];
        if (gpus.Count == 0)
        {
            return null;
        }

        return gpus.FirstOrDefault(static gpu => gpu.IsPrimary) ?? gpus[0];
    }

    private static string NormalizeVendor(string? vendor, string? gpuName)
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

        return "unknown";
    }

    private static string NormalizeDisplay(string? value)
    {
        var text = (value ?? "").Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            return "-";
        }

        return text;
    }

    private static string NormalizeCode(string? value)
    {
        var code = (value ?? "").Trim();
        return string.IsNullOrWhiteSpace(code) ? "none" : code;
    }

    private static string ResolveRuntimeDataFailureStage(string? errorCode)
    {
        var code = NormalizeCode(errorCode);
        return code switch
        {
            "invalid_json" => "runtime_data_parse",
            "payload_not_object" => "runtime_data_parse",
            "schema_version_missing" => "runtime_data_parse",
            "unsupported_schema_version" => "runtime_data_parse",
            "data_missing_or_not_object" => "runtime_data_parse",
            "missing_required_section" => "runtime_data_parse",
            "section_not_array" => "runtime_data_parse",
            _ => "runtime_data_fetch"
        };
    }
}
