namespace OptiClick.Core.Models;

public sealed record InstallPlan
{
    public string SourceArchive { get; init; } = "";
    public string PreferredProxyName { get; init; } = "";
    public string FinalDllName { get; init; } = "";
    public IReadOnlyList<string> ComponentOrder { get; init; } = Array.Empty<string>();
    public IReadOnlyList<ComponentPlan> Components { get; init; } = Array.Empty<ComponentPlan>();
    public IReadOnlyList<string> BackupCandidates { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> LegacyCleanupTargets { get; init; } = Array.Empty<string>();
    public IReadOnlyList<IniSettingPlan> IniSettings { get; init; } = Array.Empty<IniSettingPlan>();
    public IReadOnlyList<ProfilePlan> Profiles { get; init; } = Array.Empty<ProfilePlan>();
    public IReadOnlyList<RegistryPlan> RegistryRows { get; init; } = Array.Empty<RegistryPlan>();
    public IReadOnlyList<RtssActionPlan> RtssActions { get; init; } = Array.Empty<RtssActionPlan>();
    public IReadOnlyList<string> ExpectedWarnings { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> ExpectedSkipReasons { get; init; } = Array.Empty<string>();
    public IReadOnlyDictionary<string, string> AfterInstallSummary { get; init; } = new Dictionary<string, string>();
}
