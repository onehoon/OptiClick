using OptiClick.Wpf.Install.Flow;
using OptiClick.Wpf.Install.Planning;

namespace OptiClick.Wpf.Install.Archives;

public sealed record ArchiveReadinessFlowResult
{
    public bool DidRun { get; init; }
    public bool IsSuccess { get; init; }
    public ArchiveReadinessSnapshot Readiness { get; init; } = ArchiveReadinessSnapshot.NotReady;
    public IReadOnlyList<InstallFlowLogEntry> Logs { get; init; } = [];
}
