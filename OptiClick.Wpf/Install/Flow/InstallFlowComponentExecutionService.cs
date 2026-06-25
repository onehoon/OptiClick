using System.Diagnostics;
using OptiClick.Core.Install;
using OptiClick.Core.Install.Planning;
using OptiClick.Core.Runtime;
using OptiClick.Wpf.Install.Execution;
using OptiClick.Wpf.Install.Precheck;

namespace OptiClick.Wpf.Install.Flow;

public sealed class InstallFlowComponentExecutionService
{
    private readonly ComponentInstallContextBuilder _componentInstallContextBuilder;
    private readonly IComponentInstallCoordinator _componentInstallCoordinator;

    public InstallFlowComponentExecutionService(
        ComponentInstallContextBuilder componentInstallContextBuilder,
        IComponentInstallCoordinator componentInstallCoordinator)
    {
        _componentInstallContextBuilder = componentInstallContextBuilder
                                          ?? throw new ArgumentNullException(nameof(componentInstallContextBuilder));
        _componentInstallCoordinator = componentInstallCoordinator
                                       ?? throw new ArgumentNullException(nameof(componentInstallCoordinator));
    }

    public async Task<InstallFlowComponentExecutionResult> ExecuteAsync(
        InstallFlowComponentExecutionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var logs = new List<InstallFlowLogEntry>();
        var context = _componentInstallContextBuilder.Build(new ComponentInstallContextBuildInput
        {
            Plan = request.Plan,
            ExecutionDescriptor = request.ExecutionDescriptor,
            LatestRuntimeContext = request.LatestRuntimeContext,
            LatestArchiveReadiness = request.LatestArchiveReadiness,
            ModuleDownloadLinks = request.ModuleDownloadLinks.Catalog
        });

        ComponentInstallResult installResult;
        var stopwatch = Stopwatch.StartNew();
        try
        {
            installResult = await _componentInstallCoordinator.ExecuteAsync(context, cancellationToken);
        }
        catch (Exception ex)
        {
            InstallFlowLogEmitter.AddExecutionExceptionLog(logs, "execution failed with exception", ex);
            installResult = new ComponentInstallResult
            {
                IsSuccess = false,
                FailedStep = ComponentInstallStepResult.Failed(
                    ComponentInstallName.OptiScalerCore,
                    "execution_exception",
                    ex.Message)
            };
        }
        finally
        {
            stopwatch.Stop();
        }

        InstallFlowLogEmitter.AddComponentStepLogs(logs, installResult);
        return new InstallFlowComponentExecutionResult
        {
            Context = context,
            InstallResult = installResult,
            DurationMs = stopwatch.ElapsedMilliseconds,
            Logs = logs
        };
    }

}

public sealed record InstallFlowComponentExecutionRequest
{
    public required CoreInstallPlan Plan { get; init; }
    public required InstallExecutionDescriptor ExecutionDescriptor { get; init; }
    public required RuntimeContext LatestRuntimeContext { get; init; }
    public required ArchiveReadinessSnapshot LatestArchiveReadiness { get; init; }
    public required InstallPrecheckSnapshot Precheck { get; init; }
    public required ModuleDownloadLinkContext ModuleDownloadLinks { get; init; }
}

public sealed record InstallFlowComponentExecutionResult
{
    public required ComponentInstallContext Context { get; init; }
    public required ComponentInstallResult InstallResult { get; init; }
    public required long DurationMs { get; init; }
    public IReadOnlyList<InstallFlowLogEntry> Logs { get; init; } = Array.Empty<InstallFlowLogEntry>();
}
