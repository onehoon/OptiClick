using OptiClick.Wpf.Install.Planning;
using OptiClick.Wpf.Shell.Games.Actions;

namespace OptiClick.Wpf.Install.UiState;

public interface IInstallUiStateInputBuilder
{
    InstallButtonStateInputs BuildInstallButtonStateInputs(InstallUiStateBuildInput input);
}

public sealed class InstallUiStateInputBuilder : IInstallUiStateInputBuilder
{
    public InstallButtonStateInputs BuildInstallButtonStateInputs(InstallUiStateBuildInput input)
    {
        var hasSupportedGpu = !string.Equals(
            (input.ActionAvailabilityReasonCode ?? "").Trim(),
            ShellGameActionReasonCodes.UnsupportedGpu,
            StringComparison.OrdinalIgnoreCase);

        var precheckRunning = input.Precheck.State == InstallPrecheckState.Running;
        var precheckOk = input.Precheck.State == InstallPrecheckState.Passed
            && !string.IsNullOrWhiteSpace(input.Precheck.ResolvedDllName);

        var optiReady = input.ArchiveReadiness.OptiScalerState == ArchiveReadinessState.Ready;
        var optiDownloading = input.ArchiveReadiness.OptiScalerState == ArchiveReadinessState.Downloading;
        var allArchivesReady = input.ArchiveReadiness.AreAllStartupArchivesReady();

        return new InstallButtonStateInputs
        {
            MultiGpuBlocked = input.MultiGpuBlocked,
            GpuSelectionPending = input.GpuSelectionPending,
            SheetReady = input.SheetReady,
            SheetLoading = input.SheetLoading,
            InstallInProgress = input.InstallInProgress,
            AppUpdateInProgress = input.AppUpdateInProgress,
            HasValidGame = input.HasSelectedGame,
            HasSupportedGpu = hasSupportedGpu,
            InstallPrecheckRunning = precheckRunning,
            InstallPrecheckOk = precheckOk,
            OptiScalerArchiveReady = optiReady,
            OptiScalerArchiveDownloading = optiDownloading,
            AllArchivesReady = allArchivesReady,
            GamePopupConfirmed = input.PopupConfirmed
        };
    }
}
