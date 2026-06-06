using OptiClick.Wpf.Install.Precheck;
using OptiClick.Wpf.Install.UiState;
using OptiClick.Wpf.Shell.Games;
using OptiClick.Wpf.Shell.Games.Actions;
using OptiClick.Wpf.Shell.Scan;

namespace OptiClick.Wpf.Shell.Selection;

public enum ShellPopupRequestKind
{
    PrecheckFailure,
    ModNotice,
    SelectionNotice,
    InstallRejection
}

public sealed record ShellPopupRequest
{
    public ShellPopupRequestKind Kind { get; init; }
    public string Message { get; init; } = "";
    public IReadOnlyList<string> BulletItems { get; init; } = Array.Empty<string>();
    public string FooterText { get; init; } = "";
}

public sealed class ShellPopupRequestQueue
{
    private readonly Queue<ShellPopupRequest> _queue;

    public ShellPopupRequestQueue(IEnumerable<ShellPopupRequest> requests, bool confirmedImmediately, bool precheckOk)
    {
        _queue = new Queue<ShellPopupRequest>(requests ?? Array.Empty<ShellPopupRequest>());
        PrecheckOk = precheckOk;
        PopupConfirmed = confirmedImmediately || (_queue.Count == 0 && precheckOk);
    }

    public bool PopupConfirmed { get; private set; }
    public bool PrecheckOk { get; }

    public IReadOnlyList<ShellPopupRequest> PendingRequests => _queue.ToArray();

    public ShellPopupRequest? ConfirmNext()
    {
        if (_queue.Count == 0)
        {
            if (PrecheckOk)
            {
                PopupConfirmed = true;
            }

            return null;
        }

        var request = _queue.Dequeue();
        if (_queue.Count == 0 && PrecheckOk)
        {
            PopupConfirmed = true;
        }

        return request;
    }
}

public sealed record ShellInstallSelectionRequest
{
    public int SelectedIndex { get; init; } = -1;
    public IReadOnlyList<ShellGameCardModel> Games { get; init; } = Array.Empty<ShellGameCardModel>();
    public ShellGameMatchResult? SelectedMatchResult { get; init; }
    public ShellInstallSelectionState? PreviousState { get; init; }
    public IReadOnlyDictionary<string, string> TargetPathsByGameId { get; init; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    public string Language { get; init; } = "en";
    public string SelectionPopupMessage { get; init; } = "";
    public string PrecheckPopupMessage { get; init; } = "";
    public string InstallPostPopupMessage { get; init; } = "";
    public string SelectedInstallStatusCode { get; init; } = InstallStatusCodes.Installable;
    public InstallStatusSnapshot SelectedInstallStatus { get; init; } = new();

    public bool MultiGpuBlocked { get; init; }
    public bool GpuSelectionPending { get; init; }
    public bool SheetReady { get; init; } = true;
    public bool SheetLoading { get; init; }
    public bool InstallInProgress { get; init; }
    public bool AppUpdateInProgress { get; init; }
    public bool Fsr4Required { get; init; }

    public Install.Planning.InstallPrecheckSnapshot PrecheckSnapshotFallback { get; init; } = Install.Planning.InstallPrecheckSnapshot.NotStarted;
    public Install.Planning.ArchiveReadinessSnapshot ArchiveReadiness { get; init; } = Install.Planning.ArchiveReadinessSnapshot.NotReady;
}

public sealed record ShellInstallSelectionState
{
    public long RequestId { get; init; }
    public int? SelectedIndex { get; init; }
    public string SelectedGameId { get; init; } = "";
    public ShellGameCardModel? SelectedGame { get; init; }
    public ShellGameMatchResult? SelectedMatchResult { get; init; }

    public bool PopupConfirmed { get; init; }
    public bool PrecheckRunning { get; init; }
    public bool PrecheckWasRunning { get; init; }
    public bool PrecheckOk { get; init; }
    public string PrecheckError { get; init; } = "";
    public string PrecheckResolvedDllName { get; init; } = "";
    public Install.Planning.InstallPrecheckSnapshot PrecheckSnapshot { get; init; } = Install.Planning.InstallPrecheckSnapshot.NotStarted;
    public Install.Planning.ArchiveReadinessSnapshot ArchiveReadiness { get; init; } = Install.Planning.ArchiveReadinessSnapshot.NotReady;
    public string ActionAvailabilityReasonCode { get; init; } = "";
    public string SelectedInstallStatusCode { get; init; } = InstallStatusCodes.Installable;
    public string Language { get; init; } = "en";
    public string InstallPostPopupMessage { get; init; } = "";
    public InstallStatusSnapshot SelectedInstallStatus { get; init; } = new();
    public bool MultiGpuBlocked { get; init; }
    public bool GpuSelectionPending { get; init; }
    public bool SheetReady { get; init; } = true;
    public bool SheetLoading { get; init; }
    public bool InstallInProgress { get; init; }
    public bool AppUpdateInProgress { get; init; }
    public bool Fsr4Required { get; init; }

    public ShellPopupRequestQueue PopupQueue { get; init; } = new(Array.Empty<ShellPopupRequest>(), confirmedImmediately: false, precheckOk: false);
    public IReadOnlyList<ShellPopupRequest> PendingPopupRequests { get; init; } = Array.Empty<ShellPopupRequest>();

    public ShellGameActionAvailability ActionAvailability { get; init; } = new();
    public InstallSummaryPresentation InstallSummary { get; init; } = new();
    public InstallButtonPresentation InstallButtonPresentation { get; init; } = new();
    public InstallButtonPresentation RunningInstallButtonPresentation { get; init; } = new();
}

public sealed record ShellInstallSelectionResult
{
    public bool IsSuccess { get; init; }
    public bool IsStaleIgnored { get; init; }
    public ShellInstallSelectionState State { get; init; } = new();
    public string ErrorMessage { get; init; } = "";
}
