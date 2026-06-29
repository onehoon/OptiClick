namespace OptiClick.Core.OptiScaler;

public static class OptiScalerCommonIniSettingsMapper
{
    public const string TrueValue = "true";
    public const string FramerateLimit120Hz = "116";
    public const string FramerateLimit144Hz = "138";
    public const string FramerateLimit165Hz = "157";
    public const string FpsOverlayJustFpsValue = "0";
    public const string FpsOverlayTopLeftValue = "0";

    public static OptiScalerCommonIniSettingsDraft FromDocument(
        OptiScalerCommonIniSettingsDocument? document)
    {
        var materialized = OptiScalerCommonIniSettingsMaterializer.Materialize(document);
        return NormalizeDraft(
            new OptiScalerCommonIniSettingsDraft
            {
                Fsr411Mode = OptiScalerFsr411Policy.NormalizeMode(document?.Fsr411Mode),
                ShowFpsMode = ReadValue(materialized, OptiScalerCommonIniSettingsMaterializer.ShowFpsKey),
                MenuScale = ReadValue(materialized, OptiScalerCommonIniSettingsMaterializer.MenuScaleKey),
                FpsOverlayType = ReadValue(materialized, OptiScalerCommonIniSettingsMaterializer.FpsOverlayTypeKey),
                FpsOverlayPos = ReadValue(materialized, OptiScalerCommonIniSettingsMaterializer.FpsOverlayPosKey),
                FpsScale = ReadValue(materialized, OptiScalerCommonIniSettingsMaterializer.FpsScaleKey),
                DisableSplashMode = ReadValue(materialized, OptiScalerCommonIniSettingsMaterializer.DisableSplashKey),
                FramerateLimit = ReadValue(materialized, OptiScalerCommonIniSettingsMaterializer.FramerateLimitKey)
            });
    }

    public static OptiScalerCommonIniSettingsDocument ToDocument(
        OptiScalerCommonIniSettingsDraft? draft)
    {
        var normalized = NormalizePersistedDraft(draft);
        var entries = new List<OptiScalerCommonIniEntry>();
        AddExplicitEntry(entries, "Menu", "ShowFps", normalized.ShowFpsMode);
        AddExplicitEntry(entries, "Menu", "Scale", normalized.MenuScale);
        if (IsFpsOverlayEnabled(normalized.ShowFpsMode))
        {
            AddExplicitEntry(entries, "Menu", "FpsOverlayType", normalized.FpsOverlayType);
            AddExplicitEntry(entries, "Menu", "FpsOverlayPos", normalized.FpsOverlayPos);
            AddExplicitEntry(entries, "Menu", "FpsScale", normalized.FpsScale);
        }

        AddExplicitEntry(entries, "Menu", "DisableSplash", normalized.DisableSplashMode);
        AddExplicitEntry(entries, "Framerate", "FramerateLimit", normalized.FramerateLimit);
        return OptiScalerCommonIniSettingsMaterializer.NormalizeDocument(
            new OptiScalerCommonIniSettingsDocument
            {
                Version = 1,
                Fsr411Mode = normalized.Fsr411Mode,
                Entries = entries
            });
    }

    public static OptiScalerCommonIniSettingsDraft NormalizeDraft(
        OptiScalerCommonIniSettingsDraft? draft)
    {
        if (draft is null)
        {
            return new OptiScalerCommonIniSettingsDraft();
        }

        return new OptiScalerCommonIniSettingsDraft
        {
            Fsr411Mode = OptiScalerFsr411Policy.NormalizeMode(draft.Fsr411Mode),
            ShowFpsMode = NormalizeShowFpsMode(draft.ShowFpsMode),
            MenuScale = NormalizeAutoValue(draft.MenuScale),
            FpsOverlayType = NormalizeFpsOverlayTypeSelection(draft.FpsOverlayType),
            FpsOverlayPos = NormalizeFpsOverlayPositionSelection(draft.FpsOverlayPos),
            FpsScale = NormalizeAutoValue(draft.FpsScale),
            DisableSplashMode = NormalizeAutoValue(draft.DisableSplashMode),
            FramerateLimit = NormalizeFramerateLimitSelection(draft.FramerateLimit)
        };
    }

    public static OptiScalerCommonIniSettingsDraft NormalizePersistedDraft(
        OptiScalerCommonIniSettingsDraft? draft)
    {
        var normalized = NormalizeDraft(draft);
        return IsFpsOverlayEnabled(normalized.ShowFpsMode)
            ? normalized
            : normalized with
            {
                FpsOverlayType = OptiScalerCommonIniSettingsMaterializer.AutoValue,
                FpsOverlayPos = OptiScalerCommonIniSettingsMaterializer.AutoValue,
                FpsScale = OptiScalerCommonIniSettingsMaterializer.AutoValue
            };
    }

    public static string NormalizeAutoValue(string? value)
    {
        var normalized = (value ?? "").Trim();
        return string.IsNullOrWhiteSpace(normalized)
            ? OptiScalerCommonIniSettingsMaterializer.AutoValue
            : normalized;
    }

    public static string NormalizeShowFpsMode(string? value)
    {
        var normalized = NormalizeAutoValue(value);
        return string.Equals(normalized, TrueValue, StringComparison.OrdinalIgnoreCase)
            ? TrueValue
            : OptiScalerCommonIniSettingsMaterializer.AutoValue;
    }

    public static string NormalizeFpsOverlayTypeSelection(string? value)
    {
        var normalized = NormalizeAutoValue(value);
        return string.Equals(normalized, FpsOverlayJustFpsValue, StringComparison.OrdinalIgnoreCase)
            ? OptiScalerCommonIniSettingsMaterializer.AutoValue
            : normalized;
    }

    public static string NormalizeFpsOverlayPositionSelection(string? value)
    {
        var normalized = NormalizeAutoValue(value);
        return string.Equals(normalized, FpsOverlayTopLeftValue, StringComparison.OrdinalIgnoreCase)
            ? OptiScalerCommonIniSettingsMaterializer.AutoValue
            : normalized;
    }

    public static string NormalizeFramerateLimitSelection(string? value)
    {
        var normalized = NormalizeAutoValue(value);
        return string.Equals(normalized, FramerateLimit120Hz, StringComparison.OrdinalIgnoreCase)
               || string.Equals(normalized, FramerateLimit144Hz, StringComparison.OrdinalIgnoreCase)
               || string.Equals(normalized, FramerateLimit165Hz, StringComparison.OrdinalIgnoreCase)
            ? normalized
            : OptiScalerCommonIniSettingsMaterializer.AutoValue;
    }

    public static bool IsFpsOverlayEnabled(string? value)
    {
        return string.Equals(NormalizeShowFpsMode(value), TrueValue, StringComparison.OrdinalIgnoreCase);
    }

    private static void AddExplicitEntry(
        ICollection<OptiScalerCommonIniEntry> entries,
        string section,
        string key,
        string value)
    {
        var normalized = NormalizeAutoValue(value);
        if (string.Equals(normalized, OptiScalerCommonIniSettingsMaterializer.AutoValue, StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(normalized))
        {
            return;
        }

        entries.Add(
            new OptiScalerCommonIniEntry
            {
                Section = section,
                Key = key,
                Value = normalized
            });
    }

    private static string ReadValue(
        IReadOnlyDictionary<string, string> materialized,
        string key)
    {
        return materialized.TryGetValue(key, out var value)
            ? NormalizeAutoValue(value)
            : OptiScalerCommonIniSettingsMaterializer.AutoValue;
    }
}
