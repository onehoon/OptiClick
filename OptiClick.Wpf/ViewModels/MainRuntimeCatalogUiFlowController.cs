using OptiClick.Core.Runtime;
using OptiClick.Wpf.Install.UiState;
using OptiClick.Wpf.Localization;
using OptiClick.Wpf.Models;
using OptiClick.Wpf.Shell.Games.GpuBundle;
using OptiClick.Wpf.Shell.Runtime;
using OptiClick.Wpf.Shell.Selection;

namespace OptiClick.Wpf.ViewModels;

internal sealed class MainRuntimeCatalogUiFlowController
{
    public const string GpuManifestRestartRequiredErrorCode = "gpu_bundle_manifest_restart_required";

    public Task ApplyRuntimeCatalogFlowResultAsync(
        MainRuntimeCatalogUiFlowContext context,
        RuntimeCatalogFlowResult result,
        RuntimeCatalogRefreshMode refreshMode,
        CancellationToken cancellationToken)
    {
        return ApplyRuntimeCatalogFlowResultAsync(
            context,
            result,
            refreshMode,
            cancellationToken,
            isReplay: false);
    }

    public async Task ApplyRuntimeCatalogFlowResultAsync(
        MainRuntimeCatalogUiFlowContext context,
        RuntimeCatalogFlowResult result,
        RuntimeCatalogRefreshMode refreshMode,
        CancellationToken cancellationToken,
        bool isReplay,
        string? preNormalizedErrorCode = null)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(result);

        context.Services.DispatchFlowLogs(result.Logs, MainViewModelLogCategories.Runtime);
        var normalizedErrorCode = string.IsNullOrWhiteSpace(preNormalizedErrorCode)
            ? NormalizeStatusCode(result.ErrorCode, MainViewModelStatusCodes.RuntimeDataFailed)
            : preNormalizedErrorCode;
        var update = context.Services.CreateRuntimeCatalogStateUpdate(
            result,
            normalizedErrorCode);

        if (!isReplay && result.IsSuccess)
        {
            context.Callbacks.CaptureStartupRemoteCatalogSnapshot(result, normalizedErrorCode);
        }

        context.Services.ApplyStateUpdate(update);

        if (result.IsSuccess)
        {
            await ApplySuccessfulRuntimeCatalogResultAsync(
                context,
                update,
                result.ShouldApplyRemoteDataState,
                refreshMode,
                cancellationToken,
                isReplay);
            return;
        }

        if (!string.IsNullOrWhiteSpace(context.State.ReadLatestRemoteCatalogErrorCode()))
        {
            ApplyRemoteCatalogUnavailableSelectionState(context);
        }

        if (update.DialogRequest is null)
        {
            return;
        }

        await context.Services.ShowRemoteCatalogDialogOnceAsync(
            update.DialogRequest,
            cancellationToken);
    }

    public void ApplyMultiGpuBlockedUiState(MainRuntimeCatalogUiFlowContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var strings = context.State.ReadStrings();
        context.Services.ClearScannedGameState();

        if (context.State.ReadVisibleGameCount() > 0)
        {
            context.Services.ReplaceGameCards([], true);
        }
        else
        {
            context.Services.SetSelectedGame(null);
        }

        context.State.ApplySelectionState(new ShellInstallSelectionState
        {
            MultiGpuBlocked = true,
            GpuSelectionPending = false,
            InstallButtonPresentation = new InstallButtonPresentation
            {
                IsEnabled = false,
                ShowInstalling = false,
                IsLoadingBlinkReason = false,
                ReasonCode = InstallButtonReasonCodes.MultiGpuBlocked,
                Text = ""
            }
        });
        context.State.SetScanStatusText(strings.ScanBlockedUnsupportedGpuConfiguration);
    }

    public void ApplyGpuManifestRestartRequiredState(
        MainRuntimeCatalogUiFlowContext context,
        string detailErrorCode)
    {
        ArgumentNullException.ThrowIfNull(context);

        var strings = context.State.ReadStrings();
        var normalizedDetailCode = NormalizeStatusCode(
            detailErrorCode,
            GpuManifestRestartRequiredErrorCode);
        context.State.SetGpuManifestRestartRequired(true);
        context.State.SetRemoteCatalogError(
            GpuManifestRestartRequiredErrorCode,
            normalizedDetailCode);
        context.State.SetSettingsStatusText(string.Format(
            strings.RuntimeRemoteCatalogFailed,
            GpuManifestRestartRequiredErrorCode));
        context.State.SetScanStatusText(string.Format(
            strings.RuntimeCatalogNotReadyForScan,
            GpuManifestRestartRequiredErrorCode));
        context.Services.ClearScannedGameState();
        if (context.State.ReadVisibleGameCount() > 0)
        {
            context.Services.ReplaceGameCards([], false);
        }

        var selectionState = context.State.ReadSelectionState() with
        {
            SheetLoading = false,
            SheetReady = false,
            InstallButtonPresentation = new InstallButtonPresentation
            {
                IsEnabled = false,
                ShowInstalling = false,
                IsLoadingBlinkReason = false,
                ReasonCode = InstallButtonReasonCodes.SheetNotReady,
                Text = ""
            }
        };
        context.State.ApplySelectionState(selectionState);
    }

    public void ClearGpuManifestRestartRequiredState(MainRuntimeCatalogUiFlowContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.State.SetGpuManifestRestartRequired(false);
        context.State.SetGpuManifestRestartDialogShown(false);
        if (string.Equals(
                context.State.ReadLatestRemoteCatalogErrorCode(),
                GpuManifestRestartRequiredErrorCode,
                StringComparison.OrdinalIgnoreCase))
        {
            context.State.SetRemoteCatalogError("", "");
        }
    }

    public async Task ShowGpuManifestRestartRequiredDialogOnceAsync(
        MainRuntimeCatalogUiFlowContext context,
        string errorCode,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.State.ReadGpuManifestRestartDialogShown())
        {
            return;
        }

        context.State.SetGpuManifestRestartDialogShown(true);
        var strings = context.State.ReadStrings();
        var normalizedCode = NormalizeStatusCode(errorCode, GpuManifestRestartRequiredErrorCode);
        await context.Services.ShowDialogAsync(
            new AppDialogRequest
            {
                Kind = AppDialogKind.Blocking,
                Severity = DialogSeverity.Blocking,
                Title = strings.RuntimeCatalogFailedTitle,
                Summary = strings.RuntimeCatalogFailedSummary,
                BulletItems =
                [
                    strings.RuntimeCatalogFailedBullet1,
                    strings.RuntimeCatalogFailedBullet2,
                    $"Error code: {normalizedCode}"
                ],
                ErrorCode = normalizedCode,
                IsBlocking = true,
                CanClose = false,
                CloseOnOverlayClick = false,
                PrimaryButtonText = strings.DialogButtonOk
            },
            cancellationToken);
    }

    public async Task<IReadOnlyList<GpuInfo>> ResolveManifestSupportedGpuCandidatesAsync(
        MainRuntimeCatalogUiFlowContext context,
        RuntimeContext runtimeContext,
        IReadOnlyList<GpuInfo> detectedCandidates,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (detectedCandidates.Count == 0)
        {
            return Array.Empty<GpuInfo>();
        }

        var remote = runtimeContext.RemoteData ?? new RemoteDataOptions();
        var manifestEndpoint = (remote.GpuBundleManifestUrl ?? "").Trim();
        if (string.IsNullOrWhiteSpace(manifestEndpoint))
        {
            if (!remote.AllowMockGpuManifestFallback)
            {
                const string code = "gpu_bundle_manifest_endpoint_missing";
                context.Callbacks.LogRuntimeWarning(
                    $"gpu manifest endpoint missing code={code} restart_required=true");
                ApplyGpuManifestRestartRequiredState(context, code);
                await ShowGpuManifestRestartRequiredDialogOnceAsync(context, code, cancellationToken);
                return Array.Empty<GpuInfo>();
            }

            context.Callbacks.LogRuntimeWarning(
                "gpu manifest endpoint missing; fallback to detected_gpu_candidates");
            return detectedCandidates;
        }

        var manifestRequest = GpuBundleManifestFetchRequestFactory.Create(
            runtimeContext,
            appVersion: context.Services.ReadAppVersion());
        var manifestResult = await context.Services.GpuBundleManifestClient.FetchAsync(
            manifestEndpoint,
            manifestRequest,
            cancellationToken);
        if (!manifestResult.IsSuccess)
        {
            var code = manifestResult.IsSkipped
                ? "gpu_bundle_manifest_skipped"
                : NormalizeStatusCode(manifestResult.ErrorCode, "gpu_bundle_manifest_failed");
            context.Callbacks.LogRuntimeWarning(
                $"gpu manifest fetch failed code={code} restart_required=true");
            ApplyGpuManifestRestartRequiredState(context, code);
            await ShowGpuManifestRestartRequiredDialogOnceAsync(context, code, cancellationToken);
            return Array.Empty<GpuInfo>();
        }

        ClearGpuManifestRestartRequiredState(context);
        var candidateResult = GpuBundleManifestSupportedGpuCandidateResolver.Resolve(
            manifestResult.Manifest,
            runtimeContext,
            detectedCandidates,
            context.Services.GpuBundleManifestRuleResolver);
        foreach (var unsupported in candidateResult.UnsupportedCandidates)
        {
            context.Callbacks.LogRuntimeInfo(
                $"gpu candidate excluded vendor={NormalizeStatusCode(unsupported.Candidate.Vendor, MainViewModelStatusCodes.Unknown)} name=\"{NormalizeStatusCode(unsupported.Candidate.Name, MainViewModelStatusCodes.Unknown)}\" code={unsupported.ErrorCode}");
        }

        return candidateResult.SupportedCandidates;
    }

    private static async Task ApplySuccessfulRuntimeCatalogResultAsync(
        MainRuntimeCatalogUiFlowContext context,
        MainViewModelStateUpdate update,
        bool shouldApplyRemoteDataState,
        RuntimeCatalogRefreshMode refreshMode,
        CancellationToken cancellationToken,
        bool isReplay)
    {
        if (isReplay)
        {
            context.Callbacks.LogRemoteInfo(
                $"remote catalog replay applied from startup snapshot games={context.State.ReadRemoteCatalogGameCount()} visible_games={context.State.ReadVisibleGameCount()}");
            return;
        }

        if (update.ShouldResetRemoteCatalogDialogGate)
        {
            context.Services.ResetRemoteCatalogDialogGate();
        }

        if (update.ShouldRefreshVisibleGames)
        {
            context.Services.RefreshVisibleGamesFromScanMatches();
        }

        if (shouldApplyRemoteDataState && context.State.HasSupportedGamesEntries())
        {
            context.Services.RebuildSupportedGamesRows();
        }

        if (update.ShouldRefreshArchiveReadiness)
        {
            if (refreshMode == RuntimeCatalogRefreshMode.BackgroundWarmup)
            {
                await context.Services.StartStartupPreparationAsync(cancellationToken);
            }
            else
            {
                await context.Services.RefreshArchiveReadinessAsync(cancellationToken);
            }
        }

        context.Callbacks.LogRemoteInfo(
            $"remote catalog loaded games={context.State.ReadRemoteCatalogGameCount()} visible_games={context.State.ReadVisibleGameCount()}");
    }

    private static void ApplyRemoteCatalogUnavailableSelectionState(MainRuntimeCatalogUiFlowContext context)
    {
        var selectionState = context.State.ReadSelectionState() with
        {
            SheetLoading = false,
            SheetReady = false,
            InstallButtonPresentation = new InstallButtonPresentation
            {
                IsEnabled = false,
                ShowInstalling = false,
                IsLoadingBlinkReason = false,
                ReasonCode = InstallButtonReasonCodes.SheetNotReady,
                Text = ""
            }
        };
        context.State.ApplySelectionState(selectionState);
    }

    private static string NormalizeStatusCode(string? value, string fallback)
    {
        var normalized = (value ?? "").Trim();
        return string.IsNullOrWhiteSpace(normalized) ? fallback : normalized;
    }
}

internal sealed class MainRuntimeCatalogUiFlowContext
{
    public required MainRuntimeCatalogUiFlowState State { get; init; }
    public required MainRuntimeCatalogUiFlowServices Services { get; init; }
    public required MainRuntimeCatalogUiFlowCallbacks Callbacks { get; init; }
}

internal sealed class MainRuntimeCatalogUiFlowState
{
    public required Func<AppStrings> ReadStrings { get; init; }
    public required Func<ShellInstallSelectionState> ReadSelectionState { get; init; }
    public required Action<ShellInstallSelectionState> ApplySelectionState { get; init; }
    public required Func<string> ReadLatestRemoteCatalogErrorCode { get; init; }
    public required Action<string, string> SetRemoteCatalogError { get; init; }
    public required Action<bool> SetGpuManifestRestartRequired { get; init; }
    public required Func<bool> ReadGpuManifestRestartDialogShown { get; init; }
    public required Action<bool> SetGpuManifestRestartDialogShown { get; init; }
    public required Func<int> ReadRemoteCatalogGameCount { get; init; }
    public required Func<int> ReadVisibleGameCount { get; init; }
    public required Func<bool> HasSupportedGamesEntries { get; init; }
    public required Action<string> SetSettingsStatusText { get; init; }
    public required Action<string> SetScanStatusText { get; init; }
}

internal sealed class MainRuntimeCatalogUiFlowServices
{
    public required Action<IReadOnlyList<RuntimeFlowLogEntry>, string> DispatchFlowLogs { get; init; }
    public required Func<RuntimeCatalogFlowResult, string, MainViewModelStateUpdate> CreateRuntimeCatalogStateUpdate { get; init; }
    public required Action<MainViewModelStateUpdate> ApplyStateUpdate { get; init; }
    public required Action ResetRemoteCatalogDialogGate { get; init; }
    public required Action RefreshVisibleGamesFromScanMatches { get; init; }
    public required Action RebuildSupportedGamesRows { get; init; }
    public required Func<CancellationToken, Task> StartStartupPreparationAsync { get; init; }
    public required Func<CancellationToken, Task> RefreshArchiveReadinessAsync { get; init; }
    public required Func<AppDialogRequest, CancellationToken, Task> ShowRemoteCatalogDialogOnceAsync { get; init; }
    public required Func<AppDialogRequest, CancellationToken, Task> ShowDialogAsync { get; init; }
    public required Action ClearScannedGameState { get; init; }
    public required Action<IReadOnlyList<GameCardViewModel>, bool> ReplaceGameCards { get; init; }
    public required Action<GameCardViewModel?> SetSelectedGame { get; init; }
    public required IRemoteGpuBundleManifestClient GpuBundleManifestClient { get; init; }
    public required IGpuBundleManifestRuleResolver GpuBundleManifestRuleResolver { get; init; }
    public required Func<string> ReadAppVersion { get; init; }
}

internal sealed class MainRuntimeCatalogUiFlowCallbacks
{
    public required Action<RuntimeCatalogFlowResult, string> CaptureStartupRemoteCatalogSnapshot { get; init; }
    public required Action<string> LogRemoteInfo { get; init; }
    public required Action<string> LogRuntimeInfo { get; init; }
    public required Action<string> LogRuntimeWarning { get; init; }
}
