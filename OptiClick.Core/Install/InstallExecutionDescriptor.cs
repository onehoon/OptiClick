namespace OptiClick.Core.Install;

public sealed record InstallExecutionDescriptor
{
    public static readonly InstallExecutionDescriptor Empty = new();

    public InstallGameDescriptor GameDescriptor { get; init; } = InstallGameDescriptor.Empty;
    public string GpuBundleKey { get; init; } = "";
    public string GpuGroup { get; init; } = "";
    public bool ShouldInstallFsr4 { get; init; }
    public string Fsr4Variant { get; init; } = "";

    public string GameId => EffectiveGameDescriptor.GameId;
    public string MatchExe => EffectiveGameDescriptor.MatchExe;
    public string DisplayName => EffectiveGameDescriptor.DisplayName;
    public string PreferredProxyDllName => EffectiveGameDescriptor.OptiScalerDllName;
    public string ReFrameworkDestination => EffectiveGameDescriptor.ReFrameworkUrl;
    public string SpecialK => EffectiveGameDescriptor.SpecialK;
    public string ExtraBundle => EffectiveGameDescriptor.ExtraBundle;
    public bool RequiresUltimateAsiLoader => EffectiveGameDescriptor.RequiresUltimateAsiLoader;
    public bool RequiresOptiPatcher => EffectiveGameDescriptor.RequiresOptiPatcher;
    public bool RequiresUnreal5 => EffectiveGameDescriptor.RequiresUnreal5;
    public string ExcludeListRaw => EffectiveGameDescriptor.ExcludeListRaw;
    public IReadOnlyList<string> ExcludeListPatterns => EffectiveGameDescriptor.ExcludeListPatterns;

    private InstallGameDescriptor EffectiveGameDescriptor => GameDescriptor ?? InstallGameDescriptor.Empty;
}
