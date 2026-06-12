using OptiClick.Core.Install.Planning;

namespace OptiClick.Wpf.Install.Flow;

public sealed class InstallFlowExecutionUseCase
{
    private readonly InstallFlowPlanPreparationService _planPreparationService;
    private readonly InstallFlowStartGateService _startGateService;
    private readonly InstallFlowComponentExecutionService _componentExecutionService;
    private readonly InstallFlowApplyService _applyService;

    public InstallFlowExecutionUseCase(
        InstallFlowPlanPreparationService planPreparationService,
        InstallFlowStartGateService startGateService,
        InstallFlowComponentExecutionService componentExecutionService,
        InstallFlowApplyService applyService)
    {
        _planPreparationService = planPreparationService ?? throw new ArgumentNullException(nameof(planPreparationService));
        _startGateService = startGateService ?? throw new ArgumentNullException(nameof(startGateService));
        _componentExecutionService = componentExecutionService ?? throw new ArgumentNullException(nameof(componentExecutionService));
        _applyService = applyService ?? throw new ArgumentNullException(nameof(applyService));
    }

    public async Task<InstallFlowResult> ExecuteAsync(
        InstallFlowRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Text);

        var logs = new List<InstallFlowLogEntry>();
        var readiness = InstallFlowReadinessSnapshot.Create(request.ExecutionContext);
        var planPreparation = _planPreparationService.Prepare(
            InstallFlowPreparationRequestFactory.CreatePlanPreparationRequest(request, readiness));
        var plan = planPreparation.Plan;
        var startGate = _startGateService.Resolve(
            InstallFlowPreparationRequestFactory.CreateStartGateRequest(request, readiness, plan, planPreparation));

        if (!startGate.GateDecision.CanStart)
        {
            return CreateBlockedResult(request, plan, startGate);
        }

        var componentExecution = await _componentExecutionService.ExecuteAsync(
            InstallFlowExecutionRequestFactory.CreateComponentExecutionRequest(request, plan),
            cancellationToken);
        logs.AddRange(componentExecution.Logs);

        var apply = _applyService.Apply(
            InstallFlowExecutionRequestFactory.CreateApplyRequest(request, plan, componentExecution));
        logs.AddRange(apply.Logs);

        return new InstallFlowResult
        {
            DidStart = true,
            WasBlocked = false,
            StatusText = apply.ApplyResult.StatusText,
            PopupRequest = apply.ApplyResult.PopupRequest,
            Plan = plan,
            ComponentInstallResult = componentExecution.InstallResult,
            ApplyResult = apply.ApplyResult,
            Logs = logs
        };
    }

    private static InstallFlowResult CreateBlockedResult(
        InstallFlowRequest request,
        CoreInstallPlan plan,
        InstallFlowStartGateResult startGate)
    {
        var gateDecision = startGate.GateDecision;
        var logs = new List<InstallFlowLogEntry>();
        InstallFlowLogEmitter.AddGateBlockedLog(logs, gateDecision.ReasonCode, gateDecision.Stage);

        return new InstallFlowResult
        {
            DidStart = false,
            WasBlocked = true,
            StatusText = InstallFlowLogFormatter.Format(
                request.Text.InstallBlocked,
                gateDecision.ReasonCode),
            Plan = plan,
            GateDecision = gateDecision,
            Logs = logs
        };
    }
}
