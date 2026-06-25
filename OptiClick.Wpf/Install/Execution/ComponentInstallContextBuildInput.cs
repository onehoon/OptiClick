using OptiClick.Core.Install;
using OptiClick.Core.Install.Planning;
using OptiClick.Core.Runtime;

namespace OptiClick.Wpf.Install.Execution;

public sealed record ComponentInstallContextBuildInput
{
    public CoreInstallPlan Plan { get; init; } = new();
    public InstallExecutionDescriptor ExecutionDescriptor { get; init; } = InstallExecutionDescriptor.Empty;
    public RuntimeContext LatestRuntimeContext { get; init; } = new();
    public ArchiveReadinessSnapshot LatestArchiveReadiness { get; init; } = ArchiveReadinessSnapshot.NotReady;
    public ModuleDownloadLinkCatalog ModuleDownloadLinks { get; init; } = ModuleDownloadLinkCatalog.Empty;
}
