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

public static class ShellGameInstallMetadataResolver
{
    public static string GetOptiScalerDllName(ShellGameCardModel? game)
    {
        var fromMetadata = (game?.InstallMetadata?.OptiScalerDllName ?? "").Trim();
        if (!string.IsNullOrWhiteSpace(fromMetadata))
        {
            return fromMetadata;
        }

        return (game?.OptiScalerDllName ?? "").Trim();
    }

    public static string GetReFrameworkUrl(ShellGameCardModel? game)
    {
        var fromMetadata = (game?.InstallMetadata?.ReFrameworkUrl ?? "").Trim();
        if (!string.IsNullOrWhiteSpace(fromMetadata))
        {
            return fromMetadata;
        }

        return (game?.ReframeworkUrl ?? "").Trim();
    }

    public static string GetSpecialK(ShellGameCardModel? game)
    {
        var fromMetadata = (game?.InstallMetadata?.SpecialK ?? "").Trim();
        if (!string.IsNullOrWhiteSpace(fromMetadata))
        {
            return fromMetadata;
        }

        return (game?.SpecialK ?? "").Trim();
    }

    public static string GetExtraBundle(ShellGameCardModel? game)
    {
        var fromMetadata = (game?.InstallMetadata?.ExtraBundle ?? "").Trim();
        if (!string.IsNullOrWhiteSpace(fromMetadata))
        {
            return fromMetadata;
        }

        return (game?.ExtraBundle ?? "").Trim();
    }

    public static bool GetUltimateAsiLoader(ShellGameCardModel? game)
    {
        if (game is null)
        {
            return false;
        }

        return game.InstallMetadata?.UltimateAsiLoader ?? game.UltimateAsiLoader;
    }

    public static bool GetOptiPatcher(ShellGameCardModel? game)
    {
        if (game is null)
        {
            return false;
        }

        return game.InstallMetadata?.OptiPatcher ?? game.OptiPatcher;
    }

    public static bool GetUnreal5(ShellGameCardModel? game)
    {
        if (game is null)
        {
            return false;
        }

        return game.InstallMetadata?.Unreal5 ?? game.Unreal5;
    }

    public static bool GetRtssOverlay(ShellGameCardModel? game)
    {
        if (game is null)
        {
            return false;
        }

        return game.InstallMetadata?.RtssOverlay ?? game.RtssOverlay;
    }

    public static string GetGpuBundleKey(ShellGameCardModel? game)
    {
        var fromMetadata = (game?.InstallMetadata?.GpuBundleKey ?? "").Trim();
        if (!string.IsNullOrWhiteSpace(fromMetadata))
        {
            return fromMetadata;
        }

        return (game?.GpuBundleKey ?? "").Trim();
    }

    public static string GetGpuGroup(ShellGameCardModel? game)
    {
        var fromMetadata = (game?.InstallMetadata?.GpuGroup ?? "").Trim();
        if (!string.IsNullOrWhiteSpace(fromMetadata))
        {
            return fromMetadata;
        }

        return (game?.GpuGroup ?? "").Trim();
    }
}
