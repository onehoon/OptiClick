namespace OptiClick.Core.Models;

public sealed record GameEntry
{
    public string GameId { get; init; } = "";
    public string GameNameKr { get; init; } = "";
    public string GameNameEn { get; init; } = "";
    public IReadOnlyList<string> MatchFiles { get; init; } = Array.Empty<string>();
    public string MatchAnchor { get; init; } = "";
    public bool Enabled { get; init; } = true;
    public bool? SupportIntel { get; init; }
    public bool? SupportAmd { get; init; }
    public bool? SupportNvidia { get; init; }
    public string SupportedGpu { get; init; } = "";
    public string OptiScalerDllName { get; init; } = "";
    public string ReframeworkUrl { get; init; } = "";
    public string SpecialK { get; init; } = "";
    public bool UltimateAsiLoader { get; init; }
    public bool OptiPatcher { get; init; }
    public bool Unreal5 { get; init; }
    public bool RtssOverlay { get; init; }
    public string ExtraBundle { get; init; } = "";
    public IReadOnlyList<IniSettingPlan> IniSettings { get; init; } = Array.Empty<IniSettingPlan>();
}
