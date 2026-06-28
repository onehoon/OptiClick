using OptiClick.Core.Install.Planning;

namespace OptiClick.Core.Install;

public static class CoreInstallStartGateReasonCodes
{
    public const string UnsupportedOs = "unsupported_os";
    public const string MultiGpuBlocked = CoreInstallGateReasonCodes.MultiGpuBlocked;
    public const string GpuSelectionPending = CoreInstallGateReasonCodes.GpuSelectionPending;
    public const string SheetLoading = CoreInstallGateReasonCodes.SheetLoading;
    public const string SheetNotReady = CoreInstallGateReasonCodes.SheetNotReady;
    public const string InstallInProgress = CoreInstallGateReasonCodes.InstallInProgress;
    public const string AppUpdateInProgress = CoreInstallGateReasonCodes.AppUpdateInProgress;
    public const string PredownloadInProgress = CoreInstallGateReasonCodes.PredownloadInProgress;
    public const string NoGameSelected = CoreInstallGateReasonCodes.NoGameSelected;
    public const string InvalidMatch = "invalid_match";
    public const string InvalidTargetFolder = "invalid_target_folder";
    public const string InstallPrecheckRunning = CoreInstallGateReasonCodes.InstallPrecheckRunning;
    public const string PrecheckIncomplete = CoreInstallGateReasonCodes.PrecheckIncomplete;
    public const string OptiScalerArchiveDownloading = CoreInstallGateReasonCodes.OptiScalerArchiveDownloading;
    public const string OptiScalerArchiveNotReady = CoreInstallGateReasonCodes.OptiScalerArchiveNotReady;
    public const string ComponentArchiveNotReady = "component_archive_not_ready";
    public const string UnsupportedGpu = CoreInstallGateReasonCodes.UnsupportedGpu;
    public const string DisabledGame = "disabled_game";
    public const string FinalProxyMissing = "final_proxy_missing";
    public const string ProxyChainUnresolved = "proxy_chain_unresolved";
    public const string ConfirmPopupRequired = CoreInstallGateReasonCodes.ConfirmPopupRequired;
    public const string Ready = "ready";
    public const string InvalidInstallPlan = "invalid_install_plan";
}

public sealed record CoreInstallStartGateInput
{
    public bool IsWindowsSupported { get; init; } = true;
    public bool IsMultiGpuBlocked { get; init; }
    public bool IsGpuSelectionPending { get; init; }
    public bool IsSheetLoading { get; init; }
    public bool IsSheetReady { get; init; } = true;
    public bool IsInstallInProgress { get; init; }
    public bool IsAppUpdateInProgress { get; init; }
    public bool IsPredownloadInProgress { get; init; }
    public bool HasSelectedGame { get; init; }
    public bool HasValidMatch { get; init; }
    public bool HasValidTargetDirectory { get; init; }
    public ArchiveReadinessSnapshot ArchiveReadiness { get; init; } = ArchiveReadinessSnapshot.NotReady;
    public InstallPrecheckSnapshot Precheck { get; init; } = InstallPrecheckSnapshot.NotStarted;
    public IReadOnlyList<CoreInstallPlanComponentType> EnabledPlanComponents { get; init; } = [];
    public bool IsExtraBundleReady { get; init; } = true;
    public bool IsUnsupportedGpu { get; init; }
    public bool IsDisabledGame { get; init; }
    public string PlanFailureReasonCode { get; init; } = "";
    public bool IsPopupConfirmed { get; init; }
    public bool HasPendingPopupRequests { get; init; }
}

public sealed record CoreInstallStartGateDecision
{
    public bool CanStart { get; init; }
    public string ReasonCode { get; init; } = "";
    public string Stage { get; init; } = "";
}

public sealed class CoreInstallStartGatePolicy
{
    public CoreInstallStartGateDecision Resolve(CoreInstallStartGateInput? input)
    {
        if (input is null)
        {
            return Reject(CoreInstallStartGateReasonCodes.InvalidInstallPlan, "input");
        }

        if (!input.IsWindowsSupported) return Reject(CoreInstallStartGateReasonCodes.UnsupportedOs, "platform");
        if (input.IsMultiGpuBlocked) return Reject(CoreInstallStartGateReasonCodes.MultiGpuBlocked, "gpu");
        if (input.IsGpuSelectionPending) return Reject(CoreInstallStartGateReasonCodes.GpuSelectionPending, "gpu");
        if (input.IsSheetLoading) return Reject(CoreInstallStartGateReasonCodes.SheetLoading, "sheet");
        if (!input.IsSheetReady) return Reject(CoreInstallStartGateReasonCodes.SheetNotReady, "sheet");
        if (input.IsInstallInProgress) return Reject(CoreInstallStartGateReasonCodes.InstallInProgress, "runtime");
        if (input.IsAppUpdateInProgress) return Reject(CoreInstallStartGateReasonCodes.AppUpdateInProgress, "runtime");
        if (input.IsPredownloadInProgress) return Reject(CoreInstallStartGateReasonCodes.PredownloadInProgress, "runtime");
        if (!input.HasSelectedGame) return Reject(CoreInstallStartGateReasonCodes.NoGameSelected, "selection");
        if (!input.HasValidMatch) return Reject(CoreInstallStartGateReasonCodes.InvalidMatch, "selection");
        if (!input.HasValidTargetDirectory) return Reject(CoreInstallStartGateReasonCodes.InvalidTargetFolder, "target");
        if (input.Precheck.State == InstallPrecheckState.Running) return Reject(CoreInstallStartGateReasonCodes.InstallPrecheckRunning, "precheck");

        if (input.Precheck.State != InstallPrecheckState.Passed || string.IsNullOrWhiteSpace(input.Precheck.ResolvedDllName))
        {
            return Reject(CoreInstallStartGateReasonCodes.PrecheckIncomplete, "precheck");
        }

        if (input.ArchiveReadiness.OptiScalerState == ArchiveReadinessState.Downloading)
        {
            return Reject(CoreInstallStartGateReasonCodes.OptiScalerArchiveDownloading, "archive");
        }

        if (input.ArchiveReadiness.OptiScalerState != ArchiveReadinessState.Ready
            || string.IsNullOrWhiteSpace(input.ArchiveReadiness.OptiScalerSourceArchive))
        {
            return Reject(CoreInstallStartGateReasonCodes.OptiScalerArchiveNotReady, "archive");
        }

        if (HasNotReadyComponentArchive(input))
        {
            return Reject(CoreInstallStartGateReasonCodes.ComponentArchiveNotReady, "archive");
        }

        if (input.IsUnsupportedGpu) return Reject(CoreInstallStartGateReasonCodes.UnsupportedGpu, "support");
        if (input.IsDisabledGame) return Reject(CoreInstallStartGateReasonCodes.DisabledGame, "support");

        var planFailureReason = (input.PlanFailureReasonCode ?? "").Trim();
        if (!string.IsNullOrWhiteSpace(planFailureReason))
        {
            return Reject(planFailureReason, "plan");
        }

        if (input.HasPendingPopupRequests || !input.IsPopupConfirmed)
        {
            return Reject(CoreInstallStartGateReasonCodes.ConfirmPopupRequired, "popup");
        }

        return new CoreInstallStartGateDecision
        {
            CanStart = true,
            ReasonCode = CoreInstallStartGateReasonCodes.Ready,
            Stage = "ready"
        };
    }

    private static CoreInstallStartGateDecision Reject(string reasonCode, string stage)
    {
        return new CoreInstallStartGateDecision
        {
            CanStart = false,
            ReasonCode = reasonCode,
            Stage = stage
        };
    }

    private static bool HasNotReadyComponentArchive(CoreInstallStartGateInput input)
    {
        if (input.EnabledPlanComponents.Count == 0)
        {
            return false;
        }

        foreach (var component in input.EnabledPlanComponents)
        {
            switch (component)
            {
                case CoreInstallPlanComponentType.SpecialK when input.ArchiveReadiness.SpecialKState != ArchiveReadinessState.Ready:
                    return true;
                case CoreInstallPlanComponentType.REFramework when input.ArchiveReadiness.ReframeworkState != ArchiveReadinessState.Ready:
                    return true;
                case CoreInstallPlanComponentType.Unreal5 when input.ArchiveReadiness.Unreal5State != ArchiveReadinessState.Ready:
                    return true;
                case CoreInstallPlanComponentType.ExtraBundle when !input.IsExtraBundleReady:
                    return true;
            }
        }

        return false;
    }
}
