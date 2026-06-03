namespace OptiClick.Wpf.Install.Precheck;

public static class ModConflictKinds
{
    public const string ReShade = "reshade";
    public const string SpecialK = "special_k";
    public const string UltimateAsiLoader = "ultimate_asi_loader";
    public const string RenoDx = "renodx";
    public const string ReFrameworkLegacy = "reframework_legacy";
}

public enum ModConflictNoticeMode
{
    None,
    EmptyForUltimateAsiOnly,
    ReFrameworkLegacyOnly,
    GenericModConflict
}

