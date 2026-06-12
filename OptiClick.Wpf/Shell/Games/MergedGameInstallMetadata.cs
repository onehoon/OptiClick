using OptiClick.Wpf.Shell.RuntimeData;

namespace OptiClick.Wpf.Shell.Games;

public sealed class MergedGameInstallMetadata
{
    public static readonly MergedGameInstallMetadata Empty = new();

    public bool GpuBundleLoaded { get; init; }
    public bool GpuBundleSupported { get; init; }
    public string GpuProfileId { get; init; } = "";
    public string GpuBundleVendor { get; init; } = "";
    public string GpuBundleKey { get; init; } = "";
    public string GpuGroup { get; init; } = "";
    public Fsr4ManifestPolicy Fsr4 { get; init; } = Fsr4ManifestPolicy.Disabled;

    public string OptiScalerDllName { get; init; } = "";
    public string ReFrameworkUrl { get; init; } = "";
    public string SpecialK { get; init; } = "";
    public string ExtraBundle { get; init; } = "";
    public string ExcludeListRaw { get; init; } = "";
    public IReadOnlyList<string> ExcludeListPatterns { get; init; } = [];

    public bool UltimateAsiLoader { get; init; }
    public bool OptiPatcher { get; init; }
    public bool Unreal5 { get; init; }
    public bool RtssOverlay { get; init; }

    public IReadOnlyDictionary<string, string> IniSettings { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<RuntimeDataRawRow> GameIniProfileRows { get; init; } = [];
    public IReadOnlyList<RuntimeDataRawRow> GameUnrealIniProfileRows { get; init; } = [];
    public IReadOnlyList<RuntimeDataRawRow> EngineIniProfileRows { get; init; } = [];
    public IReadOnlyList<RuntimeDataRawRow> GameXmlProfileRows { get; init; } = [];
    public IReadOnlyList<RuntimeDataRawRow> RegistryProfileRows { get; init; } = [];
    public IReadOnlyList<RuntimeDataRawRow> GameJsonProfileRows { get; init; } = [];
}

internal static class ShellGameInstallMetadataResolver
{
    // Merged install metadata is the primary source for install policy values.
    // ShellGameCardModel fields remain for compatibility and are used only when metadata values are absent.
    // Keep Shell metadata merging at Shell/descriptor boundaries. Install execution should consume descriptors.
    public static MergedGameInstallMetadata ResolveEffective(ShellGameCardModel? game)
    {
        if (game is null)
        {
            return MergedGameInstallMetadata.Empty;
        }

        var metadata = game.InstallMetadata;
        return new MergedGameInstallMetadata
        {
            GpuBundleLoaded = metadata?.GpuBundleLoaded ?? game.GpuBundleLoaded,
            GpuBundleSupported = metadata?.GpuBundleSupported ?? game.GpuBundleSupported,
            GpuProfileId = PickFirst(metadata?.GpuProfileId, game.GpuProfileId),
            GpuBundleVendor = PickFirst(metadata?.GpuBundleVendor, game.GpuBundleVendor),
            GpuBundleKey = GetGpuBundleKey(game),
            GpuGroup = GetGpuGroup(game),
            Fsr4 = ResolveFsr4Policy(game),
            OptiScalerDllName = GetOptiScalerDllName(game),
            ReFrameworkUrl = GetReFrameworkUrl(game),
            SpecialK = GetSpecialK(game),
            ExtraBundle = GetExtraBundle(game),
            ExcludeListRaw = PickFirst(metadata?.ExcludeListRaw, game.ExcludeListRaw),
            ExcludeListPatterns = ResolveExcludeListPatterns(game),
            UltimateAsiLoader = GetUltimateAsiLoader(game),
            OptiPatcher = GetOptiPatcher(game),
            Unreal5 = GetUnreal5(game),
            RtssOverlay = GetRtssOverlay(game),
            IniSettings = metadata?.IniSettings
                          ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
            GameIniProfileRows = metadata?.GameIniProfileRows ?? [],
            GameUnrealIniProfileRows = metadata?.GameUnrealIniProfileRows ?? [],
            EngineIniProfileRows = metadata?.EngineIniProfileRows ?? [],
            GameXmlProfileRows = metadata?.GameXmlProfileRows ?? [],
            RegistryProfileRows = metadata?.RegistryProfileRows ?? [],
            GameJsonProfileRows = metadata?.GameJsonProfileRows ?? []
        };
    }

    private static string GetOptiScalerDllName(ShellGameCardModel? game)
    {
        // InstallMetadata (if present) is treated as source of truth.
        var fromMetadata = (game?.InstallMetadata?.OptiScalerDllName ?? "").Trim();
        if (!string.IsNullOrWhiteSpace(fromMetadata))
        {
            return fromMetadata;
        }

        return (game?.OptiScalerDllName ?? "").Trim();
    }

    private static string GetReFrameworkUrl(ShellGameCardModel? game)
    {
        var fromMetadata = (game?.InstallMetadata?.ReFrameworkUrl ?? "").Trim();
        if (!string.IsNullOrWhiteSpace(fromMetadata))
        {
            return fromMetadata;
        }

        return (game?.ReframeworkUrl ?? "").Trim();
    }

    private static string GetSpecialK(ShellGameCardModel? game)
    {
        var fromMetadata = (game?.InstallMetadata?.SpecialK ?? "").Trim();
        if (!string.IsNullOrWhiteSpace(fromMetadata))
        {
            return fromMetadata;
        }

        return (game?.SpecialK ?? "").Trim();
    }

    private static string GetExtraBundle(ShellGameCardModel? game)
    {
        var fromMetadata = (game?.InstallMetadata?.ExtraBundle ?? "").Trim();
        if (!string.IsNullOrWhiteSpace(fromMetadata))
        {
            return fromMetadata;
        }

        return (game?.ExtraBundle ?? "").Trim();
    }

    private static bool GetUltimateAsiLoader(ShellGameCardModel? game)
    {
        if (game is null)
        {
            return false;
        }

        return game.InstallMetadata?.UltimateAsiLoader ?? game.UltimateAsiLoader;
    }

    private static bool GetOptiPatcher(ShellGameCardModel? game)
    {
        if (game is null)
        {
            return false;
        }

        return game.InstallMetadata?.OptiPatcher ?? game.OptiPatcher;
    }

    private static bool GetUnreal5(ShellGameCardModel? game)
    {
        if (game is null)
        {
            return false;
        }

        return game.InstallMetadata?.Unreal5 ?? game.Unreal5;
    }

    private static bool GetRtssOverlay(ShellGameCardModel? game)
    {
        if (game is null)
        {
            return false;
        }

        return game.InstallMetadata?.RtssOverlay ?? game.RtssOverlay;
    }

    private static string GetGpuBundleKey(ShellGameCardModel? game)
    {
        var fromMetadata = (game?.InstallMetadata?.GpuBundleKey ?? "").Trim();
        if (!string.IsNullOrWhiteSpace(fromMetadata))
        {
            return fromMetadata;
        }

        return (game?.GpuBundleKey ?? "").Trim();
    }

    private static string GetGpuGroup(ShellGameCardModel? game)
    {
        var fromMetadata = (game?.InstallMetadata?.GpuGroup ?? "").Trim();
        if (!string.IsNullOrWhiteSpace(fromMetadata))
        {
            return fromMetadata;
        }

        return (game?.GpuGroup ?? "").Trim();
    }

    private static Fsr4ManifestPolicy ResolveFsr4Policy(ShellGameCardModel? game)
    {
        if (game?.InstallMetadata?.Fsr4 is { } metadataPolicy)
        {
            return metadataPolicy;
        }

        return game?.Fsr4 ?? Fsr4ManifestPolicy.Disabled;
    }

    private static IReadOnlyList<string> ResolveExcludeListPatterns(ShellGameCardModel game)
    {
        var metadataPatterns = game.InstallMetadata?.ExcludeListPatterns;
        if (metadataPatterns is { Count: > 0 })
        {
            return metadataPatterns.ToArray();
        }

        return game.ExcludeListPatterns?.ToArray() ?? [];
    }

    private static string PickFirst(string? first, string? second)
    {
        var normalizedFirst = (first ?? "").Trim();
        if (!string.IsNullOrWhiteSpace(normalizedFirst))
        {
            return normalizedFirst;
        }

        return (second ?? "").Trim();
    }
}
