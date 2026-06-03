namespace OptiClick.Core.Install.Summary;

public sealed record InstallSummaryInput
{
    public string InstallStatusCode { get; init; } = "";
    public string InstalledVersion { get; init; } = "";
    public string CurrentVersion { get; init; } = "";
    public string CurrentDisplayVersion { get; init; } = "";

    public bool OptiPatcher { get; init; }
    public bool Unreal5 { get; init; }
    public string ReframeworkUrl { get; init; } = "";
    public bool UltimateAsiLoader { get; init; }
    public string SpecialK { get; init; } = "";
    public bool RtssOverlay { get; init; }

    public string InstallSummaryNote { get; init; } = "";
}
