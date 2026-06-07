using OptiClick.Wpf.Install.Execution;
using OptiClick.Wpf.Localization;
using OptiClick.Wpf.Shell.Games;
using OptiClick.Wpf.Shell.Selection;
using WpfInstallPlan = OptiClick.Wpf.Install.Planning.InstallPlan;

namespace OptiClick.Wpf.Install.Flow;

public sealed record InstallResultApplyRequest
{
    public required WpfInstallPlan Plan { get; init; }
    public required ComponentInstallResult InstallResult { get; init; }
    public required ShellGameCardModel SelectedGame { get; init; }
    public required ShellInstallSelectionState SelectionState { get; init; }
    public IReadOnlyDictionary<string, string> CommonOptiScalerIniSettings { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    public required AppStrings Strings { get; init; }
}
