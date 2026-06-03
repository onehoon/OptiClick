using System.IO;
using OptiClick.Wpf.Install.Execution;
using OptiClick.Wpf.Install.Planning;
using OptiClick.Wpf.Install.UiState;

namespace OptiClick.Wpf.Install.Gates;

public sealed class InstallStartGateResolver : IInstallStartGateResolver
{
    private static readonly HashSet<string> BlockingComponentReviewCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        ComponentInstallParityReviewCodes.FinalProxyMissing,
        ComponentInstallParityReviewCodes.ProxyChainUnresolved
    };

    private readonly IWritePermissionProbe _writePermissionProbe;

    public InstallStartGateResolver(IWritePermissionProbe writePermissionProbe)
    {
        _writePermissionProbe = writePermissionProbe;
    }

    public InstallStartGateDecision Resolve(InstallStartGateInput input)
    {
        if (input is null)
        {
            return Reject(InstallStartGateReasonCodes.InvalidInstallPlan, "input");
        }

        if (!input.IsWindowsSupported) return Reject(InstallStartGateReasonCodes.UnsupportedOs, "platform");
        if (input.IsMultiGpuBlocked) return Reject(InstallStartGateReasonCodes.MultiGpuBlocked, "gpu");
        if (input.IsGpuSelectionPending) return Reject(InstallStartGateReasonCodes.GpuSelectionPending, "gpu");
        if (input.IsSheetLoading) return Reject(InstallStartGateReasonCodes.SheetLoading, "sheet");
        if (!input.IsSheetReady) return Reject(InstallStartGateReasonCodes.SheetNotReady, "sheet");
        if (input.IsInstallInProgress) return Reject(InstallStartGateReasonCodes.InstallInProgress, "runtime");
        if (input.IsAppUpdateInProgress) return Reject(InstallStartGateReasonCodes.AppUpdateInProgress, "runtime");
        if (input.IsPredownloadInProgress) return Reject(InstallStartGateReasonCodes.PredownloadInProgress, "runtime");
        if (!input.HasSelectedGame) return Reject(InstallStartGateReasonCodes.NoGameSelected, "selection");
        if (!input.HasValidMatch) return Reject(InstallStartGateReasonCodes.InvalidMatch, "selection");
        if (!IsValidTargetDirectory(input.TargetPath)) return Reject(InstallStartGateReasonCodes.InvalidTargetFolder, "target");
        if (input.Precheck.State == InstallPrecheckState.Running) return Reject(InstallStartGateReasonCodes.InstallPrecheckRunning, "precheck");

        if (input.Precheck.State != InstallPrecheckState.Passed || string.IsNullOrWhiteSpace(input.Precheck.ResolvedDllName))
        {
            return Reject(InstallStartGateReasonCodes.PrecheckIncomplete, "precheck");
        }

        if (input.ArchiveReadiness.OptiScalerState == ArchiveReadinessState.Downloading)
        {
            return Reject(InstallStartGateReasonCodes.OptiScalerArchiveDownloading, "archive");
        }

        if (input.ArchiveReadiness.OptiScalerState != ArchiveReadinessState.Ready
            || string.IsNullOrWhiteSpace(input.ArchiveReadiness.OptiScalerSourceArchive))
        {
            return Reject(InstallStartGateReasonCodes.OptiScalerArchiveNotReady, "archive");
        }

        if (HasComponentArchiveNotReady(input))
        {
            return Reject(InstallStartGateReasonCodes.ComponentArchiveNotReady, "archive");
        }

        if (input.RequiresFsr4 && !input.IsFsr4Ready)
        {
            return Reject(InstallStartGateReasonCodes.Fsr4NotReady, "archive");
        }

        if (input.IsUnsupportedGpu) return Reject(InstallStartGateReasonCodes.UnsupportedGpu, "support");
        if (input.IsDisabledGame) return Reject(InstallStartGateReasonCodes.DisabledGame, "support");

        if (input.InstallPlan is not null && string.IsNullOrWhiteSpace(input.InstallPlan.FinalProxyDllName))
        {
            return Reject(InstallStartGateReasonCodes.FinalProxyMissing, "plan");
        }

        if (input.InstallPlan is not null && ProxyDllNameResolver.BuildCandidateChainForPreferred(input.InstallPlan.FinalProxyDllName).Count == 0)
        {
            return Reject(InstallStartGateReasonCodes.ProxyChainUnresolved, "plan");
        }

        if (!IsValidInstallPlan(input.InstallPlan, input.ComponentReview))
        {
            return Reject(InstallStartGateReasonCodes.InvalidInstallPlan, "plan");
        }

        if (input.HasPendingPopupRequests || !input.IsPopupConfirmed)
        {
            return Reject(InstallStartGateReasonCodes.ConfirmPopupRequired, "popup");
        }

        if (input.RequireWritePermissionProbe)
        {
            var probeResult = _writePermissionProbe.Probe(input.TargetPath);
            if (!probeResult.IsSuccess)
            {
                return Reject(InstallStartGateReasonCodes.WritePermissionDenied, "permission");
            }
        }

        return new InstallStartGateDecision
        {
            CanStart = true,
            ReasonCode = InstallStartGateReasonCodes.Ready,
            Stage = "ready",
            RequiresPopup = false,
            PopupRequest = new PopupPresentationRequest
            {
                Kind = PopupPresentationKind.None,
                ReasonCode = InstallStartGateReasonCodes.Ready
            }
        };
    }

    private static bool HasComponentArchiveNotReady(InstallStartGateInput input)
    {
        var plan = input.InstallPlan;
        if (plan is null || plan.Components.Count == 0)
        {
            return false;
        }

        foreach (var component in plan.Components)
        {
            if (!component.Enabled)
            {
                continue;
            }

            switch (component.Type)
            {
                case InstallPlanComponentType.OptiPatcher when input.ArchiveReadiness.OptiPatcherState != ArchiveReadinessState.Ready:
                    return true;
                case InstallPlanComponentType.SpecialK when input.ArchiveReadiness.SpecialKState != ArchiveReadinessState.Ready:
                    return true;
                case InstallPlanComponentType.REFramework when input.ArchiveReadiness.ReframeworkState != ArchiveReadinessState.Ready:
                    return true;
                case InstallPlanComponentType.UltimateAsiLoader when input.ArchiveReadiness.UalState != ArchiveReadinessState.Ready:
                    return true;
                case InstallPlanComponentType.Unreal5 when input.ArchiveReadiness.Unreal5State != ArchiveReadinessState.Ready:
                    return true;
                case InstallPlanComponentType.Fsr4 when input.ArchiveReadiness.Fsr4State != ArchiveReadinessState.Ready:
                    return true;
                case InstallPlanComponentType.ExtraBundle when !input.IsExtraBundleReady:
                    return true;
            }
        }

        return false;
    }

    private static bool IsValidInstallPlan(InstallPlan? plan, ComponentInstallParityReviewResult? componentReview)
    {
        if (plan is null)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(plan.TargetFolder))
        {
            return false;
        }

        if (!ProxyDllNameResolver.TryResolveProfilePreferredStart(plan.FinalProxyDllName, out _, out _))
        {
            return false;
        }

        if (componentReview is null)
        {
            return true;
        }

        if (!componentReview.IsSuccess)
        {
            return false;
        }

        return !componentReview.Events.Any(static e => BlockingComponentReviewCodes.Contains(e.Code ?? ""));
    }

    private static bool IsValidTargetDirectory(string targetPath)
    {
        var normalizedTargetPath = InstallTargetPathNormalizer.NormalizeTargetDirectory(targetPath);
        if (string.IsNullOrWhiteSpace(normalizedTargetPath))
        {
            return false;
        }

        if (File.Exists(normalizedTargetPath))
        {
            return false;
        }

        return Directory.Exists(normalizedTargetPath);
    }

    private static InstallStartGateDecision Reject(string reasonCode, string stage)
    {
        return new InstallStartGateDecision
        {
            CanStart = false,
            ReasonCode = reasonCode,
            Stage = stage,
            RequiresPopup = true,
            PopupRequest = new PopupPresentationRequest
            {
                Kind = PopupPresentationKind.Warning,
                ReasonCode = reasonCode
            }
        };
    }
}
