namespace OptiClick.Core.Install;

public enum InstallGameMatchState
{
    None,
    Matched,
    MultipleCandidates,
    Disabled,
    UnsupportedGpu
}

public sealed record InstallGameMatchSnapshot
{
    public InstallGameMatchState State { get; init; }
    public string MatchedExe { get; init; } = "";
    public string FolderPath { get; init; } = "";
    public string ReasonCode { get; init; } = "";
    public bool IsUnsupportedGpu { get; init; }
}

public sealed record InstallActionAvailabilitySnapshot
{
    public string ReasonCode { get; init; } = "";
}
