using OptiClick.Wpf.Install.UiState;

namespace OptiClick.Wpf.Install.Flow;

public sealed record InstallResultApplyResult
{
    public bool FinalSuccess { get; init; }
    public string StatusText { get; init; } = "";
    public PopupPresentationRequest? PopupRequest { get; init; }
    public IReadOnlyList<InstallFlowLogEntry> Logs { get; init; } = [];
}
