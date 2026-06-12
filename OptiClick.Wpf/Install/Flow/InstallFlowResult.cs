using OptiClick.Core.Install.Planning;
using OptiClick.Wpf.Install.Execution;
using OptiClick.Wpf.Install.Gates;
using OptiClick.Wpf.Install.UiState;

namespace OptiClick.Wpf.Install.Flow;

public sealed record InstallFlowResult
{
    public bool DidStart { get; init; }
    public bool WasBlocked { get; init; }
    public string StatusText { get; init; } = "";
    public PopupPresentationRequest? PopupRequest { get; init; }
    public CoreInstallPlan? Plan { get; init; }
    public InstallStartGateDecision? GateDecision { get; init; }
    public ComponentInstallResult? ComponentInstallResult { get; init; }
    public InstallResultApplyResult? ApplyResult { get; init; }
    public IReadOnlyList<InstallFlowLogEntry> Logs { get; init; } = [];
}
