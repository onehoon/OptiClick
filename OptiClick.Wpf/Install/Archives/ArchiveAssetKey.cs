namespace OptiClick.Wpf.Install.Archives;

public enum ArchiveAssetKey
{
    OptiScaler,
    Fsr4,
    OptiPatcher,
    SpecialK,
    ReFramework,
    UltimateAsiLoader,
    Unreal5
}

public static class ArchiveAssetRuntimeDataKeys
{
    public const string OptiScaler = "optiscaler";
    public const string Fsr4 = "fsr4int8";
    public const string OptiPatcher = "optipatcher";
    public const string SpecialK = "specialk";
    public const string ReFramework = "reframework";
    public const string UltimateAsiLoader = "ultimateasiloader";
    public const string Unreal5 = "unreal5";

    public static string ToRuntimeDataEntryKey(ArchiveAssetKey key)
    {
        return key switch
        {
            ArchiveAssetKey.OptiScaler => OptiScaler,
            ArchiveAssetKey.Fsr4 => Fsr4,
            ArchiveAssetKey.OptiPatcher => OptiPatcher,
            ArchiveAssetKey.SpecialK => SpecialK,
            ArchiveAssetKey.ReFramework => ReFramework,
            ArchiveAssetKey.UltimateAsiLoader => UltimateAsiLoader,
            ArchiveAssetKey.Unreal5 => Unreal5,
            _ => ""
        };
    }

    public static string ToStateKey(ArchiveAssetKey key)
    {
        return key switch
        {
            ArchiveAssetKey.UltimateAsiLoader => "ual",
            _ => ToRuntimeDataEntryKey(key)
        };
    }
}
