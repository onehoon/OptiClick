using OptiClick.Core.Models;

namespace OptiClick.Core.Games;

public static class GameEntryMapper
{
    public static bool TryMap(IReadOnlyDictionary<string, string> row, out GameEntry game)
    {
        game = new GameEntry();
        var gameId = Get(row, "game_id", "gameId", "id");
        var matchFiles = GameEntryNormalizer.NormalizeMatchFiles(Get(row, "match_exe", "match_files", "matchExe"));

        var enabled = FlagParser.ParseNullableBoolean(Get(row, "enabled", "enable", "is_enabled")) ?? true;
        if (!GameEntryValidator.ShouldInclude(gameId, matchFiles, enabled))
        {
            return false;
        }

        var mapped = new GameEntry
        {
            GameId = gameId,
            GameNameKr = Get(row, "game_kr", "gameNameKr", "name_kr"),
            GameNameEn = Get(row, "game_en", "gameNameEn", "name_en"),
            MatchFiles = matchFiles,
            MatchAnchor = GameEntryNormalizer.ResolveMatchAnchor(matchFiles),
            Enabled = enabled,
            SupportIntel = FlagParser.ParseNullableBoolean(Get(row, "support_intel", "intel", "supportIntel")),
            SupportAmd = FlagParser.ParseNullableBoolean(Get(row, "support_amd", "amd", "supportAmd")),
            SupportNvidia = FlagParser.ParseNullableBoolean(Get(row, "support_nvidia", "nvidia", "supportNvidia")),
            SupportedGpu = Get(row, "supported_gpu", "supportedGpu"),
            OptiScalerDllName = Get(row, "optiscaler_dll_name", "optiScalerDllName", "proxy_dll"),
            ReframeworkUrl = Get(row, "reframework_url", "reframework", "reframeworkUrl"),
            SpecialK = Get(row, "specialk", "special_k", "specialK"),
            UltimateAsiLoader = FlagParser.ParseBoolean(Get(row, "ultimate_asi_loader", "ultimateAsiLoader", "ual")),
            OptiPatcher = FlagParser.ParseBoolean(Get(row, "optipatcher", "optiPatcher")),
            Unreal5 = FlagParser.ParseBoolean(Get(row, "unreal5", "ue5", "unreal_5")),
            RtssOverlay = FlagParser.ParseBoolean(Get(row, "rtss_overlay", "rtssOverlay")),
            ExtraBundle = Get(row, "extra_bundle", "extraBundle")
        };
        game = GameEntryNormalizer.Normalize(mapped);
        return GameEntryValidator.IsValid(game);
    }

    private static string Get(IReadOnlyDictionary<string, string> row, params string[] names)
    {
        foreach (var name in names)
        {
            if (row.TryGetValue(name, out var value))
            {
                return value.Trim();
            }
        }
        return "";
    }
}
