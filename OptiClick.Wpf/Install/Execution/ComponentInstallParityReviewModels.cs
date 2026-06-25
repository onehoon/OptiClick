using OptiClick.Core.Install.Planning;
using OptiClick.Core.RuntimeData;

namespace OptiClick.Wpf.Install.Execution;

public sealed record ComponentInstallParityReviewInput
{
    public CoreInstallPlan Plan { get; init; } = new();
    public AttachedRuntimeProfileRows ProfileRows { get; init; } = AttachedRuntimeProfileRows.Empty;
    public ArchiveReadinessSnapshot ArchiveReadiness { get; init; } = ArchiveReadinessSnapshot.NotReady;
    public InstallPrecheckSnapshot Precheck { get; init; } = InstallPrecheckSnapshot.NotStarted;
}

public sealed record ComponentInstallParityEvent
{
    public string Code { get; init; } = "";
    public string Detail { get; init; } = "";
}

public sealed record ComponentInstallParityReviewResult
{
    public bool IsSuccess { get; init; }
    public string ErrorCode { get; init; } = "";
    public IReadOnlyList<ComponentInstallParityEvent> Events { get; init; } = [];

    public string FinalProxyDllName { get; init; } = "";
    public IReadOnlyList<string> ProxyCandidateChain { get; init; } = [];

    public IReadOnlyList<string> ManagedBackupCandidates { get; init; } = [];
    public IReadOnlyList<string> LegacyCleanupTargets { get; init; } = [];

    public bool OptiPatcher { get; init; }
    public bool ReFramework { get; init; }
    public bool SpecialK { get; init; }
    public bool Unreal5 { get; init; }
    public bool ExtraBundle { get; init; }
    public bool RtssOverlay { get; init; }

    public IReadOnlyList<string> ExcludeListPatterns { get; init; } = [];

    public int GameIniProfileRowCount { get; init; }
    public int GameUnrealIniProfileRowCount { get; init; }
    public int GameXmlProfileRowCount { get; init; }
    public int GameJsonProfileRowCount { get; init; }
    public int EngineIniProfileRowCount { get; init; }
    public int RegistryProfileRowCount { get; init; }
}

public interface IComponentInstallParityReviewBuilder
{
    ComponentInstallParityReviewResult Build(ComponentInstallParityReviewInput input);
}
