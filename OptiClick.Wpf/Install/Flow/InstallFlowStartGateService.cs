using OptiClick.Core.Install.Planning;
using OptiClick.Wpf.Install.Execution;
using OptiClick.Wpf.Install.Gates;
using OptiClick.Wpf.Shell.Games.Actions;

namespace OptiClick.Wpf.Install.Flow;

public sealed class InstallFlowStartGateService
{
    private readonly IInstallStartGateResolver _installStartGateResolver;

    public InstallFlowStartGateService(IInstallStartGateResolver installStartGateResolver)
    {
        _installStartGateResolver = installStartGateResolver
                                    ?? throw new ArgumentNullException(nameof(installStartGateResolver));
    }

    public InstallFlowStartGateResult Resolve(InstallFlowStartGateRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Selection);
        ArgumentNullException.ThrowIfNull(request.Operation);
        ArgumentNullException.ThrowIfNull(request.Readiness);

        var selection = request.Selection;
        var operation = request.Operation;
        var readiness = request.Readiness;
        var gateDecision = _installStartGateResolver.Resolve(new InstallStartGateInput
        {
            IsWindowsSupported = operation.IsWindowsSupported,
            IsMultiGpuBlocked = selection.IsMultiGpuBlocked,
            IsGpuSelectionPending = selection.IsGpuSelectionPending,
            IsSheetLoading = readiness.IsSheetLoading,
            IsSheetReady = readiness.IsSheetReady,
            IsInstallInProgress = operation.IsInstallExecutionInProgress,
            IsAppUpdateInProgress = operation.IsAppUpdateInProgress,
            IsPredownloadInProgress = false,
            HasSelectedGame = true,
            HasValidMatch = selection.HasValidMatch,
            TargetPath = request.Plan.TargetFolder,
            ArchiveReadiness = request.ArchiveReadiness,
            Precheck = request.Precheck,
            ShouldInstallFsr4 = request.ShouldInstallFsr4,
            IsFsr4Ready = request.ArchiveReadiness.Fsr4State == ArchiveReadinessState.Ready,
            IsExtraBundleReady = request.ModuleDownloadLinks.IsExtraBundleReady(
                ResolveExtraBundleAlias(request.Plan)),
            IsUnsupportedGpu = string.Equals(
                selection.ActionAvailabilityReasonCode,
                ShellGameActionReasonCodes.UnsupportedGpu,
                StringComparison.OrdinalIgnoreCase),
            IsDisabledGame = !operation.IsEnabled,
            IsPopupConfirmed = selection.IsPopupConfirmed,
            HasPendingPopupRequests = selection.HasPendingPopupRequests,
            InstallPlan = request.Plan,
            ComponentReview = request.ComponentReview,
            RequireWritePermissionProbe = true
        });

        return new InstallFlowStartGateResult
        {
            GateDecision = gateDecision
        };
    }

    private static string ResolveExtraBundleAlias(CoreInstallPlan plan)
    {
        var extraBundle = plan.Components.FirstOrDefault(static component =>
            component.Enabled
            && component.Type == CoreInstallPlanComponentType.ExtraBundle);
        return (extraBundle?.RequiredArchiveAlias ?? "").Trim();
    }
}

public sealed record InstallFlowStartGateRequest
{
    public required CoreInstallPlan Plan { get; init; }
    public required ModuleDownloadLinkContext ModuleDownloadLinks { get; init; }
    public required ArchiveReadinessSnapshot ArchiveReadiness { get; init; }
    public required InstallPrecheckSnapshot Precheck { get; init; }
    public required InstallFlowReadinessSnapshot Readiness { get; init; }
    public required InstallFlowSelectionGateSnapshot Selection { get; init; }
    public required InstallFlowOperationGateSnapshot Operation { get; init; }
    public required bool ShouldInstallFsr4 { get; init; }
    public required ComponentInstallParityReviewResult ComponentReview { get; init; }
}

public sealed record InstallFlowStartGateResult
{
    public required InstallStartGateDecision GateDecision { get; init; }
}
