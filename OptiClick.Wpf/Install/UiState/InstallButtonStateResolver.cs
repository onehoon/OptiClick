namespace OptiClick.Wpf.Install.UiState;

public interface IInstallButtonStateResolver
{
    InstallButtonState Compute(InstallButtonStateInputs inputs);
}

public sealed class InstallButtonStateResolver : IInstallButtonStateResolver
{
    public InstallButtonState Compute(InstallButtonStateInputs inputs)
    {
        var reasonCode = ResolveReasonCode(inputs);
        return new InstallButtonState
        {
            Enabled = string.IsNullOrWhiteSpace(reasonCode),
            ShowInstalling = inputs.InstallInProgress,
            ReasonCode = reasonCode
        };
    }

    private static string ResolveReasonCode(InstallButtonStateInputs inputs)
    {
        if (inputs.MultiGpuBlocked) return InstallButtonReasonCodes.MultiGpuBlocked;
        if (inputs.GpuSelectionPending) return InstallButtonReasonCodes.GpuSelectionPending;
        if (inputs.SheetLoading) return InstallButtonReasonCodes.SheetLoading;
        if (!inputs.SheetReady) return InstallButtonReasonCodes.SheetNotReady;
        if (inputs.InstallInProgress) return InstallButtonReasonCodes.InstallInProgress;
        if (inputs.AppUpdateInProgress) return InstallButtonReasonCodes.AppUpdateInProgress;
        if (!inputs.HasValidGame) return InstallButtonReasonCodes.NoGameSelected;
        if (inputs.InstallPrecheckRunning) return InstallButtonReasonCodes.InstallPrecheckRunning;
        if (!inputs.InstallPrecheckOk) return InstallButtonReasonCodes.PrecheckIncomplete;
        if (inputs.OptiScalerArchiveDownloading) return InstallButtonReasonCodes.OptiScalerArchiveDownloading;
        if (!inputs.OptiScalerArchiveReady) return InstallButtonReasonCodes.OptiScalerArchiveNotReady;
        if (!inputs.AllArchivesReady) return InstallButtonReasonCodes.AllArchivesNotReady;
        if (!inputs.HasSupportedGpu) return InstallButtonReasonCodes.UnsupportedGpu;
        if (!inputs.GamePopupConfirmed) return InstallButtonReasonCodes.ConfirmPopupRequired;
        return "";
    }
}

public interface IInstallEntryGateResolver
{
    InstallEntryGateDecision Validate(InstallEntryGateInputs inputs);
}

public sealed class InstallEntryGateResolver : IInstallEntryGateResolver
{
    public InstallEntryGateDecision Validate(InstallEntryGateInputs inputs)
    {
        if (inputs.MultiGpuBlocked) return Reject(InstallEntryRejectionCodes.MultiGpuBlocked);
        if (inputs.InstallInProgress) return Reject(InstallEntryRejectionCodes.InstallInProgress);
        if (inputs.PredownloadInProgress) return Reject(InstallEntryRejectionCodes.PredownloadInProgress);
        if (!inputs.SelectedGameIndex.HasValue) return Reject(InstallEntryRejectionCodes.NoGameSelected);
        if (inputs.OptiScalerArchiveDownloading) return Reject(InstallEntryRejectionCodes.OptiScalerArchiveDownloading);
        if (inputs.InstallPrecheckRunning) return Reject(InstallEntryRejectionCodes.InstallPrecheckRunning);
        if (!inputs.InstallPrecheckOk || string.IsNullOrWhiteSpace(inputs.InstallPrecheckDllName))
        {
            return Reject(InstallEntryRejectionCodes.PrecheckIncomplete, inputs.InstallPrecheckError);
        }

        if (!inputs.OptiScalerArchiveReady || string.IsNullOrWhiteSpace(inputs.OptiSourceArchive))
        {
            return Reject(InstallEntryRejectionCodes.OptiScalerArchiveNotReady, inputs.OptiScalerArchiveError);
        }

        var selectedIndex = inputs.SelectedGameIndex.Value;
        if (selectedIndex < 0 || selectedIndex >= inputs.FoundGamesCount)
        {
            return Reject(InstallEntryRejectionCodes.InvalidGameSelection);
        }

        if (!inputs.GamePopupConfirmed)
        {
            return Reject(InstallEntryRejectionCodes.ConfirmPopupRequired);
        }

        return new InstallEntryGateDecision { Ok = true };
    }

    private static InstallEntryGateDecision Reject(string code, string detail = "")
    {
        return new InstallEntryGateDecision
        {
            Ok = false,
            Code = code,
            Detail = (detail ?? "").Trim()
        };
    }
}
