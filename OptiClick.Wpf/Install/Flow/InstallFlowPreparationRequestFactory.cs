using OptiClick.Core.Install.Planning;

namespace OptiClick.Wpf.Install.Flow;

internal static class InstallFlowPreparationRequestFactory
{
    public static InstallFlowPlanPreparationRequest CreatePlanPreparationRequest(
        InstallFlowRequest request,
        InstallFlowReadinessSnapshot readiness)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(readiness);

        var context = request.ExecutionContext;
        var selection = context.SelectionSnapshot;
        return new InstallFlowPlanPreparationRequest
        {
            ExecutionDescriptor = context.ExecutionDescriptor,
            ProfileRows = context.ProfileRows,
            ActionAvailabilitySnapshot = selection.ActionAvailabilitySnapshot,
            Precheck = selection.Precheck,
            IsMultiGpuBlocked = selection.MultiGpuBlocked,
            IsSelectionPopupConfirmed = selection.PopupConfirmed,
            IsGpuSelectionPending = selection.GpuSelectionPending,
            ArchiveReadiness = context.LatestArchiveReadiness,
            MatchSnapshot = selection.MatchSnapshot,
            Readiness = readiness,
            IsInstallExecutionInProgress = context.IsInstallExecutionInProgress,
            IsAppUpdateInProgress = context.IsAppUpdateInProgress
        };
    }

    public static InstallFlowStartGateRequest CreateStartGateRequest(
        InstallFlowRequest request,
        InstallFlowReadinessSnapshot readiness,
        CoreInstallPlan plan,
        InstallFlowPlanPreparationResult planPreparation)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(readiness);
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(planPreparation);

        var context = request.ExecutionContext;
        var selection = context.SelectionSnapshot;
        return new InstallFlowStartGateRequest
        {
            Plan = plan,
            ModuleDownloadLinks = context.ModuleDownloadLinks,
            ArchiveReadiness = context.LatestArchiveReadiness,
            Precheck = selection.Precheck,
            Readiness = readiness,
            Selection = InstallFlowSelectionGateSnapshot.Create(selection),
            Operation = InstallFlowOperationGateSnapshot.Create(context),
            ShouldInstallFsr4 = planPreparation.ShouldInstallFsr4,
            ComponentReview = planPreparation.ComponentReview
        };
    }

}
