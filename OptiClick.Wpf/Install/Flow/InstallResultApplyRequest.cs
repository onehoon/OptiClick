using OptiClick.Core.Install.Planning;
using OptiClick.Core.OptiScaler;
using OptiClick.Core.RuntimeData;
using OptiClick.Wpf.Install.Execution;

namespace OptiClick.Wpf.Install.Flow;

public sealed record InstallResultApplyRequest
{
    public required CoreInstallPlan Plan { get; init; }
    public required ComponentInstallResult InstallResult { get; init; }
    public string InstallPostPopupMessage { get; init; } = "";
    public AttachedRuntimeProfileRows ProfileRows { get; init; } = AttachedRuntimeProfileRows.Empty;
    public OptiScalerIniApplyContext OptiScalerIniApplyContext { get; init; } =
        new OptiScalerIniApplyContext();
    public required InstallFlowText Text { get; init; }
}
