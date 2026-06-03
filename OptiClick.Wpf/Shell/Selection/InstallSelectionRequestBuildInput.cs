using OptiClick.Wpf.Install.Planning;
using OptiClick.Wpf.Shell.Games;
using OptiClick.Wpf.Shell.Scan;
using OptiClick.Wpf.ViewModels;
using OptiClick.Core.Runtime;

namespace OptiClick.Wpf.Shell.Selection;

public sealed record InstallSelectionRequestBuildInput
{
    public int SelectedIndex { get; init; } = -1;
    public GameCardViewModel SelectedCard { get; init; } = null!;
    public IReadOnlyList<GameCardViewModel> Cards { get; init; } = Array.Empty<GameCardViewModel>();
    public IReadOnlyDictionary<string, ShellGameMatchResult> MatchByGameId { get; init; } = new Dictionary<string, ShellGameMatchResult>(StringComparer.OrdinalIgnoreCase);
    public IReadOnlyDictionary<string, string> TargetPathByGameId { get; init; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    public ShellInstallSelectionState PreviousState { get; init; } = new();
    public IReadOnlyDictionary<string, object?> ModuleDownloadLinks { get; init; } = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
    public ArchiveReadinessSnapshot LatestArchiveReadiness { get; init; } = ArchiveReadinessSnapshot.NotReady;
    public AppLanguage SelectedLanguage { get; init; } = AppLanguage.English;
    public bool IsInstallExecutionInProgress { get; init; }
    public bool IsAppUpdateInProgress { get; init; }
    public bool MultiGpuBlocked { get; init; }
    public bool GpuSelectionPending { get; init; }
    public string LatestRemoteCatalogErrorCode { get; init; } = "";
}
