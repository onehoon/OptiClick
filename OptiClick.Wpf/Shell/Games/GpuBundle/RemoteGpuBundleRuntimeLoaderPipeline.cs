using System.Net.Http;
using System.Text;
using System.Text.Json;
using OptiClick.Core.Runtime;
using OptiClick.Wpf.Logging;
using OptiClick.Wpf.Shell.Games.Support;

namespace OptiClick.Wpf.Shell.Games.GpuBundle;

public sealed class RemoteGpuBundleRuntimeLoadResult
{
    public bool IsSuccess { get; init; }
    public bool IsSkipped { get; init; }
    public bool IsUnsupported { get; init; }
    public string ErrorCode { get; init; } = "";
    public RemoteGpuBundle Bundle { get; init; } = new();
    public string BundleKey { get; init; } = "";
    public string GpuGroup { get; init; } = "";
    public string Vendor { get; init; } = "";

    public static RemoteGpuBundleRuntimeLoadResult Success(
        RemoteGpuBundle bundle,
        string bundleKey,
        string gpuGroup,
        string vendor)
    {
        return new RemoteGpuBundleRuntimeLoadResult
        {
            IsSuccess = true,
            Bundle = bundle ?? new RemoteGpuBundle(),
            BundleKey = (bundleKey ?? "").Trim(),
            GpuGroup = (gpuGroup ?? "").Trim().ToLowerInvariant(),
            Vendor = (vendor ?? "").Trim().ToLowerInvariant()
        };
    }

    public static RemoteGpuBundleRuntimeLoadResult Skipped(string errorCode = "")
    {
        return new RemoteGpuBundleRuntimeLoadResult
        {
            IsSkipped = true,
            ErrorCode = (errorCode ?? "").Trim()
        };
    }

    public static RemoteGpuBundleRuntimeLoadResult Unsupported(string errorCode)
    {
        return new RemoteGpuBundleRuntimeLoadResult
        {
            IsUnsupported = true,
            ErrorCode = (errorCode ?? "").Trim()
        };
    }

    public static RemoteGpuBundleRuntimeLoadResult Failure(string errorCode)
    {
        return new RemoteGpuBundleRuntimeLoadResult
        {
            ErrorCode = (errorCode ?? "").Trim()
        };
    }
}

public interface IRemoteGpuBundleRuntimeLoader
{
    Task<RemoteGpuBundleRuntimeLoadResult> LoadAsync(
        RuntimeContext runtimeContext,
        CancellationToken cancellationToken = default);
}

public sealed class RemoteGpuBundleRuntimeLoader : IRemoteGpuBundleRuntimeLoader
{
    private readonly IRemoteGpuBundleManifestClient _manifestClient;
    private readonly IGpuBundleManifestRuleResolver _ruleResolver;
    private readonly IRemoteGpuBundleClient _bundleClient;
    private readonly IRemoteGpuBundleParser _bundleParser;
    private readonly Services.IAppVersionProvider _appVersionProvider;
    private readonly IAppLogger _logger;

    public RemoteGpuBundleRuntimeLoader(
        IRemoteGpuBundleManifestClient manifestClient,
        IGpuBundleManifestRuleResolver ruleResolver,
        IRemoteGpuBundleClient bundleClient,
        IRemoteGpuBundleParser bundleParser,
        Services.IAppVersionProvider appVersionProvider,
        IAppLogger? logger = null)
    {
        _manifestClient = manifestClient ?? throw new ArgumentNullException(nameof(manifestClient));
        _ruleResolver = ruleResolver ?? throw new ArgumentNullException(nameof(ruleResolver));
        _bundleClient = bundleClient ?? throw new ArgumentNullException(nameof(bundleClient));
        _bundleParser = bundleParser ?? throw new ArgumentNullException(nameof(bundleParser));
        _appVersionProvider = appVersionProvider ?? throw new ArgumentNullException(nameof(appVersionProvider));
        _logger = logger ?? NullAppLogger.Instance;
    }

    public async Task<RemoteGpuBundleRuntimeLoadResult> LoadAsync(
        RuntimeContext runtimeContext,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var safeRuntimeContext = runtimeContext ?? new RuntimeContext();
            var remoteOptions = safeRuntimeContext.RemoteData ?? new RemoteDataOptions();
            var manifestEndpoint = (remoteOptions.GpuBundleManifestUrl ?? "").Trim();
            var bundleEndpoint = (remoteOptions.GpuBundleUrl ?? "").Trim();
            if (string.IsNullOrWhiteSpace(manifestEndpoint) || string.IsNullOrWhiteSpace(bundleEndpoint))
            {
                _logger.Warning("remote", "gpu-bundle-runtime skipped code=gpu_bundle_endpoint_missing");
                return RemoteGpuBundleRuntimeLoadResult.Skipped("gpu_bundle_endpoint_missing");
            }

            var selectedGpu = ResolveSelectedGpuForManifest(safeRuntimeContext);
            if (selectedGpu is null)
            {
                var errorCode = HasMultipleGpuCandidates(safeRuntimeContext.Gpus)
                    ? "gpu_selection_pending"
                    : "gpu_not_found";
                _logger.Warning("remote", $"gpu-bundle-runtime skipped code={errorCode}");
                return RemoteGpuBundleRuntimeLoadResult.Failure(errorCode);
            }

            var appVersion = (_appVersionProvider.GetCurrentVersion() ?? "").Trim();
            var manifestRequest = new GpuBundleManifestFetchRequest
            {
                Vendor = NormalizeVendorForManifest(selectedGpu.Vendor, selectedGpu.Name),
                GpuRaw = NormalizeSpaceForManifest(selectedGpu.Name),
                DeviceManufacturer = (safeRuntimeContext.Device?.Manufacturer ?? "").Trim(),
                DeviceModel = (safeRuntimeContext.Device?.Model ?? "").Trim(),
                RequestSource = "app",
                AppVersion = appVersion
            };

            var manifestResult = await _manifestClient.FetchAsync(manifestEndpoint, manifestRequest, cancellationToken);
            if (manifestResult.IsSkipped)
            {
                _logger.Warning("remote", "gpu-bundle-runtime skipped code=gpu_bundle_manifest_skipped");
                return RemoteGpuBundleRuntimeLoadResult.Skipped("gpu_bundle_manifest_skipped");
            }

            if (!manifestResult.IsSuccess)
            {
                _logger.Error("remote", $"gpu-bundle-runtime failed stage=manifest code={NormalizeLogValue(manifestResult.ErrorCode, "manifest_failed")}");
                return RemoteGpuBundleRuntimeLoadResult.Failure(manifestResult.ErrorCode);
            }

            var match = _ruleResolver.Resolve(manifestResult.Manifest, safeRuntimeContext);
            if (match.IsUnsupported)
            {
                _logger.Warning(
                    "remote",
                    $"gpu-bundle-runtime unsupported stage=rule_match code={NormalizeLogValue(match.ErrorCode, "bundle_rule_not_matched")} vendor={NormalizeLogValue(match.Vendor, "none")} gpu_raw={NormalizeLogValue(match.GpuRaw, "none")}");
                await ReportUnsupportedGpuAsync(
                    bundleEndpoint,
                    safeRuntimeContext,
                    manifestResult.Manifest.ManifestVersion,
                    NormalizeLogValue(match.Vendor, ""),
                    NormalizeLogValue(match.GpuRaw, NormalizeSpaceForManifest(selectedGpu.Name)),
                    appVersion,
                    cancellationToken);
                return RemoteGpuBundleRuntimeLoadResult.Unsupported(match.ErrorCode);
            }

            if (!match.IsMatched)
            {
                var matchError = string.IsNullOrWhiteSpace(match.ErrorCode) ? "gpu_bundle_rule_not_matched" : match.ErrorCode;
                _logger.Error(
                    "remote",
                    $"gpu-bundle-runtime failed stage=rule_match code={NormalizeLogValue(matchError, "gpu_bundle_rule_not_matched")} vendor={NormalizeLogValue(match.Vendor, "none")} gpu_raw={NormalizeLogValue(match.GpuRaw, "none")}");
                await ReportUnsupportedGpuAsync(
                    bundleEndpoint,
                    safeRuntimeContext,
                    manifestResult.Manifest.ManifestVersion,
                    NormalizeLogValue(match.Vendor, ""),
                    NormalizeLogValue(match.GpuRaw, NormalizeSpaceForManifest(selectedGpu.Name)),
                    appVersion,
                    cancellationToken);
                return RemoteGpuBundleRuntimeLoadResult.Failure(
                    string.IsNullOrWhiteSpace(match.ErrorCode) ? "gpu_bundle_rule_not_matched" : match.ErrorCode);
            }

            _logger.Info(
                "remote",
                $"gpu-bundle-runtime rule-matched vendor={NormalizeLogValue(match.Vendor, "none")} bundle={NormalizeLogValue(match.BundleKey, "none")} gpu_group={NormalizeLogValue(match.GpuGroup, "none")} manifest_version={NormalizeLogValue(manifestResult.Manifest.ManifestVersion, "none")}");

            var request = new GpuBundleFetchRequest
            {
                Vendor = match.Vendor,
                BundleKey = match.BundleKey,
                GpuRaw = match.GpuRaw,
                ManifestVersion = manifestResult.Manifest.ManifestVersion,
                RequestSource = "app",
                DeviceManufacturer = safeRuntimeContext.Device?.Manufacturer ?? "",
                DeviceModel = safeRuntimeContext.Device?.Model ?? "",
                AppVersion = appVersion
            };

            var fetchResult = await _bundleClient.FetchAsync(bundleEndpoint, request, cancellationToken);
            if (fetchResult.IsSkipped)
            {
                _logger.Warning("remote", "gpu-bundle-runtime skipped code=gpu_bundle_fetch_skipped");
                return RemoteGpuBundleRuntimeLoadResult.Skipped("gpu_bundle_fetch_skipped");
            }

            if (!fetchResult.IsSuccess)
            {
                _logger.Error("remote", $"gpu-bundle-runtime failed stage=bundle_fetch code={NormalizeLogValue(fetchResult.ErrorCode, "bundle_fetch_failed")}");
                return RemoteGpuBundleRuntimeLoadResult.Failure(fetchResult.ErrorCode);
            }

            var parseResult = _bundleParser.Parse(
                fetchResult.Content,
                selectedGpuGroup: match.GpuGroup,
                requestVendor: match.Vendor,
                bundleKey: match.BundleKey,
                manifestVersion: manifestResult.Manifest.ManifestVersion);
            if (!parseResult.IsSuccess)
            {
                _logger.Error("remote", $"gpu-bundle-runtime failed stage=bundle_parse code={NormalizeLogValue(parseResult.ErrorCode, "bundle_parse_failed")}");
                return RemoteGpuBundleRuntimeLoadResult.Failure(parseResult.ErrorCode);
            }

            _logger.Info(
                "remote",
                $"gpu-bundle-runtime success vendor={NormalizeLogValue(match.Vendor, "none")} bundle={NormalizeLogValue(match.BundleKey, "none")} gpu_group={NormalizeLogValue(match.GpuGroup, "none")} game_count={parseResult.Bundle.GamesByGameId.Count}");

            return RemoteGpuBundleRuntimeLoadResult.Success(
                parseResult.Bundle,
                match.BundleKey,
                match.GpuGroup,
                match.Vendor);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.Warning("remote", "gpu-bundle-runtime canceled code=gpu_bundle_canceled");
            return RemoteGpuBundleRuntimeLoadResult.Failure("gpu_bundle_canceled");
        }
        catch (Exception ex)
        {
            _logger.Error("remote", "gpu-bundle-runtime failed code=gpu_bundle_unexpected_error", ex);
            return RemoteGpuBundleRuntimeLoadResult.Failure("gpu_bundle_unexpected_error");
        }
    }

    private static GpuInfo? ResolveSelectedGpuForManifest(RuntimeContext runtimeContext)
    {
        if (runtimeContext?.SelectedGpu is not null)
        {
            return runtimeContext.SelectedGpu;
        }

        var gpus = BuildDistinctGpuCandidates(runtimeContext?.Gpus);
        return gpus.Count == 1 ? gpus[0] : null;
    }

    private static bool HasMultipleGpuCandidates(IReadOnlyList<GpuInfo>? gpus)
    {
        return BuildDistinctGpuCandidates(gpus).Count > 1;
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
            var name = NormalizeSpaceForManifest(gpu.Name);
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            var vendor = NormalizeSpaceForManifest(gpu.Vendor);
            var key = $"{vendor}|{name}";
            if (seen.Add(key))
            {
                list.Add(gpu);
            }
        }

        return list;
    }

    private static string NormalizeVendorForManifest(string? vendor, string? gpuName)
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

    private static string NormalizeSpaceForManifest(string? value)
    {
        return string.Join(" ", (value ?? "").Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)).Trim();
    }

    private async Task ReportUnsupportedGpuAsync(
        string bundleEndpoint,
        RuntimeContext runtimeContext,
        string manifestVersion,
        string vendor,
        string gpuRaw,
        string appVersion,
        CancellationToken cancellationToken)
    {
        _logger.Info(
            "remote",
            $"gpu-bundle-runtime report-only vendor={NormalizeLogValue(vendor, "none")} gpu_raw={NormalizeLogValue(gpuRaw, "none")} manifest_version={NormalizeLogValue(manifestVersion, "none")}");

        var reportResult = await _bundleClient.ReportUnsupportedAsync(
            bundleEndpoint,
            new GpuBundleUnsupportedReportRequest
            {
                Vendor = vendor,
                GpuRaw = gpuRaw,
                RequestSource = "app",
                DeviceManufacturer = (runtimeContext.Device?.Manufacturer ?? "").Trim(),
                DeviceModel = (runtimeContext.Device?.Model ?? "").Trim(),
                AppVersion = (appVersion ?? "").Trim(),
                ManifestVersion = (manifestVersion ?? "").Trim(),
                Reason = "manifest_no_match"
            },
            cancellationToken);

        if (reportResult.IsSuccess)
        {
            _logger.Info("remote", "gpu-bundle-runtime report-only sent");
            return;
        }

        _logger.Warning("remote", $"gpu-bundle-runtime report-only failed code={NormalizeLogValue(reportResult.ErrorCode, "bundle_report_failed")}");
    }

    private static string NormalizeLogValue(string? value, string fallback)
    {
        var normalized = (value ?? "").Trim();
        return string.IsNullOrWhiteSpace(normalized) ? fallback : normalized;
    }
}
