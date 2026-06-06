using OptiClick.Wpf.Localization;
using WpfInstallPlan = OptiClick.Wpf.Install.Planning.InstallPlan;

namespace OptiClick.Wpf.Install.Config;

public sealed record ConfigApplyFlowRequest
{
    public required WpfInstallPlan Plan { get; init; }
    public IReadOnlyDictionary<string, string> OptiScalerIniSettings { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    public IReadOnlyDictionary<string, string> CommonOptiScalerIniSettings { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    public required AppStrings Strings { get; init; }
    public required bool InstallSucceeded { get; init; }
}
