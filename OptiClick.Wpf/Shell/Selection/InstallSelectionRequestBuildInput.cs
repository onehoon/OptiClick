using OptiClick.Wpf.Install.Planning;
using OptiClick.Wpf.Shell.Scan;
using OptiClick.Core.Runtime;
using OptiClick.Wpf.Shell.Games;

namespace OptiClick.Wpf.Shell.Selection;

public sealed record InstallSelectionRequestBuildInput
{
    public int SelectedIndex { get; init; } = -1;
    public ShellGameCardModel SelectedCard { get; init; } = null!;
    public IReadOnlyList<ShellGameCardModel> Cards { get; init; } = Array.Empty<ShellGameCardModel>();
    public IReadOnlyDictionary<string, ShellGameMatchResult> MatchByGameId { get; init; } = new Dictionary<string, ShellGameMatchResult>(StringComparer.OrdinalIgnoreCase);
    public IReadOnlyDictionary<string, string> TargetPathByGameId { get; init; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    public ShellInstallSelectionState PreviousState { get; init; } = new();
    public ModuleDownloadLinkContext ModuleDownloadLinks { get; init; } =
        ModuleDownloadLinkContext.Empty;
    public ArchiveReadinessSnapshot LatestArchiveReadiness { get; init; } = ArchiveReadinessSnapshot.NotReady;
    public AppLanguage SelectedLanguage { get; init; } = AppLanguage.English;
    public bool IsInstallExecutionInProgress { get; init; }
    public bool IsAppUpdateInProgress { get; init; }
    public bool MultiGpuBlocked { get; init; }
    public bool GpuSelectionPending { get; init; }
    public string LatestRemoteCatalogErrorCode { get; init; } = "";
}
