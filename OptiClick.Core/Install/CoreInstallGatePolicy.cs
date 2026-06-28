using System;
using OptiClick.Core.Install.Planning;

namespace OptiClick.Core.Install;

public sealed class CoreInstallGatePolicy
{
    public IReadOnlyList<CoreInstallPlanBlockReason> ResolveBlockReasons(CoreInstallPlanBuildInput input)
    {
        if (input is null)
        {
            return Array.Empty<CoreInstallPlanBlockReason>();
        }

        var reasons = new List<CoreInstallPlanBlockReason>();
        var game = input.GameDescriptor;

        if (input.IsMultiGpuBlocked)
        {
            reasons.Add(Block(CoreInstallPlanReasonCodes.MultiGpuBlocked));
        }

        if (input.IsGpuSelectionPending)
        {
            reasons.Add(Block(CoreInstallPlanReasonCodes.GpuSelectionPending));
        }

        if (input.IsSheetLoading)
        {
            reasons.Add(Block(CoreInstallPlanReasonCodes.SheetLoading));
        }

        if (!input.IsSheetReady)
        {
            reasons.Add(Block(CoreInstallPlanReasonCodes.SheetNotReady));
        }

        if (input.IsInstallInProgress)
        {
            reasons.Add(Block(CoreInstallPlanReasonCodes.InstallInProgress));
        }

        if (input.IsAppUpdateInProgress)
        {
            reasons.Add(Block(CoreInstallPlanReasonCodes.AppUpdateInProgress));
        }

        if (game is null)
        {
            reasons.Add(Block(CoreInstallPlanReasonCodes.NoGameSelected));
        }
        else if (!ProxyDllNamePolicy.TryResolvePreferredStart(game.OptiScalerDllName, out _, out var preferredError))
        {
            reasons.Add(Block(preferredError));
        }

        if (input.Precheck.State == InstallPrecheckState.Running)
        {
            reasons.Add(Block(CoreInstallPlanReasonCodes.InstallPrecheckRunning));
        }

        if (input.Precheck.State != InstallPrecheckState.Passed || string.IsNullOrWhiteSpace(input.Precheck.ResolvedDllName))
        {
            reasons.Add(Block(CoreInstallPlanReasonCodes.PrecheckIncomplete, input.Precheck.ErrorText));
        }

        if (input.ArchiveReadiness.OptiScalerState == ArchiveReadinessState.Downloading)
        {
            reasons.Add(Block(CoreInstallPlanReasonCodes.OptiScalerArchiveDownloading));
        }

        if (input.ArchiveReadiness.OptiScalerState != ArchiveReadinessState.Ready
            || string.IsNullOrWhiteSpace(input.ArchiveReadiness.OptiScalerSourceArchive))
        {
            reasons.Add(Block(CoreInstallPlanReasonCodes.OptiScalerArchiveNotReady));
        }

        if (IsUnsupportedGpu(input))
        {
            reasons.Add(Block(CoreInstallPlanReasonCodes.UnsupportedGpu));
        }

        if (!input.IsSelectionPopupConfirmed)
        {
            reasons.Add(Block(CoreInstallPlanReasonCodes.ConfirmPopupRequired));
        }

        if (input.IsPredownloadInProgress)
        {
            reasons.Add(Block(CoreInstallPlanReasonCodes.PredownloadInProgress));
        }

        return reasons;
    }

    private static bool IsUnsupportedGpu(CoreInstallPlanBuildInput input)
    {
        if (string.Equals(input.ActionAvailability.ReasonCode, CoreInstallPlanReasonCodes.UnsupportedGpu, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (input.MatchSnapshot?.IsUnsupportedGpu == true)
        {
            return true;
        }

        return false;
    }

    private static CoreInstallPlanBlockReason Block(string code, string detail = "")
    {
        return new CoreInstallPlanBlockReason
        {
            Code = code,
            Detail = (detail ?? "").Trim()
        };
    }
}
