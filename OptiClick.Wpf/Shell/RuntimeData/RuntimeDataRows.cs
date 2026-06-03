using System.Text.Json;

namespace OptiClick.Wpf.Shell.RuntimeData;

public static class RuntimeDataSectionNames
{
    public const string GameMaster = "game_master";
    public const string GameIniProfile = "game_ini_profile";
    public const string GameUnrealIniProfile = "game_unreal_ini_profile";
    public const string GameXmlProfile = "game_xml_profile";
    public const string GameJsonProfile = "game_json_profile";
    public const string EngineIniProfile = "engine_ini_profile";
    public const string RegistryProfile = "registry_profile";
    public const string ResourceMaster = "resource_master";
    public const string MessageBinding = "message_binding";
    public const string MessageCenter = "message_center";
    public const string NewGameSupport = "new_game_support";

    public static readonly string[] RequiredSections =
    [
        EngineIniProfile,
        GameIniProfile,
        GameJsonProfile,
        GameMaster,
        GameUnrealIniProfile,
        GameXmlProfile,
        MessageBinding,
        MessageCenter,
        RegistryProfile,
        ResourceMaster
    ];
}

public record RuntimeDataRawRow
{
    public IReadOnlyDictionary<string, JsonElement> Values { get; init; } =
        new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
}

public sealed record RuntimeDataProfileRow : RuntimeDataRawRow;

public sealed record RuntimeDataResourceRow : RuntimeDataRawRow;

public sealed record RuntimeDataMessageRow : RuntimeDataRawRow;

public sealed record RuntimeDataNewGameSupportRow : RuntimeDataRawRow;

public static class RuntimeDataRowReader
{
    public static string GetString(RuntimeDataRawRow row, string key)
    {
        if (row is null || string.IsNullOrWhiteSpace(key))
        {
            return "";
        }

        if (!row.Values.TryGetValue(key, out var value))
        {
            return "";
        }

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString()?.Trim() ?? "",
            JsonValueKind.Null or JsonValueKind.Undefined => "",
            _ => value.ToString().Trim()
        };
    }

    public static bool TryGetValue(RuntimeDataRawRow row, string key, out JsonElement value)
    {
        value = default;
        if (row is null || string.IsNullOrWhiteSpace(key))
        {
            return false;
        }

        return row.Values.TryGetValue(key, out value);
    }
}

public static class RuntimeDataGameProfileKeys
{
    public const string InstallPreKr = "__install_pre_kr__";
    public const string InstallPreEn = "__install_pre_en__";
    public const string InstallPostKr = "__install_post_kr__";
    public const string InstallPostEn = "__install_post_en__";
    public const string InstallSummaryNoteKr = "__install_summary_note_kr__";
    public const string InstallSummaryNoteEn = "__install_summary_note_en__";
    public const string GuideUrl = "__guide_url__";
    public const string ExcludeList = "exclude_list";
}
