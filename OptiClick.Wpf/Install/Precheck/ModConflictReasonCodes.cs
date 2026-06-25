namespace OptiClick.Wpf.Install.Precheck;

public static class ModConflictKinds
{
    public const string ReShade = "reshade";
    public const string SpecialK = "special_k";
    public const string RenoDx = "renodx";
    public const string LennyModLoader = "lenny_mod_loader";
    public const string ScriptHookRdr2 = "script_hook_rdr2";
    public const string ReFrameworkLegacy = "reframework_legacy";
}

public enum ModConflictNoticeMode
{
    None,
    ReFrameworkLegacyOnly,
    GenericModConflict
}

