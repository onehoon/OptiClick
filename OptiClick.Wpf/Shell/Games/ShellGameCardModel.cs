using System.Text.Json;

namespace OptiClick.Wpf.Shell.Games;

public sealed class ShellGameCardModel
{
    public string GameId { get; init; } = "";
    public string MatchExe { get; init; } = "";
    public string DisplayName { get; init; } = "";
    public string GameNameEn { get; init; } = "";
    public string GameNameKr { get; init; } = "";
    public string CoverSteamAppId { get; init; } = "";
    public string CoverUrl { get; init; } = "";
    public string SupportedGpu { get; init; } = "";
    public bool Enabled { get; init; }
    public bool? SupportIntel { get; init; }
    public bool? SupportAmd { get; init; }
    public bool? SupportNvidia { get; init; }
    public bool GpuBundleLoaded { get; init; }
    public bool GpuBundleSupported { get; init; }
    public string GpuProfileId { get; init; } = "";
    public string GpuBundleVendor { get; init; } = "";
    public string GpuBundleKey { get; init; } = "";
    public string GpuGroup { get; init; } = "";
    public Fsr4ManifestPolicy Fsr4 { get; init; } = Fsr4ManifestPolicy.Disabled;

    // Install policy values are mirrored for backward compatibility with legacy callers.
    // Runtime resolution should prioritize values from InstallMetadata (via ShellGameInstallMetadataResolver),
    // then fall back to these fields only when metadata is missing.
    public string OptiScalerDllName { get; init; } = "";
    public string ReframeworkUrl { get; init; } = "";
    public string SpecialK { get; init; } = "";
    public bool UltimateAsiLoader { get; init; }
    public bool OptiPatcher { get; init; }
    public bool Unreal5 { get; init; }
    public bool RtssOverlay { get; init; }
    public string ExtraBundle { get; init; } = "";
    public string InstallPreKr { get; init; } = "";
    public string InstallPreEn { get; init; } = "";
    public string InstallPostKr { get; init; } = "";
    public string InstallPostEn { get; init; } = "";
    public string GuideUrl { get; init; } = "";
    public string ExcludeListRaw { get; init; } = "";
    public IReadOnlyList<string> ExcludeListPatterns { get; init; } = [];
    public MergedGameInstallMetadata? InstallMetadata { get; init; }
    public IReadOnlyDictionary<string, JsonElement> RuntimeDataValues { get; init; } =
        new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
}
