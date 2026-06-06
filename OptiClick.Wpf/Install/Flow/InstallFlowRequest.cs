using OptiClick.Core.Runtime;
using OptiClick.Wpf.Install.Planning;
using OptiClick.Wpf.Localization;
using OptiClick.Wpf.Shell.Scan;
using OptiClick.Wpf.Shell.Selection;
using OptiClick.Wpf.ViewModels;

namespace OptiClick.Wpf.Install.Flow;

public sealed record InstallFlowRequest
{
    public required GameCardViewModel SelectedGame { get; init; }
    public required int SelectedIndex { get; init; }
    public required RuntimeContext LatestRuntimeContext { get; init; }
    public required ArchiveReadinessSnapshot LatestArchiveReadiness { get; init; }
    public required ShellInstallSelectionState SelectionState { get; init; }
    public required IReadOnlyDictionary<string, ShellGameMatchResult> MatchByGameId { get; init; }
    public required IReadOnlyDictionary<string, string> TargetPathByGameId { get; init; }
    public required IReadOnlyDictionary<string, object?> ModuleDownloadLinks { get; init; }
    public IReadOnlyDictionary<string, string> CommonOptiScalerIniSettings { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    public required bool IsWindowsSupported { get; init; }
    public required bool IsInstallExecutionInProgress { get; init; }
    public required bool IsAppUpdateInProgress { get; init; }
    public required AppStrings Strings { get; init; }
    public string LatestRemoteCatalogErrorCode { get; init; } = "";
}
