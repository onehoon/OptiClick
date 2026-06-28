using OptiClick.Core.Install;
using OptiClick.Core.Install.Flow;
using OptiClick.Core.Install.Planning;
using OptiClick.Wpf.Install.Execution;
using OptiClick.Wpf.Install.Planning;
using OptiClick.Wpf.Shell.RuntimeData;

namespace OptiClick.Wpf.Install.Flow;

public sealed class InstallFlowPlanPreparationService
{
    private readonly IInstallPlanBuilder _installPlanBuilder;
    private readonly IComponentInstallParityReviewBuilder _componentInstallParityReviewBuilder;
    private readonly InstallPlanInputBuilder _installPlanInputBuilder;

    public InstallFlowPlanPreparationService(
        IInstallPlanBuilder installPlanBuilder,
        IComponentInstallParityReviewBuilder componentInstallParityReviewBuilder,
        InstallPlanInputBuilder installPlanInputBuilder)
    {
        _installPlanBuilder = installPlanBuilder ?? throw new ArgumentNullException(nameof(installPlanBuilder));
        _componentInstallParityReviewBuilder = componentInstallParityReviewBuilder
                                               ?? throw new ArgumentNullException(nameof(componentInstallParityReviewBuilder));
        _installPlanInputBuilder = installPlanInputBuilder ?? throw new ArgumentNullException(nameof(installPlanInputBuilder));
    }

    public InstallFlowPlanPreparationResult Prepare(InstallFlowPlanPreparationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var planBuildInput = _installPlanInputBuilder.Build(new InstallPlanInputBuildContext
        {
            ExecutionDescriptor = request.ExecutionDescriptor,
            ProfileRows = request.ProfileRows,
            ActionAvailabilitySnapshot = request.ActionAvailabilitySnapshot,
            Precheck = request.Precheck,
            IsMultiGpuBlocked = request.IsMultiGpuBlocked,
            IsSelectionPopupConfirmed = request.IsSelectionPopupConfirmed,
            IsGpuSelectionPending = request.IsGpuSelectionPending,
            LatestArchiveReadiness = request.ArchiveReadiness,
            MatchSnapshot = request.MatchSnapshot,
            TargetFolderHint = request.MatchSnapshot?.FolderPath ?? "",
            MatchedExeHint = request.ExecutionDescriptor.MatchExe,
            IsSheetLoading = request.Readiness.IsSheetLoading,
            IsSheetReady = request.Readiness.IsSheetReady,
            IsInstallExecutionInProgress = request.IsInstallExecutionInProgress,
            IsAppUpdateInProgress = request.IsAppUpdateInProgress
        });
        var planBuildResult = _installPlanBuilder.Build(planBuildInput);
        var plan = planBuildResult.Plan;
        var componentReview = _componentInstallParityReviewBuilder.Build(new ComponentInstallParityReviewInput
        {
            Plan = plan,
            ProfileRows = request.ProfileRows,
            ArchiveReadiness = request.ArchiveReadiness,
            Precheck = request.Precheck
        });

        return new InstallFlowPlanPreparationResult
        {
            Plan = plan,
            ComponentReview = componentReview
        };
    }
}

public sealed record InstallFlowPlanPreparationRequest
{
    public required InstallExecutionDescriptor ExecutionDescriptor { get; init; }
    public required AttachedRuntimeProfileRows ProfileRows { get; init; }
    public required InstallActionAvailabilitySnapshot ActionAvailabilitySnapshot { get; init; }
    public required InstallPrecheckSnapshot Precheck { get; init; }
    public required bool IsMultiGpuBlocked { get; init; }
    public required bool IsSelectionPopupConfirmed { get; init; }
    public required bool IsGpuSelectionPending { get; init; }
    public required ArchiveReadinessSnapshot ArchiveReadiness { get; init; }
    public InstallGameMatchSnapshot? MatchSnapshot { get; init; }
    public required InstallFlowReadinessSnapshot Readiness { get; init; }
    public required bool IsInstallExecutionInProgress { get; init; }
    public required bool IsAppUpdateInProgress { get; init; }
}

public sealed record InstallFlowPlanPreparationResult
{
    public required CoreInstallPlan Plan { get; init; }
    public required ComponentInstallParityReviewResult ComponentReview { get; init; }
}
