using OptiClick.Core.Install.Planning;
using OptiClick.Core.OptiScaler;
using OptiClick.Core.RuntimeData;
using OptiClick.Wpf.Install.Execution;
using OptiClick.Wpf.Install.Presentation;

namespace OptiClick.Wpf.Install.Flow;

public sealed class InstallFlowApplyService
{
    private readonly IInstallResultApplier _installResultApplier;

    public InstallFlowApplyService(IInstallResultApplier installResultApplier)
    {
        _installResultApplier = installResultApplier ?? throw new ArgumentNullException(nameof(installResultApplier));
    }

    public InstallFlowApplyResult Apply(InstallFlowApplyRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Text);

        var applyResult = _installResultApplier.Apply(new InstallResultApplyRequest
        {
            Plan = request.Plan,
            InstallResult = request.InstallResult,
            InstallPostPopupMessage = request.InstallPostPopupMessage,
            ProfileRows = request.ProfileRows,
            OptiScalerIniApplyContext = request.OptiScalerIniApplyContext,
            Text = request.Text
        });
        var logs = new List<InstallFlowLogEntry>();
        logs.AddRange(applyResult.Logs);
        InstallFlowLogEmitter.AddCompletionLog(
            logs,
            request.Plan.GameId,
            request.ComponentContext,
            request.InstallResult,
            applyResult,
            request.DurationMs);

        return new InstallFlowApplyResult
        {
            ApplyResult = applyResult,
            Logs = logs
        };
    }
}

public sealed record InstallFlowApplyRequest
{
    public required CoreInstallPlan Plan { get; init; }
    public required ComponentInstallResult InstallResult { get; init; }
    public required ComponentInstallContext ComponentContext { get; init; }
    public required long DurationMs { get; init; }
    public string InstallPostPopupMessage { get; init; } = "";
    public AttachedRuntimeProfileRows ProfileRows { get; init; } = AttachedRuntimeProfileRows.Empty;
    public OptiScalerIniApplyContext OptiScalerIniApplyContext { get; init; } = new();
    public required InstallFlowText Text { get; init; }
}

public sealed record InstallFlowApplyResult
{
    public required InstallResultApplyResult ApplyResult { get; init; }
    public IReadOnlyList<InstallFlowLogEntry> Logs { get; init; } = Array.Empty<InstallFlowLogEntry>();
}
