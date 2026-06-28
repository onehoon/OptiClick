namespace OptiClick.Wpf.Install.Archives;

public enum ArchiveAssetKey
{
    OptiScaler,
    OptiPatcher,
    SpecialK,
    ReFramework,
    Unreal5
}

public static class ArchiveAssetRuntimeDataKeys
{
    public const string OptiScaler = "optiscaler";
    public const string OptiPatcher = "optipatcher";
    public const string SpecialK = "specialk";
    public const string ReFramework = "reframework";
    public const string Unreal5 = "unreal5";

    public static string ToRuntimeDataEntryKey(ArchiveAssetKey key)
    {
        return key switch
        {
            ArchiveAssetKey.OptiScaler => OptiScaler,
            ArchiveAssetKey.OptiPatcher => OptiPatcher,
            ArchiveAssetKey.SpecialK => SpecialK,
            ArchiveAssetKey.ReFramework => ReFramework,
            ArchiveAssetKey.Unreal5 => Unreal5,
            _ => ""
        };
    }

    public static string ToStateKey(ArchiveAssetKey key)
    {
        return key switch
        {
            _ => ToRuntimeDataEntryKey(key)
        };
    }

}
