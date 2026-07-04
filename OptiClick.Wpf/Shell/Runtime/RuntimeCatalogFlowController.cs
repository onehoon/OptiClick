using OptiClick.Core.Runtime;
using OptiClick.Wpf.Localization;
using OptiClick.Wpf.Models;
using OptiClick.Wpf.Shell.RuntimeData;
using OptiClick.Wpf.Shell.Games;

namespace OptiClick.Wpf.Shell.Runtime;

public sealed class RuntimeCatalogFlowController
{
    private readonly IRemoteCatalogPipeline? _remoteCatalogPipeline;
    private readonly ModuleDownloadLinkMapBuilder _moduleDownloadLinkMapBuilder;
    private readonly OptiScalerVariantCatalogBuilder _optiScalerVariantCatalogBuilder;
    private readonly Fsr4VariantCatalogBuilder _fsr4VariantCatalogBuilder;
    private readonly RuntimeCatalogDialogPresenter _dialogPresenter;

    public RuntimeCatalogFlowController(
        IRemoteCatalogPipeline? remoteCatalogPipeline,
        ModuleDownloadLinkMapBuilder? moduleDownloadLinkMapBuilder = null,
        RuntimeCatalogDialogPresenter? dialogPresenter = null,
        OptiScalerVariantCatalogBuilder? optiScalerVariantCatalogBuilder = null,
        Fsr4VariantCatalogBuilder? fsr4VariantCatalogBuilder = null)
    {
        _remoteCatalogPipeline = remoteCatalogPipeline;
        _moduleDownloadLinkMapBuilder = moduleDownloadLinkMapBuilder ?? new ModuleDownloadLinkMapBuilder();
        _optiScalerVariantCatalogBuilder = optiScalerVariantCatalogBuilder ?? new OptiScalerVariantCatalogBuilder();
        _fsr4VariantCatalogBuilder = fsr4VariantCatalogBuilder ?? new Fsr4VariantCatalogBuilder();
        _dialogPresenter = dialogPresenter ?? new RuntimeCatalogDialogPresenter();
    }

    public async Task<RuntimeCatalogFlowResult> RefreshAsync(
        RuntimeCatalogFlowRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Text);

        var logs = new List<RuntimeFlowLogEntry>();
        var context = request.LatestRuntimeContext ?? new RuntimeContext();
        var text = request.Text;

        if (_remoteCatalogPipeline is null)
        {
            logs.Add(Error("remote", "remote catalog pipeline is missing"));
            return new RuntimeCatalogFlowResult
            {
                DidRun = false,
                IsSuccess = false,
                ErrorCode = "gpu_bundle_pipeline_missing",
                SettingsStatusText = LocalizedTextFormatter.Format(
                    text.RuntimeRemoteCatalogFailed,
                    "gpu_bundle_pipeline_missing"),
                DialogRequest = _dialogPresenter.BuildPipelineMissingDialog(text),
                Logs = logs
            };
        }

        logs.Add(Info("remote", BuildRemoteEndpointLogSummary(context.RemoteData)));

        RemoteCatalogPipelineResult pipelineResult;
        try
        {
            pipelineResult = await _remoteCatalogPipeline.LoadAsync(
                context,
                request.SelectedLanguage,
                cancellationToken);
            logs.Add(Info(
                "gpu-bundle-merge",
                $"runtime_games={pipelineResult.RuntimeGameCount} bundle_games={pipelineResult.BundleGameCount} matched={pipelineResult.MatchedGameCount} supported={pipelineResult.SupportedGameCount}"));
        }
        catch (Exception ex)
        {
            logs.Add(Error("remote", "remote catalog pipeline failed", ex));
            return new RuntimeCatalogFlowResult
            {
                DidRun = true,
                IsSuccess = false,
                ErrorCode = "remote_catalog_unexpected_error",
                SettingsStatusText = LocalizedTextFormatter.Format(
                    text.RuntimeRemoteCatalogFailed,
                    "remote_catalog_unexpected_error"),
                DialogRequest = _dialogPresenter.BuildUnexpectedErrorDialog(text),
                Logs = logs
            };
        }

        if (pipelineResult.IsSkipped)
        {
            var code = NormalizeStatusCode(pipelineResult.ErrorCode, "runtime_data_skipped");
            logs.Add(Warning("remote", $"remote catalog skipped code={code}"));
            return new RuntimeCatalogFlowResult
            {
                DidRun = true,
                IsSuccess = false,
                ErrorCode = code,
                SettingsStatusText = LocalizedTextFormatter.Format(text.RuntimeRemoteCatalogSkipped, code),
                DialogRequest = _dialogPresenter.BuildSkippedDialog(code, text),
                Logs = logs
            };
        }

        if (pipelineResult.IsAuthV2BusinessStatus)
        {
            var code = NormalizeStatusCode(pipelineResult.AuthV2Status, NormalizeStatusCode(pipelineResult.ErrorCode, "auth_v2_session_not_ready"));
            logs.Add(Warning(
                "remote-v2",
                $"auth business status status={code} candidates={pipelineResult.AuthV2Candidates.Count}"));
            return new RuntimeCatalogFlowResult
            {
                DidRun = true,
                IsSuccess = false,
                ErrorCode = code,
                IsAuthV2BusinessStatus = true,
                AuthV2Status = code,
                AuthV2Candidates = pipelineResult.AuthV2Candidates,
                RuntimeData = pipelineResult.RuntimeData ?? RemoteRuntimeData.Empty,
                SettingsStatusText = LocalizedTextFormatter.Format(text.RuntimeRemoteCatalogFailed, code),
                DialogRequest = BuildAuthV2BusinessStatusDialog(code, pipelineResult.AuthV2Candidates, text),
                Logs = logs
            };
        }

        if (!pipelineResult.IsSuccess)
        {
            var code = NormalizeStatusCode(pipelineResult.ErrorCode, "runtime_data_failed");
            logs.Add(Error("remote", $"remote catalog failed code={code}"));
            return new RuntimeCatalogFlowResult
            {
                DidRun = true,
                IsSuccess = false,
                ErrorCode = code,
                RuntimeData = pipelineResult.RuntimeData ?? RemoteRuntimeData.Empty,
                SettingsStatusText = LocalizedTextFormatter.Format(text.RuntimeRemoteCatalogFailed, code),
                DialogRequest = IsGpuDetectionFailedCatalogError(code, context)
                    ? _dialogPresenter.BuildGpuDetectionFailedDialog(text, request.IsGpuDetectionRetryAttempt)
                    : IsUnsupportedGpuCatalogError(code)
                    ? _dialogPresenter.BuildUnsupportedGpuDialog(text)
                    : _dialogPresenter.BuildFailedDialog(code, text),
                Logs = logs
            };
        }

        var runtimeData = pipelineResult.RuntimeData ?? RemoteRuntimeData.Empty;
        var catalog = pipelineResult.Catalog ?? ShellGameCatalog.Empty;
        var moduleDownloadLinks = ModuleDownloadLinkContext.FromEntries(
            _moduleDownloadLinkMapBuilder.Build(runtimeData.ResourceMaster));
        var variantCatalogResult = _optiScalerVariantCatalogBuilder.Build(runtimeData.ResourceMaster);
        var fsr4VariantCatalogResult = _fsr4VariantCatalogBuilder.Build(runtimeData.ResourceMaster);
        foreach (var log in variantCatalogResult.Logs)
        {
            logs.Add(log);
        }
        foreach (var log in fsr4VariantCatalogResult.Logs)
        {
            logs.Add(log);
        }

        if (catalog.Games.Count == 0)
        {
            logs.Add(Error("remote", "remote catalog mapped zero cards"));
            return new RuntimeCatalogFlowResult
            {
                DidRun = true,
                IsSuccess = false,
                ShouldApplyRemoteDataState = true,
                ErrorCode = "empty_catalog",
                RuntimeData = runtimeData,
                Catalog = catalog,
                ModuleDownloadLinks = moduleDownloadLinks,
                OptiScalerVariantCatalog = variantCatalogResult.Catalog,
                Fsr4VariantCatalog = fsr4VariantCatalogResult.Catalog,
                SettingsStatusText = LocalizedTextFormatter.Format(text.RuntimeRemoteCatalogFailed, "empty_catalog"),
                DialogRequest = _dialogPresenter.BuildEmptyCatalogDialog(text),
                Logs = logs
            };
        }

        var loadedText = LocalizedTextFormatter.Format(
            text.RuntimeRemoteCatalogLoadedScanHint,
            catalog.Games.Count);
        return new RuntimeCatalogFlowResult
        {
            DidRun = true,
            IsSuccess = true,
            ShouldApplyRemoteDataState = true,
            RuntimeData = runtimeData,
            Catalog = catalog,
            ModuleDownloadLinks = moduleDownloadLinks,
            OptiScalerVariantCatalog = variantCatalogResult.Catalog,
            Fsr4VariantCatalog = fsr4VariantCatalogResult.Catalog,
            SettingsStatusText = loadedText,
            ScanStatusText = loadedText,
            ResetRemoteCatalogDialogGate = true,
            ShouldRefreshVisibleGames = true,
            ShouldRefreshArchiveReadiness = true,
            Logs = logs
        };
    }

    private static string BuildRemoteEndpointLogSummary(RemoteDataOptions? remoteData)
    {
        var safeRemoteData = remoteData ?? new RemoteDataOptions();
        if (safeRemoteData.IsV2)
        {
            var authConfigured = !string.IsNullOrWhiteSpace(safeRemoteData.AuthV2BaseUrl);
            var dataConfigured = !string.IsNullOrWhiteSpace(safeRemoteData.DataV2BaseUrl);
            return $"endpoint_status protocol=v2 auth={(authConfigured ? "configured" : "missing")} data={(dataConfigured ? "configured" : "missing")}";
        }

        var runtimeDataConfigured = !string.IsNullOrWhiteSpace(safeRemoteData.GetEffectiveRuntimeDataUrl());
        var manifestConfigured = !string.IsNullOrWhiteSpace(safeRemoteData.GpuBundleManifestUrl);
        var bundleConfigured = !string.IsNullOrWhiteSpace(safeRemoteData.GpuBundleUrl);
        return $"endpoint_status runtime_data={(runtimeDataConfigured ? "configured" : "missing")} manifest={(manifestConfigured ? "configured" : "missing")} bundle={(bundleConfigured ? "configured" : "missing")}";
    }

    private AppDialogRequest BuildAuthV2BusinessStatusDialog(
        string status,
        IReadOnlyList<GpuInfo> candidates,
        RuntimeCatalogFlowText text)
    {
        if (string.Equals(status, "unsupported", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "gpu_unsupported", StringComparison.OrdinalIgnoreCase))
        {
            return _dialogPresenter.BuildUnsupportedGpuDialog(text);
        }

        if (string.Equals(status, "gpu_selection_required", StringComparison.OrdinalIgnoreCase))
        {
            return _dialogPresenter.BuildAuthV2GpuSelectionRequiredDialog(text, FormatCandidateLabels(candidates, text));
        }

        if (string.Equals(status, "invalid_selected_gpu", StringComparison.OrdinalIgnoreCase))
        {
            return _dialogPresenter.BuildUnsupportedGpuDialog(text);
        }

        if (string.Equals(status, "multi_gpu_unsupported", StringComparison.OrdinalIgnoreCase))
        {
            return _dialogPresenter.BuildAuthV2MultiGpuUnsupportedDialog(text);
        }

        return _dialogPresenter.BuildFailedDialog(status, text);
    }

    private static IReadOnlyList<string> FormatCandidateLabels(
        IReadOnlyList<GpuInfo> candidates,
        RuntimeCatalogFlowText text)
    {
        if (candidates is not { Count: > 0 })
        {
            return [];
        }

        return candidates
            .Where(static candidate => !string.IsNullOrWhiteSpace(candidate.Name))
            .Select(candidate =>
            {
                var vendor = NormalizeStatusCode(candidate.Vendor, text.StatusUnknown);
                var name = NormalizeStatusCode(candidate.Name, text.StatusUnknown);
                return $"{vendor}: {name}";
            })
            .ToArray();
    }

    private static RuntimeFlowLogEntry Info(string category, string message)
    {
        return new RuntimeFlowLogEntry
        {
            Level = "info",
            Category = category,
            Message = message
        };
    }

    private static RuntimeFlowLogEntry Warning(string category, string message)
    {
        return new RuntimeFlowLogEntry
        {
            Level = "warning",
            Category = category,
            Message = message
        };
    }

    private static RuntimeFlowLogEntry Error(string category, string message, Exception? exception = null)
    {
        return new RuntimeFlowLogEntry
        {
            Level = "error",
            Category = category,
            Message = message,
            Exception = exception
        };
    }

    private static string NormalizeStatusCode(string? value, string fallback)
    {
        var normalized = (value ?? "").Trim();
        return string.IsNullOrWhiteSpace(normalized) ? fallback : normalized;
    }

    private static bool IsUnsupportedGpuCatalogError(string errorCode)
    {
        return string.Equals(errorCode, "bundle_rule_not_matched", StringComparison.OrdinalIgnoreCase)
               || string.Equals(errorCode, "gpu_unsupported", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsGpuDetectionFailedCatalogError(string errorCode, RuntimeContext context)
    {
        if (string.Equals(errorCode, "gpu_detection_failed", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!string.Equals(errorCode, "gpu_unsupported", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(errorCode, "gpu_not_found", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var detection = context.HardwareDetection ?? new RuntimeHardwareDetectionInfo();
        if (string.Equals(detection.GpuInfoSource, "unsupported", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return string.Equals(detection.GpuInfoSource, "fallback", StringComparison.OrdinalIgnoreCase)
               || IsUnknownGpuFallback(context.SelectedGpu)
               || context.Gpus.Any(IsUnknownGpuFallback);
    }

    private static bool IsUnknownGpuFallback(GpuInfo? gpu)
    {
        return gpu is not null
               && string.Equals((gpu.Name ?? "").Trim(), "Unknown GPU", StringComparison.OrdinalIgnoreCase)
               && string.Equals((gpu.Vendor ?? "").Trim(), "Unknown", StringComparison.OrdinalIgnoreCase);
    }
}
