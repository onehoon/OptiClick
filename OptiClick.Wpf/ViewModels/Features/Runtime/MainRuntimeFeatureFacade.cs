using OptiClick.Core.Runtime;
using OptiClick.Wpf.Localization;
using OptiClick.Wpf.Shell.Flow;
using OptiClick.Wpf.Shell.Runtime;
using OptiClick.Wpf.ViewModels.Features.Runtime.DeviceIdentity;
using OptiClick.Wpf.ViewModels.Features.Runtime.GpuManifest;

namespace OptiClick.Wpf.ViewModels.Features.Runtime;

internal sealed class MainRuntimeFeatureFacade
{
    private readonly RuntimeShellState _runtimeShellState;
    private readonly IOperatingSystemSupportPolicy _operatingSystemSupportPolicy;
    private readonly bool _hasGameCardFactory;
    private readonly MainStartupRuntimeFacade _runtimeFacade;
    private readonly RuntimeCatalogCoordinator _runtimeCatalogCoordinator;
    private readonly MainRuntimeCatalogUiFlowController _runtimeCatalogUiFlowController;
    private readonly MainRuntimeCatalogUiFlowContextFactory _runtimeCatalogUiFlowContextFactory;
    private readonly MainRuntimeFlowContextFactory _runtimeFlowContextFactory;
    private readonly RuntimeSummaryStateController _runtimeSummaryStateController;
    private readonly FlowLogDispatcher _flowLogDispatcher;
    private readonly Func<MainRuntimeDataCatalogRefreshRequest> _buildCatalogRefreshRequest;
    private readonly MainRuntimeDeviceIdentityFeature _deviceIdentity;
    private readonly MainRuntimeGpuManifestFeature _gpuManifest;

    public MainRuntimeFeatureFacade(
        RuntimeShellState runtimeShellState,
        IOperatingSystemSupportPolicy operatingSystemSupportPolicy,
        bool hasGameCardFactory,
        MainStartupRuntimeFacade runtimeFacade,
        RuntimeCatalogCoordinator runtimeCatalogCoordinator,
        MainRuntimeCatalogUiFlowController runtimeCatalogUiFlowController,
        MainRuntimeCatalogUiFlowContextFactory runtimeCatalogUiFlowContextFactory,
        MainRuntimeFlowContextFactory runtimeFlowContextFactory,
        RuntimeSummaryStateController runtimeSummaryStateController,
        FlowLogDispatcher flowLogDispatcher,
        Func<MainRuntimeDataCatalogRefreshRequest> buildCatalogRefreshRequest,
        MainRuntimeDeviceIdentityFeature deviceIdentity,
        MainRuntimeGpuManifestFeature gpuManifest)
    {
        _runtimeShellState = runtimeShellState ?? throw new ArgumentNullException(nameof(runtimeShellState));
        _operatingSystemSupportPolicy =
            operatingSystemSupportPolicy ?? throw new ArgumentNullException(nameof(operatingSystemSupportPolicy));
        _hasGameCardFactory = hasGameCardFactory;
        _runtimeFacade = runtimeFacade ?? throw new ArgumentNullException(nameof(runtimeFacade));
        _runtimeCatalogCoordinator =
            runtimeCatalogCoordinator ?? throw new ArgumentNullException(nameof(runtimeCatalogCoordinator));
        _runtimeCatalogUiFlowController =
            runtimeCatalogUiFlowController ?? throw new ArgumentNullException(nameof(runtimeCatalogUiFlowController));
        _runtimeCatalogUiFlowContextFactory =
            runtimeCatalogUiFlowContextFactory ?? throw new ArgumentNullException(nameof(runtimeCatalogUiFlowContextFactory));
        _runtimeFlowContextFactory =
            runtimeFlowContextFactory ?? throw new ArgumentNullException(nameof(runtimeFlowContextFactory));
        _runtimeSummaryStateController =
            runtimeSummaryStateController ?? throw new ArgumentNullException(nameof(runtimeSummaryStateController));
        _flowLogDispatcher = flowLogDispatcher ?? throw new ArgumentNullException(nameof(flowLogDispatcher));
        _buildCatalogRefreshRequest =
            buildCatalogRefreshRequest ?? throw new ArgumentNullException(nameof(buildCatalogRefreshRequest));
        _deviceIdentity = deviceIdentity ?? throw new ArgumentNullException(nameof(deviceIdentity));
        _gpuManifest = gpuManifest ?? throw new ArgumentNullException(nameof(gpuManifest));
    }

    public bool IsOperatingSystemPolicySupported()
    {
        return _runtimeShellState.EnsureOperatingSystemEvaluated(
            _operatingSystemSupportPolicy,
            MainViewModelStatusCodes.Unknown).IsSupported;
    }

    public bool IsUnsupportedOperatingSystem()
    {
        return _runtimeShellState.EnsureOperatingSystemEvaluated(
            _operatingSystemSupportPolicy,
            MainViewModelStatusCodes.Unknown).IsUnsupportedOperatingSystem;
    }

    public Task RefreshRuntimeContextAsync(CancellationToken cancellationToken = default)
    {
        return _runtimeFacade.RefreshRuntimeContextAsync(
            _runtimeFlowContextFactory.CreateRuntimeContextRefreshContext(),
            cancellationToken);
    }

    public DeviceIdentityRulesFlowResult ApplyLocalDeviceIdentityRules()
    {
        return _deviceIdentity.ApplyLocalDeviceIdentityRules();
    }

    public Task ApplyLocalDeviceIdentityRulesAsync(
        RuntimeSummaryStateText text,
        Action<RuntimeSummaryStateUpdate> applyRuntimeSummaryStateUpdate,
        CancellationToken cancellationToken = default)
    {
        return _deviceIdentity.ApplyLocalDeviceIdentityRulesAsync(
            text,
            applyRuntimeSummaryStateUpdate,
            cancellationToken);
    }

    public Task RefreshDeviceIdentityRulesAsync(CancellationToken cancellationToken = default)
    {
        return _deviceIdentity.RefreshDeviceIdentityRulesAsync(cancellationToken);
    }

    public Task RefreshRuntimeDataCatalogForStartupAsync(CancellationToken cancellationToken = default)
    {
        return RefreshRuntimeDataCatalogAsync(
            RuntimeCatalogRefreshMode.BackgroundWarmup,
            cancellationToken);
    }

    public Task RefreshRuntimeDataCatalogAsync(
        RuntimeCatalogRefreshMode refreshMode,
        CancellationToken cancellationToken = default)
    {
        return RefreshRuntimeDataCatalogAsync(
            _buildCatalogRefreshRequest(),
            refreshMode,
            cancellationToken);
    }

    public Task RefreshRuntimeDataCatalogWithSelectedGpuAsync(
        GpuInfo selectedGpu,
        RuntimeCatalogRefreshMode refreshMode,
        CancellationToken cancellationToken = default)
    {
        var selectedContext = _runtimeShellState.LatestRuntimeContext with
        {
            SelectedGpu = selectedGpu
        };
        return RefreshRuntimeDataCatalogAsync(
            _buildCatalogRefreshRequest(),
            refreshMode,
            cancellationToken,
            selectedContext,
            bypassStartupSnapshot: true);
    }

    private Task RefreshRuntimeCatalogAsync(
        RuntimeCatalogCoordinatorRequest request,
        CancellationToken cancellationToken)
    {
        return _runtimeCatalogCoordinator.RefreshAsync(request, cancellationToken);
    }

    private async Task RefreshRuntimeDataCatalogAsync(
        MainRuntimeDataCatalogRefreshRequest request,
        RuntimeCatalogRefreshMode refreshMode,
        CancellationToken cancellationToken,
        RuntimeContext? runtimeContextOverride = null,
        bool bypassStartupSnapshot = false)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!bypassStartupSnapshot && _runtimeShellState.TryGetStartupRemoteCatalogSnapshot(out var startupRemoteCatalogSnapshot))
        {
            await ApplyRuntimeCatalogFlowResultAsync(
                startupRemoteCatalogSnapshot.Result,
                refreshMode,
                cancellationToken,
                isReplay: true,
                preNormalizedErrorCode: startupRemoteCatalogSnapshot.NormalizedErrorCode);
            return;
        }

        var latestRuntimeContext = runtimeContextOverride ?? _runtimeShellState.LatestRuntimeContext;
        await RefreshRuntimeCatalogAsync(
            new RuntimeCatalogCoordinatorRequest
            {
                HasGameCardFactory = _hasGameCardFactory,
                IsMultiGpuBlocked = MultiGpuBlocked,
                IsGpuSelectionPending = GpuSelectionPending,
                IsGpuManifestRestartRequired = _runtimeShellState.IsGpuManifestRestartRequired,
                LatestRemoteCatalogDetailErrorCode = _runtimeShellState.LatestRemoteCatalogDetailErrorCode,
                GpuManifestRestartRequiredErrorCode = _gpuManifest.RestartRequiredErrorCode,
                LatestRuntimeContext = latestRuntimeContext,
                SelectedLanguage = request.SelectedLanguage,
                Text = RuntimeCatalogCoordinatorText.FromAppStrings(request.Strings),
                RefreshMode = refreshMode,
                BuildRuntimeCatalogRequest = request.BuildRuntimeCatalogRequest,
                ApplyMultiGpuBlockedUiState = ApplyMultiGpuBlockedUiState,
                ApplyGpuManifestRestartRequiredState = ApplyGpuManifestRestartRequiredState,
                ShowGpuManifestRestartRequiredDialogOnceAsync = ShowGpuManifestRestartRequiredDialogOnceAsync,
                ApplySettingsStatusText = request.ApplySettingsStatusText,
                ApplyRuntimeCatalogFlowResultAsync = ApplyRuntimeCatalogFlowResultAsync
            },
            cancellationToken);
    }

    public Task ApplyRuntimeCatalogFlowResultAsync(
        RuntimeCatalogFlowResult result,
        RuntimeCatalogRefreshMode refreshMode,
        CancellationToken cancellationToken)
    {
        return _runtimeCatalogUiFlowController.ApplyRuntimeCatalogFlowResultAsync(
            CreateCatalogUiFlowContext(),
            result,
            refreshMode,
            cancellationToken);
    }

    public Task ApplyRuntimeCatalogFlowResultAsync(
        RuntimeCatalogFlowResult result,
        RuntimeCatalogRefreshMode refreshMode,
        CancellationToken cancellationToken,
        bool isReplay,
        string? preNormalizedErrorCode = null)
    {
        return _runtimeCatalogUiFlowController.ApplyRuntimeCatalogFlowResultAsync(
            CreateCatalogUiFlowContext(),
            result,
            refreshMode,
            cancellationToken,
            isReplay,
            preNormalizedErrorCode);
    }

    public MainRuntimeCatalogUiFlowContext CreateCatalogUiFlowContext()
    {
        return _runtimeCatalogUiFlowContextFactory.Create();
    }

    public bool MultiGpuBlocked => _gpuManifest.MultiGpuBlocked;
    public bool GpuSelectionPending => _gpuManifest.GpuSelectionPending;
    public string SelectedGpuLogSource => _gpuManifest.SelectedGpuLogSource;

    public void ApplyMultiGpuBlockedUiState()
    {
        _gpuManifest.ApplyMultiGpuBlockedUiState();
    }

    public void ApplyGpuManifestRestartRequiredState(string detailErrorCode)
    {
        _gpuManifest.ApplyGpuManifestRestartRequiredState(detailErrorCode);
    }

    public void ClearGpuManifestRestartRequiredState()
    {
        _gpuManifest.ClearGpuManifestRestartRequiredState();
    }

    public Task ShowGpuManifestRestartRequiredDialogOnceAsync(
        string errorCode,
        CancellationToken cancellationToken)
    {
        return _gpuManifest.ShowGpuManifestRestartRequiredDialogOnceAsync(errorCode, cancellationToken);
    }

    public Task<IReadOnlyList<GpuInfo>> ResolveManifestSupportedGpuCandidatesAsync(
        RuntimeContext runtimeContext,
        IReadOnlyList<GpuInfo> detectedCandidates,
        CancellationToken cancellationToken)
    {
        return _gpuManifest.ResolveManifestSupportedGpuCandidatesAsync(
            runtimeContext,
            detectedCandidates,
            cancellationToken);
    }

    public RuntimeSummaryStateUpdate BuildLatestRuntimeSummaryStateUpdate(RuntimeSummaryStateText text)
    {
        return _runtimeSummaryStateController.Build(_runtimeShellState.LatestRuntimeContext, text);
    }

    public void DispatchRuntimeLogs(IEnumerable<IFlowLogEntry> logs)
    {
        _flowLogDispatcher.Dispatch(logs, MainViewModelLogCategories.Runtime);
    }
}

internal sealed record MainRuntimeDataCatalogRefreshRequest
{
    public required AppLanguage SelectedLanguage { get; init; }
    public required AppStrings Strings { get; init; }
    public required Func<RuntimeContext, AppLanguage, RuntimeCatalogFlowText, RuntimeCatalogFlowRequest>
        BuildRuntimeCatalogRequest
    {
        get;
        init;
    }

    public required Action<string> ApplySettingsStatusText { get; init; }
}
