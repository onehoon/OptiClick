using OptiClick.Core.Runtime;
using OptiClick.Wpf.Install.Planning;
using OptiClick.Wpf.Shell.Scan;
using OptiClick.Wpf.Shell.Games;

namespace OptiClick.Wpf.Shell.Selection;

public sealed record GameSelectionFlowRequest
{
    public required ShellGameCardModel SelectedCard { get; init; }
    public required int SelectedIndex { get; init; }
    public required IReadOnlyList<ShellGameCardModel> Games { get; init; }
    public required ShellInstallSelectionState PreviousSelectionState { get; init; }
    public required IReadOnlyDictionary<string, ShellGameMatchResult> MatchByGameId { get; init; }
    public required IReadOnlyDictionary<string, string> TargetPathByGameId { get; init; }
    public required ModuleDownloadLinkContext ModuleDownloadLinks { get; init; }
    public required ArchiveReadinessSnapshot LatestArchiveReadiness { get; init; }
    public required AppLanguage SelectedLanguage { get; init; }
    public required bool IsInstallExecutionInProgress { get; init; }
    public required bool IsAppUpdateInProgress { get; init; }
    public bool MultiGpuBlocked { get; init; }
    public bool GpuSelectionPending { get; init; }
    public string LatestRemoteCatalogErrorCode { get; init; } = "";
}
