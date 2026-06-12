using OptiClick.Core.Runtime;
using OptiClick.Wpf.Shell.Flow;
using OptiClick.Wpf.Shell.Gpu;
using OptiClick.Wpf.Shell.Selection;

namespace OptiClick.Wpf.Shell.Runtime;

public sealed class RuntimeContextCoordinator
{
    private readonly RuntimeContextFlowController _runtimeContextFlowController;
    private readonly RuntimeSummaryStateController _runtimeSummaryStateController;
    private readonly FlowLogDispatcher _flowLogDispatcher;
    private readonly GpuSelectionCoordinator _gpuSelectionCoordinator;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    public RuntimeContextCoordinator(
        RuntimeContextFlowController runtimeContextFlowController,
        RuntimeSummaryStateController runtimeSummaryStateController,
        FlowLogDispatcher flowLogDispatcher,
        GpuSelectionCoordinator gpuSelectionCoordinator)
    {
        _runtimeContextFlowController = runtimeContextFlowController ?? throw new ArgumentNullException(nameof(runtimeContextFlowController));
        _runtimeSummaryStateController = runtimeSummaryStateController ?? throw new ArgumentNullException(nameof(runtimeSummaryStateController));
        _flowLogDispatcher = flowLogDispatcher ?? throw new ArgumentNullException(nameof(flowLogDispatcher));
        _gpuSelectionCoordinator = gpuSelectionCoordinator ?? throw new ArgumentNullException(nameof(gpuSelectionCoordinator));
    }

    public async Task RefreshAsync(
        RuntimeContextCoordinatorRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            var result = await _runtimeContextFlowController.RefreshAsync(cancellationToken);
            _flowLogDispatcher.Dispatch(result.Logs, request.LogCategory);
            if (!result.IsSuccess)
            {
                return;
            }

            var resolvedContext = await request.ResolveRuntimeContextForGpuSelectionAsync(result.Context, cancellationToken);
            request.ApplyRuntimeSummaryStateUpdate(_runtimeSummaryStateController.Build(resolvedContext, request.Text.SummaryText));
            request.ApplySelectionState(request.SelectionState with
            {
                MultiGpuBlocked = _gpuSelectionCoordinator.MultiGpuBlocked,
                GpuSelectionPending = _gpuSelectionCoordinator.GpuSelectionPending
            });
        }
        finally
        {
            _refreshLock.Release();
        }
    }
}

public sealed record RuntimeContextCoordinatorRequest
{
    public required RuntimeContextCoordinatorText Text { get; init; }

    public required ShellInstallSelectionState SelectionState { get; init; }

    public required Func<RuntimeContext, CancellationToken, Task<RuntimeContext>> ResolveRuntimeContextForGpuSelectionAsync { get; init; }

    public required Action<RuntimeSummaryStateUpdate> ApplyRuntimeSummaryStateUpdate { get; init; }

    public required Action<ShellInstallSelectionState> ApplySelectionState { get; init; }

    public string LogCategory { get; init; } = "runtime";
}
