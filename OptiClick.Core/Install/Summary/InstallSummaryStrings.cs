namespace OptiClick.Core.Install.Summary;

public sealed record InstallSummaryStrings
{
    public string ActionInstall { get; init; } = "Install";
    public string ActionUpdate { get; init; } = "Update";
    public string ActionReinstall { get; init; } = "Reinstall";
    public string AutoConfigApplied { get; init; } = "auto config applied";

    public string ComponentOptiPatcher { get; init; } = "OptiPatcher";
    public string ComponentUnreal5 { get; init; } = "Unreal 5";
    public string ComponentReframework { get; init; } = "REFramework";
    public string ComponentUltimateAsiLoader { get; init; } = "Ultimate ASI Loader";
    public string ComponentSpecialK { get; init; } = "Special K";
    public string ComponentRtssOverlay { get; init; } = "RTSS Overlay";
}
