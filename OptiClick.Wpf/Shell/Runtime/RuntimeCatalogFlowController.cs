using OptiClick.Core.Runtime;
using OptiClick.Wpf.Localization;
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
                DialogRequest = _dialogPresenter.BuildFailedDialog(code, text),
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
        var runtimeDataConfigured = !string.IsNullOrWhiteSpace(safeRemoteData.GetEffectiveRuntimeDataUrl());
        var manifestConfigured = !string.IsNullOrWhiteSpace(safeRemoteData.GpuBundleManifestUrl);
        var bundleConfigured = !string.IsNullOrWhiteSpace(safeRemoteData.GpuBundleUrl);
        return $"endpoint_status runtime_data={(runtimeDataConfigured ? "configured" : "missing")} manifest={(manifestConfigured ? "configured" : "missing")} bundle={(bundleConfigured ? "configured" : "missing")}";
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
}
