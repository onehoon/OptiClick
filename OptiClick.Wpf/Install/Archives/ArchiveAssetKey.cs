namespace OptiClick.Wpf.Install.Archives;

public enum ArchiveAssetKey
{
    OptiScaler,
    OptiPatcher,
    SpecialK,
    ReFramework,
    Unreal5,
    Fsr4,
    Amdxc64
}

public static class ArchiveAssetRuntimeDataKeys
{
    public const string OptiScaler = "optiscaler";
    public const string OptiPatcher = "optipatcher";
    public const string SpecialK = "specialk";
    public const string ReFramework = "reframework";
    public const string Unreal5 = "unreal5";
    public const string Fsr4 = "fsr4_variants";
    public const string Amdxc64 = "amdxc64";

    public static string ToRuntimeDataEntryKey(ArchiveAssetKey key)
    {
        if (StartupArchiveAssetDefinitions.TryGet(key, out var definition))
        {
            return definition.RuntimeDataKey;
        }

        return key switch
        {
            ArchiveAssetKey.OptiScaler => OptiScaler,
            ArchiveAssetKey.OptiPatcher => OptiPatcher,
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
