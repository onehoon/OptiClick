using System.Globalization;
using OptiClick.Core.Runtime;
using OptiClick.Wpf.Shell.RuntimeData;
using OptiClick.Wpf.Shell.Games;

namespace OptiClick.Wpf.Shell.Runtime;

public sealed class RuntimeCatalogFlowController
{
    private static readonly TimeSpan DefaultRemoteServiceHealthProbeTimeout = TimeSpan.FromSeconds(2);

    private readonly IRemoteCatalogPipeline? _remoteCatalogPipeline;
    private readonly ModuleDownloadLinkMapBuilder _moduleDownloadLinkMapBuilder;
    private readonly OptiScalerVariantCatalogBuilder _optiScalerVariantCatalogBuilder;
    private readonly RuntimeCatalogDialogPresenter _dialogPresenter;
    private readonly IRemoteServiceHealthProbe? _remoteServiceHealthProbe;
    private readonly TimeSpan _remoteServiceHealthProbeTimeout;

    public RuntimeCatalogFlowController(
        IRemoteCatalogPipeline? remoteCatalogPipeline,
        ModuleDownloadLinkMapBuilder? moduleDownloadLinkMapBuilder = null,
        RuntimeCatalogDialogPresenter? dialogPresenter = null,
        IRemoteServiceHealthProbe? remoteServiceHealthProbe = null,
        TimeSpan? remoteServiceHealthProbeTimeout = null,
        OptiScalerVariantCatalogBuilder? optiScalerVariantCatalogBuilder = null)
    {
        _remoteCatalogPipeline = remoteCatalogPipeline;
        _moduleDownloadLinkMapBuilder = moduleDownloadLinkMapBuilder ?? new ModuleDownloadLinkMapBuilder();
        _optiScalerVariantCatalogBuilder = optiScalerVariantCatalogBuilder ?? new OptiScalerVariantCatalogBuilder();
        _dialogPresenter = dialogPresenter ?? new RuntimeCatalogDialogPresenter();
        _remoteServiceHealthProbe = remoteServiceHealthProbe;
        _remoteServiceHealthProbeTimeout = remoteServiceHealthProbeTimeout ?? DefaultRemoteServiceHealthProbeTimeout;
    }

    public async Task<RuntimeCatalogFlowResult> RefreshAsync(
        RuntimeCatalogFlowRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Strings);

        var logs = new List<RuntimeFlowLogEntry>();
        var context = request.LatestRuntimeContext ?? new RuntimeContext();
        var strings = request.Strings;

        if (_remoteCatalogPipeline is null)
        {
            logs.Add(Error("remote", "remote catalog pipeline is missing"));
            return new RuntimeCatalogFlowResult
            {
                DidRun = false,
                IsSuccess = false,
                ErrorCode = "gpu_bundle_pipeline_missing",
                SettingsStatusText = Format(strings.RuntimeRemoteCatalogFailed, "gpu_bundle_pipeline_missing"),
                DialogRequest = _dialogPresenter.BuildPipelineMissingDialog(strings),
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
            var health = await TryProbeRemoteServiceHealthAsync(logs, cancellationToken);
            logs.Add(Error("remote", "remote catalog pipeline failed", ex));
            return new RuntimeCatalogFlowResult
            {
                DidRun = true,
                IsSuccess = false,
                ErrorCode = "remote_catalog_unexpected_error",
                SettingsStatusText = Format(strings.RuntimeRemoteCatalogFailed, "remote_catalog_unexpected_error"),
                DialogRequest = _dialogPresenter.BuildUnexpectedErrorDialog(strings, health),
                Logs = logs
            };
        }

        if (pipelineResult.IsSkipped)
        {
            var health = await TryProbeRemoteServiceHealthAsync(logs, cancellationToken);
            var code = NormalizeStatusCode(pipelineResult.ErrorCode, "runtime_data_skipped");
            logs.Add(Warning("remote", $"remote catalog skipped code={code}"));
            return new RuntimeCatalogFlowResult
            {
                DidRun = true,
                IsSuccess = false,
                ErrorCode = code,
                SettingsStatusText = Format(strings.RuntimeRemoteCatalogSkipped, code),
                DialogRequest = _dialogPresenter.BuildSkippedDialog(code, strings, health),
                Logs = logs
            };
        }

        if (!pipelineResult.IsSuccess)
        {
            var health = await TryProbeRemoteServiceHealthAsync(logs, cancellationToken);
            var code = NormalizeStatusCode(pipelineResult.ErrorCode, "runtime_data_failed");
            logs.Add(Error("remote", $"remote catalog failed code={code}"));
            return new RuntimeCatalogFlowResult
            {
                DidRun = true,
                IsSuccess = false,
                ErrorCode = code,
                RuntimeData = pipelineResult.RuntimeData ?? RemoteRuntimeData.Empty,
                SettingsStatusText = Format(strings.RuntimeRemoteCatalogFailed, code),
                DialogRequest = _dialogPresenter.BuildFailedDialog(code, strings, health),
                Logs = logs
            };
        }

        var runtimeData = pipelineResult.RuntimeData ?? RemoteRuntimeData.Empty;
        var catalog = pipelineResult.Catalog ?? ShellGameCatalog.Empty;
        var moduleDownloadLinks = _moduleDownloadLinkMapBuilder.Build(runtimeData.ResourceMaster);
        var variantCatalogResult = _optiScalerVariantCatalogBuilder.Build(runtimeData.ResourceMaster);
        foreach (var log in variantCatalogResult.Logs)
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
                SettingsStatusText = Format(strings.RuntimeRemoteCatalogFailed, "empty_catalog"),
                DialogRequest = _dialogPresenter.BuildEmptyCatalogDialog(strings),
                Logs = logs
            };
        }

        var loadedText = Format(strings.RuntimeRemoteCatalogLoadedScanHint, catalog.Games.Count);
        return new RuntimeCatalogFlowResult
        {
            DidRun = true,
            IsSuccess = true,
            ShouldApplyRemoteDataState = true,
            RuntimeData = runtimeData,
            Catalog = catalog,
            ModuleDownloadLinks = moduleDownloadLinks,
            OptiScalerVariantCatalog = variantCatalogResult.Catalog,
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

    private static string Format(string template, params object[] args)
    {
        return string.Format(CultureInfo.CurrentCulture, template ?? "", args ?? []);
    }

    private static string NormalizeStatusCode(string? value, string fallback)
    {
        var normalized = (value ?? "").Trim();
        return string.IsNullOrWhiteSpace(normalized) ? fallback : normalized;
    }

    private async Task<RemoteServiceHealthSnapshot?> TryProbeRemoteServiceHealthAsync(
        ICollection<RuntimeFlowLogEntry> logs,
        CancellationToken cancellationToken)
    {
        if (_remoteServiceHealthProbe is null)
        {
            return null;
        }

        try
        {
            var health = await ProbeRemoteServiceHealthWithTimeoutAsync(cancellationToken);
            if (health is null)
            {
                logs.Add(Warning("remote", "remote_service_status probe_timeout"));
                return null;
            }

            var cloudflareIndicator = NormalizeStatusCode(health.Cloudflare.Indicator, "unknown");
            var cloudflareDescription = NormalizeStatusCode(health.Cloudflare.Description, "none");
            var githubIndicator = NormalizeStatusCode(health.GitHub.Indicator, "unknown");
            var githubDescription = NormalizeStatusCode(health.GitHub.Description, "none");
            var cloudflareProbeCode = NormalizeStatusCode(health.Cloudflare.ErrorCode, "ok");
            var githubProbeCode = NormalizeStatusCode(health.GitHub.ErrorCode, "ok");
            logs.Add(Info(
                "remote",
                $"remote_service_status cloudflare={cloudflareIndicator} cloudflare_desc=\"{cloudflareDescription}\" cloudflare_probe={cloudflareProbeCode} github={githubIndicator} github_desc=\"{githubDescription}\" github_probe={githubProbeCode}"));
            return health;
        }
        catch (Exception ex)
        {
            logs.Add(Warning("remote", $"remote_service_status probe_failed type={ex.GetType().Name}"));
            return null;
        }
    }

    private async Task<RemoteServiceHealthSnapshot?> ProbeRemoteServiceHealthWithTimeoutAsync(
        CancellationToken cancellationToken)
    {
        if (_remoteServiceHealthProbe is null)
        {
            return null;
        }

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var probeTask = _remoteServiceHealthProbe.ProbeAsync(timeoutCts.Token);
        var timeoutTask = Task.Delay(_remoteServiceHealthProbeTimeout, cancellationToken);
        var completedTask = await Task.WhenAny(probeTask, timeoutTask);
        if (completedTask == probeTask)
        {
            return await probeTask;
        }

        if (cancellationToken.IsCancellationRequested)
        {
            cancellationToken.ThrowIfCancellationRequested();
        }

        await timeoutCts.CancelAsync();
        _ = ObserveTimedOutHealthProbeAsync(probeTask);
        return null;
    }

    private static async Task ObserveTimedOutHealthProbeAsync(Task<RemoteServiceHealthSnapshot> probeTask)
    {
        try
        {
            await probeTask;
        }
        catch
        {
            // Observe late failures from a timed-out diagnostic probe.
        }
    }
}
