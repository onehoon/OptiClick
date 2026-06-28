using OptiClick.Core.Install.Planning;
using OptiClick.Wpf.Install.Execution;
using OptiClick.Wpf.Install.UiState;

namespace OptiClick.Wpf.Install.Gates;

public sealed record InstallStartGateInput
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
    public string TargetPath { get; init; } = "";

    public ArchiveReadinessSnapshot ArchiveReadiness { get; init; } = ArchiveReadinessSnapshot.NotReady;
    public InstallPrecheckSnapshot Precheck { get; init; } = InstallPrecheckSnapshot.NotStarted;
    public bool IsExtraBundleReady { get; init; } = true;

    public bool IsUnsupportedGpu { get; init; }
    public bool IsDisabledGame { get; init; }
    public bool IsPopupConfirmed { get; init; }
    public bool HasPendingPopupRequests { get; init; }

    public CoreInstallPlan? InstallPlan { get; init; }
    public ComponentInstallParityReviewResult? ComponentReview { get; init; }

    public bool RequireWritePermissionProbe { get; init; } = true;
}

public sealed record InstallStartGateDecision
{
    public bool CanStart { get; init; }
    public string ReasonCode { get; init; } = "";
    public string Stage { get; init; } = "";
    public bool RequiresPopup { get; init; }
    public PopupPresentationRequest PopupRequest { get; init; } = new();
}
