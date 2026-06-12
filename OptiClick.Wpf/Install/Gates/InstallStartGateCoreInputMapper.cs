using OptiClick.Core.Install;
using OptiClick.Core.Install.Planning;

namespace OptiClick.Wpf.Install.Gates;

internal static class InstallStartGateCoreInputMapper
{
    public static CoreInstallStartGateInput Map(
        InstallStartGateInput input,
        bool hasValidTargetDirectory)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new CoreInstallStartGateInput
        {
            IsWindowsSupported = input.IsWindowsSupported,
            IsMultiGpuBlocked = input.IsMultiGpuBlocked,
            IsGpuSelectionPending = input.IsGpuSelectionPending,
            IsSheetLoading = input.IsSheetLoading,
            IsSheetReady = input.IsSheetReady,
            IsInstallInProgress = input.IsInstallInProgress,
            IsAppUpdateInProgress = input.IsAppUpdateInProgress,
            IsPredownloadInProgress = input.IsPredownloadInProgress,
            HasSelectedGame = input.HasSelectedGame,
            HasValidMatch = input.HasValidMatch,
            HasValidTargetDirectory = hasValidTargetDirectory,
            ArchiveReadiness = input.ArchiveReadiness ?? ArchiveReadinessSnapshot.NotReady,
            Precheck = input.Precheck ?? InstallPrecheckSnapshot.NotStarted,
            EnabledPlanComponents = ToCoreEnabledPlanComponents(input.InstallPlan),
            IsExtraBundleReady = input.IsExtraBundleReady,
            ShouldInstallFsr4 = input.ShouldInstallFsr4,
            IsFsr4Ready = input.IsFsr4Ready,
            IsUnsupportedGpu = input.IsUnsupportedGpu,
            IsDisabledGame = input.IsDisabledGame,
            PlanFailureReasonCode = InstallStartGatePlanFailureResolver.Resolve(input),
            IsPopupConfirmed = input.IsPopupConfirmed,
            HasPendingPopupRequests = input.HasPendingPopupRequests
        };
    }

    private static IReadOnlyList<CoreInstallPlanComponentType> ToCoreEnabledPlanComponents(CoreInstallPlan? plan)
    {
        if (plan is null || plan.Components.Count == 0)
        {
            return Array.Empty<CoreInstallPlanComponentType>();
        }

        return plan.Components
            .Where(static component => component.Enabled)
            .Select(static component => component.Type)
            .ToArray();
    }
}
