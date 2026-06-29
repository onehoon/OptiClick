namespace OptiClick.Core.OptiScaler;

public static class OptiScalerFsr411Policy
{
    public const string ModeAuto = "auto";
    public const string ModeEnabled = "enabled";
    public const string RadeonRx60Bundle = "radeon_rx60";
    public const string RadeonRx70Bundle = "radeon_rx70";
    public const string RadeonRx90Bundle = "radeon_rx90";

    public static bool IsNativeBundle(string? bundleKey)
    {
        var normalized = NormalizeBundleKey(bundleKey);
        return normalized is RadeonRx70Bundle or RadeonRx90Bundle;
    }

    public static bool IsRadeonRx60Bundle(string? bundleKey)
    {
        return string.Equals(
            NormalizeBundleKey(bundleKey),
            RadeonRx60Bundle,
            StringComparison.OrdinalIgnoreCase);
    }

    public static bool ShouldShowMenu(string? bundleKey)
    {
        return !IsNativeBundle(bundleKey);
    }

    public static string NormalizeMode(string? mode)
    {
        return string.Equals((mode ?? "").Trim(), ModeEnabled, StringComparison.OrdinalIgnoreCase)
            ? ModeEnabled
            : ModeAuto;
    }

    public static string NormalizeModeForBundle(string? mode, string? bundleKey)
    {
        return IsNativeBundle(bundleKey) ? ModeAuto : NormalizeMode(mode);
    }

    public static IReadOnlyList<OptiScalerCommonIniEntry> BuildManagedEntries(
        string? mode,
        string? bundleKey)
    {
        var normalizedMode = NormalizeModeForBundle(mode, bundleKey);
        if (!string.Equals(normalizedMode, ModeEnabled, StringComparison.OrdinalIgnoreCase))
        {
            return AutoEntries();
        }

        return
        [
            Entry(
                OptiScalerFsr411IniKeys.FsrSection,
                OptiScalerFsr411IniKeys.Fsr4ForceModel,
                "2"),
            Entry(
                OptiScalerFsr411IniKeys.FsrSection,
                OptiScalerFsr411IniKeys.LoadCustomAmdxc64OnRdna2,
                IsRadeonRx60Bundle(bundleKey) ? "true" : ModeAuto)
        ];
    }

    private static IReadOnlyList<OptiScalerCommonIniEntry> AutoEntries()
    {
        return
        [
            Entry(
                OptiScalerFsr411IniKeys.FsrSection,
                OptiScalerFsr411IniKeys.Fsr4ForceModel,
                ModeAuto),
            Entry(
                OptiScalerFsr411IniKeys.FsrSection,
                OptiScalerFsr411IniKeys.LoadCustomAmdxc64OnRdna2,
                ModeAuto)
        ];
    }

    private static OptiScalerCommonIniEntry Entry(string section, string key, string value)
    {
        return new OptiScalerCommonIniEntry
        {
            Section = section,
            Key = key,
            Value = value
        };
    }

    private static string NormalizeBundleKey(string? value)
    {
        return (value ?? string.Empty).Trim().ToLowerInvariant();
    }
}
