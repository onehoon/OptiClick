namespace OptiClick.Core.Install;

public sealed record InstallGameDescriptor
{
    public static readonly InstallGameDescriptor Empty = new();

    public string GameId { get; init; } = "";
    public string DisplayName { get; init; } = "";
    public string GameNameEn { get; init; } = "";
    public string GameNameKr { get; init; } = "";
    public string MatchExe { get; init; } = "";
    public string OptiScalerDllName { get; init; } = "";
    public string ReFrameworkUrl { get; init; } = "";
    public string SpecialK { get; init; } = "";
    public bool RequiresOptiPatcher { get; init; }
    public bool RequiresUnreal5 { get; init; }
    public bool RequiresRtssProfile { get; init; }
    public string ExtraBundle { get; init; } = "";
    public string ExcludeListRaw { get; init; } = "";
    public IReadOnlyList<string> ExcludeListPatterns { get; init; } = Array.Empty<string>();
}
