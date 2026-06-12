using OptiClick.Core.Install.Planning;

namespace OptiClick.Wpf.Install.Flow;

internal static class InstallFlowExecutionRequestFactory
{
    public static InstallFlowComponentExecutionRequest CreateComponentExecutionRequest(
        InstallFlowRequest request,
        CoreInstallPlan plan)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(plan);

        var context = request.ExecutionContext;
        return new InstallFlowComponentExecutionRequest
        {
            Plan = plan,
            ExecutionDescriptor = context.ExecutionDescriptor,
            LatestRuntimeContext = context.LatestRuntimeContext,
            LatestArchiveReadiness = context.LatestArchiveReadiness,
            Precheck = context.SelectionSnapshot.Precheck,
            ModuleDownloadLinks = context.ModuleDownloadLinks
        };
    }

    public static InstallFlowApplyRequest CreateApplyRequest(
        InstallFlowRequest request,
        CoreInstallPlan plan,
        InstallFlowComponentExecutionResult componentExecution)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(componentExecution);

        return new InstallFlowApplyRequest
        {
            Plan = plan,
            InstallResult = componentExecution.InstallResult,
            ComponentContext = componentExecution.Context,
            DurationMs = componentExecution.DurationMs,
            InstallPostPopupMessage = request.InstallPostPopupMessage,
            ProfileRows = request.ExecutionContext.ProfileRows,
            OptiScalerIniApplyContext = request.ExecutionContext.OptiScalerIniApplyContext,
            Text = request.Text
        };
    }
}
