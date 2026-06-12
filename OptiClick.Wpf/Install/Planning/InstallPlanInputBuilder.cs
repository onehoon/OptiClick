using OptiClick.Core.Install;
using OptiClick.Core.Install.Planning;

namespace OptiClick.Wpf.Install.Planning;

public sealed class InstallPlanInputBuilder
{
    public CoreInstallPlanBuildInput Build(InstallPlanInputBuildContext context)
    {
        var executionDescriptor = context.ExecutionDescriptor;
        var normalizedTargetPath = InstallTargetPathNormalizer.NormalizeTargetDirectory(context.TargetFolderHint);
        var matchedExeHint = string.IsNullOrWhiteSpace(context.MatchedExeHint)
            ? executionDescriptor.MatchExe
            : context.MatchedExeHint;

        return new CoreInstallPlanBuildInput
        {
            GameDescriptor = executionDescriptor.GameDescriptor,
            MatchSnapshot = context.MatchSnapshot,
            ActionAvailability = context.ActionAvailabilitySnapshot,
            ArchiveReadiness = context.LatestArchiveReadiness,
            Precheck = context.Precheck,
            TargetFolderHint = normalizedTargetPath,
            MatchedExeHint = matchedExeHint,
            IsInstallInProgress = context.IsInstallExecutionInProgress,
            // Predownload is currently represented by archive readiness.
            // Keep false unless a separate predownload gate is reintroduced.
            IsPredownloadInProgress = false,
            IsMultiGpuBlocked = context.IsMultiGpuBlocked,
            IsAppUpdateInProgress = context.IsAppUpdateInProgress,
            IsSelectionPopupConfirmed = context.IsSelectionPopupConfirmed,
            IsGpuSelectionPending = context.IsGpuSelectionPending,
            IsSheetLoading = context.IsSheetLoading,
            IsSheetReady = context.IsSheetReady,
            ShouldInstallFsr4 = executionDescriptor.ShouldInstallFsr4,
            Fsr4Variant = executionDescriptor.Fsr4Variant
        };
    }
}
