using OptiClick.Core.Games.GpuBundle;
using OptiClick.Core.RuntimeData;

namespace OptiClick.Core.Install;

public sealed record InstallDescriptorInput
{
    public static readonly InstallDescriptorInput Empty = new();

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
    public IReadOnlyList<string> ExcludeListPatterns { get; init; } = [];
    public string GpuBundleKey { get; init; } = "";
    public string GpuGroup { get; init; } = "";
    public Fsr4ManifestPolicy Fsr4 { get; init; } = Fsr4ManifestPolicy.Disabled;
    public bool IsEnabled { get; init; } = true;

    public IReadOnlyDictionary<string, string> IniSettings { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<RuntimeDataRawRow> GameIniProfileRows { get; init; } = [];
    public IReadOnlyList<RuntimeDataRawRow> GameUnrealIniProfileRows { get; init; } = [];
    public IReadOnlyList<RuntimeDataRawRow> EngineIniProfileRows { get; init; } = [];
    public IReadOnlyList<RuntimeDataRawRow> GameXmlProfileRows { get; init; } = [];
    public IReadOnlyList<RuntimeDataRawRow> RegistryProfileRows { get; init; } = [];
    public IReadOnlyList<RuntimeDataRawRow> GameJsonProfileRows { get; init; } = [];
}
