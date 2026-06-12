using OptiClick.Core.Runtime;
using OptiClick.Wpf.Models;
using OptiClick.Wpf.Shell.Dialogs;
using OptiClick.Wpf.Shell.Gpu;
using OptiClick.Wpf.Shell.Runtime;
using OptiClick.Wpf.Shell.Selection;
using OptiClick.Wpf.Threading;

namespace OptiClick.Wpf.ViewModels;

internal sealed class MainRuntimeFlowContextFactory
{
    private readonly MainRuntimeFlowContextFactoryInput _input;

    public MainRuntimeFlowContextFactory(MainRuntimeFlowContextFactoryInput input)
    {
        _input = input ?? throw new ArgumentNullException(nameof(input));
    }

    public MainRuntimeContextRefreshContext CreateRuntimeContextRefreshContext()
    {
        return new MainRuntimeContextRefreshContext
        {
            BuildRuntimeContextCoordinatorRequest = BuildRuntimeContextCoordinatorRequest,
            RefreshRuntimeContextAsync = (request, ct) =>
                _input.Dependencies.RuntimeContextCoordinator.RefreshAsync(request, ct)
        };
    }

    public MainDeviceIdentityRulesContext CreateDeviceIdentityRulesContext()
    {
        return new MainDeviceIdentityRulesContext
        {
            RefreshAsync = ct => _input.DeviceRulesRefreshLock.TryRunExclusiveAsync(
                async identityRuleCancellationToken =>
                {
                    var result = await _input.Dependencies.DeviceIdentityRulesFlowController.RefreshAsync(
                        identityRuleCancellationToken);
                    _input.DispatchRuntimeLogs(result.Logs, _input.RuntimeLogCategory);
                    if (!result.DidRun || !result.IsSuccess)
                    {
                        return;
                    }

                    _input.ApplyRuntimeSummaryStateUpdate(_input.BuildRuntimeSummaryStateUpdate());
                },
                ct)
        };
    }

    public MainRuntimeCatalogRefreshContext CreateRuntimeCatalogRefreshContext()
    {
        return new MainRuntimeCatalogRefreshContext
        {
            RefreshRuntimeCatalogAsync = _input.RefreshRuntimeCatalogAsync
        };
    }

    private RuntimeContextCoordinatorRequest BuildRuntimeContextCoordinatorRequest()
    {
        return new RuntimeContextCoordinatorRequest
        {
            Text = RuntimeContextCoordinatorText.FromAppStrings(_input.ReadStrings()),
            SelectionState = _input.ReadSelectionState(),
            ResolveRuntimeContextForGpuSelectionAsync = ResolveRuntimeContextForGpuSelectionAsync,
            ApplyRuntimeSummaryStateUpdate = _input.ApplyRuntimeSummaryStateUpdate,
            ApplySelectionState = _input.ApplySelectionState,
            LogCategory = _input.RuntimeLogCategory
        };
    }

    private async Task<RuntimeContext> ResolveRuntimeContextForGpuSelectionAsync(
        RuntimeContext context,
        CancellationToken cancellationToken)
    {
        return await _input.Dependencies.GpuSelectionCoordinator.ResolveAsync(
            new GpuSelectionCoordinatorRequest
            {
                Context = context ?? new RuntimeContext(),
                ResolveSupportedGpuCandidatesAsync = _input.ResolveManifestSupportedGpuCandidatesAsync,
                PromptDualGpuSelectionAsync = PromptDualGpuSelectionAsync,
                ShowMultiGpuBlockedPopupAsync = ShowMultiGpuBlockedPopupAsync,
                ApplyMultiGpuBlockedUiState = _input.ApplyMultiGpuBlockedUiState,
                ReadManifestRestartRequired = _input.ReadManifestRestartRequired,
                LogInfo = _input.LogRuntimeInfo,
                LogWarning = _input.LogRuntimeWarning
            },
            cancellationToken);
    }

    private async Task ShowMultiGpuBlockedPopupAsync(CancellationToken cancellationToken)
    {
        var strings = _input.ReadStrings();
        await _input.ShowDialogAsync(
            new AppDialogRequest
            {
                Kind = AppDialogKind.Blocking,
                Severity = DialogSeverity.Warning,
                Title = strings.GpuUnsupportedConfigurationTitle,
                Summary = strings.GpuUnsupportedConfigurationSummary,
                IsBlocking = true,
                CloseOnOverlayClick = false
            },
            cancellationToken);
    }

    private async Task<GpuInfo?> PromptDualGpuSelectionAsync(
        GpuInfo firstGpu,
        GpuInfo secondGpu,
        CancellationToken cancellationToken)
    {
        var isKoreanUi = _input.IsKoreanUi();
        var request = new AppDialogRequest
        {
            Kind = AppDialogKind.GpuSelection,
            Severity = DialogSeverity.Warning,
            Title = isKoreanUi ? "GPU 선택" : "GPU Selection",
            Summary = isKoreanUi
                ? "듀얼 GPU가 감지되었습니다.\nOptiScaler를 어떤 GPU 기준으로 설치할지 선택해 주세요.\n선택한 GPU 기준으로 설치됩니다.\n선택한 GPU와 다른 GPU로 게임을 실행하면 정상 동작하지 않을 수 있습니다."
                : "Dual GPUs were detected.\nSelect which GPU OptiScaler should be installed for.\nInstallation will use settings for the selected GPU.\nIt may not work correctly if the game is run on the other GPU.",
            PrimaryButtonText = GpuSelectionCoordinator.BuildGpuSelectionButtonText(firstGpu, 1),
            SecondaryButtonText = GpuSelectionCoordinator.BuildGpuSelectionButtonText(secondGpu, 2),
            PrimaryResult = AppDialogResult.Continue,
            SecondaryResult = AppDialogResult.Cancel,
            IsBlocking = true,
            CanClose = false,
            CloseOnOverlayClick = false
        };

        var result = await _input.ShowDialogAsync(request, cancellationToken);
        if (result == AppDialogResult.Continue)
        {
            return firstGpu;
        }

        if (result == AppDialogResult.Cancel)
        {
            return secondGpu;
        }

        return null;
    }
}

internal sealed record MainRuntimeFlowContextFactoryInput
{
    public required MainRuntimeFlowResolvedDependencies Dependencies { get; init; }
    public required SemaphoreSlim DeviceRulesRefreshLock { get; init; }
    public required Func<OptiClick.Wpf.Localization.AppStrings> ReadStrings { get; init; }
    public required Func<bool> IsKoreanUi { get; init; }
    public required Func<ShellInstallSelectionState> ReadSelectionState { get; init; }
    public required Action<ShellInstallSelectionState> ApplySelectionState { get; init; }
    public required Func<RuntimeSummaryStateUpdate> BuildRuntimeSummaryStateUpdate { get; init; }
    public required Action<RuntimeSummaryStateUpdate> ApplyRuntimeSummaryStateUpdate { get; init; }
    public required Func<RuntimeContext, IReadOnlyList<GpuInfo>, CancellationToken, Task<IReadOnlyList<GpuInfo>>>
        ResolveManifestSupportedGpuCandidatesAsync
    {
        get;
        init;
    }

    public required Action ApplyMultiGpuBlockedUiState { get; init; }
    public required Func<bool> ReadManifestRestartRequired { get; init; }
    public required Action<IReadOnlyList<RuntimeFlowLogEntry>, string> DispatchRuntimeLogs { get; init; }
    public required Func<AppDialogRequest, CancellationToken, Task<AppDialogResult>> ShowDialogAsync { get; init; }
    public required Action<string> LogRuntimeInfo { get; init; }
    public required Action<string> LogRuntimeWarning { get; init; }
    public required Func<RuntimeCatalogRefreshMode, CancellationToken, Task> RefreshRuntimeCatalogAsync { get; init; }
    public string RuntimeLogCategory { get; init; } = MainViewModelLogCategories.Runtime;
}
