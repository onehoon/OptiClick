using OptiClick.Wpf.Logging;
using OptiClick.Wpf.Shell.Flow;
using OptiClick.Wpf.Shell.Runtime;
using OptiClick.Wpf.Threading;

namespace OptiClick.Wpf.ViewModels.Features.Runtime.DeviceIdentity;

internal sealed class MainRuntimeDeviceIdentityFeature
{
    private readonly RuntimeShellState _runtimeShellState;
    private readonly MainStartupRuntimeFacade _runtimeFacade;
    private readonly SemaphoreSlim _deviceRulesRefreshLock;
    private readonly DeviceIdentityRulesFlowController _deviceIdentityRulesFlowController;
    private readonly MainRuntimeFlowContextFactory _runtimeFlowContextFactory;
    private readonly RuntimeSummaryStateController _runtimeSummaryStateController;
    private readonly FlowLogDispatcher _flowLogDispatcher;

    public MainRuntimeDeviceIdentityFeature(
        RuntimeShellState runtimeShellState,
        MainStartupRuntimeFacade runtimeFacade,
        SemaphoreSlim deviceRulesRefreshLock,
        DeviceIdentityRulesFlowController deviceIdentityRulesFlowController,
        MainRuntimeFlowContextFactory runtimeFlowContextFactory,
        RuntimeSummaryStateController runtimeSummaryStateController,
        FlowLogDispatcher flowLogDispatcher)
    {
        _runtimeShellState = runtimeShellState ?? throw new ArgumentNullException(nameof(runtimeShellState));
        _runtimeFacade = runtimeFacade ?? throw new ArgumentNullException(nameof(runtimeFacade));
        _deviceRulesRefreshLock =
            deviceRulesRefreshLock ?? throw new ArgumentNullException(nameof(deviceRulesRefreshLock));
        _deviceIdentityRulesFlowController =
            deviceIdentityRulesFlowController ?? throw new ArgumentNullException(nameof(deviceIdentityRulesFlowController));
        _runtimeFlowContextFactory =
            runtimeFlowContextFactory ?? throw new ArgumentNullException(nameof(runtimeFlowContextFactory));
        _runtimeSummaryStateController =
            runtimeSummaryStateController ?? throw new ArgumentNullException(nameof(runtimeSummaryStateController));
        _flowLogDispatcher = flowLogDispatcher ?? throw new ArgumentNullException(nameof(flowLogDispatcher));
    }

    public DeviceIdentityRulesFlowResult ApplyLocalDeviceIdentityRules()
    {
        return _deviceIdentityRulesFlowController.ApplyLocalCache();
    }

    public Task ApplyLocalDeviceIdentityRulesAsync(
        RuntimeSummaryStateText text,
        Action<RuntimeSummaryStateUpdate> applyRuntimeSummaryStateUpdate,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(applyRuntimeSummaryStateUpdate);

        return _deviceRulesRefreshLock.TryRunExclusiveAsync(
            _ =>
            {
                var result = ApplyLocalDeviceIdentityRules();
                DispatchRuntimeLogs(result.Logs);
                if (result.DidRun && result.IsSuccess)
                {
                    applyRuntimeSummaryStateUpdate(BuildRuntimeSummaryStateUpdate(text));
                }

                return Task.CompletedTask;
            },
            cancellationToken);
    }

    public Task RefreshDeviceIdentityRulesAsync(CancellationToken cancellationToken = default)
    {
        return _runtimeFacade.RefreshDeviceIdentityRulesAsync(
            _runtimeFlowContextFactory.CreateDeviceIdentityRulesContext(),
            cancellationToken);
    }

    private void DispatchRuntimeLogs(IEnumerable<RuntimeFlowLogEntry> logs)
    {
        _flowLogDispatcher.Dispatch(logs, MainViewModelLogCategories.Runtime);
    }

    private RuntimeSummaryStateUpdate BuildRuntimeSummaryStateUpdate(RuntimeSummaryStateText text)
    {
        return _runtimeSummaryStateController.Build(_runtimeShellState.LatestRuntimeContext, text);
    }
}
