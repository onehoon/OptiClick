namespace OptiClick.Core.Models;

public sealed record InstallGateResult
{
    public bool UiBlocked { get; init; }
    public bool WorkerBlocked { get; init; }
    public string BlockingReason { get; init; } = "";
    public bool RequiresPopupConfirmation { get; init; }
    public IReadOnlyList<string> UiReasons { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> WorkerReasons { get; init; } = Array.Empty<string>();
}
