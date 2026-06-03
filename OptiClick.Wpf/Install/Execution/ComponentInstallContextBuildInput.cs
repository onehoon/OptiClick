using WpfInstallPlan = OptiClick.Wpf.Install.Planning.InstallPlan;
using OptiClick.Core.Runtime;
using OptiClick.Wpf.Shell.Games;
using OptiClick.Wpf.Install.Planning;

namespace OptiClick.Wpf.Install.Execution;

public sealed record ComponentInstallContextBuildInput
{
    public WpfInstallPlan Plan { get; init; } = new();
    public ShellGameCardModel SelectedGame { get; init; } = new();
    public RuntimeContext LatestRuntimeContext { get; init; } = new();
    public ArchiveReadinessSnapshot LatestArchiveReadiness { get; init; } = ArchiveReadinessSnapshot.NotReady;
    public IReadOnlyDictionary<string, object?> ModuleDownloadLinks { get; init; } = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
}
