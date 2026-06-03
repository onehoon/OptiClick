using OptiClick.Core.Runtime;
using OptiClick.Wpf.Shell.Scan;
using OptiClick.Wpf.Shell.Selection;
using OptiClick.Wpf.ViewModels;

namespace OptiClick.Wpf.Install.Planning;

public sealed record InstallPlanInputBuildContext
{
    public GameCardViewModel SelectedGame { get; init; } = null!;
    public RuntimeContext LatestRuntimeContext { get; init; } = new();
    public ShellInstallSelectionState SelectionState { get; init; } = new();
    public ArchiveReadinessSnapshot LatestArchiveReadiness { get; init; } = ArchiveReadinessSnapshot.NotReady;
    public IReadOnlyDictionary<string, ShellGameMatchResult> MatchByGameId { get; init; } = new Dictionary<string, ShellGameMatchResult>(StringComparer.OrdinalIgnoreCase);
    public IReadOnlyDictionary<string, string> TargetPathByGameId { get; init; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    public bool IsInstallExecutionInProgress { get; init; }
    public bool IsAppUpdateInProgress { get; init; }
}
