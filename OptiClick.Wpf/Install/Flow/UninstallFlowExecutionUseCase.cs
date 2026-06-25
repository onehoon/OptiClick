using OptiClick.Wpf.Install.Planning;
using OptiClick.Wpf.Install.Uninstall;
using InfrastructureUninstall = OptiClick.Infrastructure.Install.Uninstall;

namespace OptiClick.Wpf.Install.Flow;

internal sealed class UninstallFlowExecutionUseCase
{
    private readonly IOptiClickUninstallPlanBuilder _planBuilder;
    private readonly IOptiClickUninstallExecutor _executor;

    public UninstallFlowExecutionUseCase(
        IOptiClickUninstallPlanBuilder planBuilder,
        IOptiClickUninstallExecutor executor)
    {
        _planBuilder = planBuilder ?? throw new ArgumentNullException(nameof(planBuilder));
        _executor = executor ?? throw new ArgumentNullException(nameof(executor));
    }

    public UninstallFlowPlanResult BuildPlan(UninstallFlowExecutionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var logs = new List<UninstallFlowLogEntry>();
        var targetPath = InstallTargetPathNormalizer.NormalizeTargetDirectory(request.TargetPath);
        logs.Add(UninstallFlowLogEntry.Info(
            UninstallFlowLogFormatter.FormatPlanBuildStart(request.SelectedGameId, targetPath)));

        var plan = _planBuilder.BuildPlan(new OptiClickUninstallPlanBuildRequest
        {
            TargetPath = targetPath,
            GameDescriptor = request.ExecutionDescriptor.GameDescriptor,
            FinalProxyDllName = request.SelectionSnapshot.FinalProxyDllName,
            EngineIniProfileRows = request.EngineIniProfileRows
        });
        logs.Add(UninstallFlowLogEntry.Info(
            UninstallFlowLogFormatter.FormatPlanBuildResult(
                plan.Status.ToString(),
                plan.Candidates.Count,
                plan.DirectoryCandidates.Count,
                plan.EngineIniCleanupTargets.Count,
                plan.SkippedFiles.Count,
                plan.ErrorCode)));

        switch (plan.Status)
        {
            case InfrastructureUninstall.UninstallPlanStatus.Ready:
                return BuildReadyPlanResult(request, plan, logs);
            case InfrastructureUninstall.UninstallPlanStatus.NothingToRemove:
                return new UninstallFlowPlanResult
                {
                    Plan = plan,
                    DialogKind = UninstallFlowDialogKind.NoRemovableItems,
                    Logs = logs
                };
            case InfrastructureUninstall.UninstallPlanStatus.InvalidTarget:
            case InfrastructureUninstall.UninstallPlanStatus.ValidationFailed:
            default:
                logs.Add(UninstallFlowLogEntry.Warning(
                    UninstallFlowLogFormatter.FormatPlanRejected(plan.Status.ToString(), plan.ErrorCode)));
                return new UninstallFlowPlanResult
                {
                    Plan = plan,
                    DialogKind = UninstallFlowDialogKind.ValidationFailed,
                    Logs = logs
                };
        }
    }

    public async Task<UninstallFlowExecutionResult> ExecuteAsync(
        UninstallFlowExecutionRequest request,
        InfrastructureUninstall.UninstallPlan plan,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(plan);

        var executionResult = await _executor.ExecuteAsync(plan, cancellationToken);
        var logs = new List<UninstallFlowLogEntry>
        {
            UninstallFlowLogEntry.Info(
                UninstallFlowLogFormatter.FormatExecuteResult(
                    executionResult.Status.ToString(),
                    executionResult.DeletedFiles.Count,
                    executionResult.FailedFiles.Count,
                    executionResult.DeletedDirectories.Count,
                    executionResult.FailedDirectories.Count,
                    executionResult.SkippedFiles.Count,
                    executionResult.CleanedEngineIniEntries.Count,
                    executionResult.FailedEngineIniEntries.Count,
                    executionResult.SkippedEngineIniEntries.Count,
                    executionResult.ErrorCode))
        };

        return new UninstallFlowExecutionResult
        {
            ExecutionResult = executionResult,
            DialogKind = UninstallFlowDialogKind.Completion,
            ShouldRefreshSelection = true,
            Logs = logs
        };
    }

    private UninstallFlowPlanResult BuildReadyPlanResult(
        UninstallFlowExecutionRequest request,
        InfrastructureUninstall.UninstallPlan plan,
        IReadOnlyList<UninstallFlowLogEntry> logs)
    {
        if (plan.Candidates.Count == 0
            && plan.DirectoryCandidates.Count == 0
            && plan.EngineIniCleanupTargets.Count == 0)
        {
            return new UninstallFlowPlanResult
            {
                Plan = plan,
                DialogKind = UninstallFlowDialogKind.NoRemovableItems,
                Logs = logs
            };
        }

        return new UninstallFlowPlanResult
        {
            Plan = plan,
            CanExecute = true,
            DialogKind = UninstallFlowDialogKind.Confirmation,
            Logs = logs
        };
    }
}
