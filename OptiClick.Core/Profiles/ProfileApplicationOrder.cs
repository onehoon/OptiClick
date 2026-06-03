namespace OptiClick.Core.Profiles;

public static class ProfileApplicationOrder
{
    public static readonly IReadOnlyList<string> Kinds = new[]
    {
        "optiscaler_ini",
        "game_ini_profile",
        "game_unreal_ini_profile",
        "game_xml_profile",
        "game_json_profile",
        "engine_ini_profile",
        "registry_profile",
        "rtss"
    };
}
