namespace OptiClick.Core.OptiScaler;

public static class OptiScalerCommonIniSettingsMaterializer
{
    public const string AutoValue = "auto";
    public const string ShowFpsKey = "Menu:ShowFps";
    public const string MenuScaleKey = "Menu:Scale";
    public const string FpsOverlayTypeKey = "Menu:FpsOverlayType";
    public const string FpsOverlayPosKey = "Menu:FpsOverlayPos";
    public const string FpsScaleKey = "Menu:FpsScale";
    public const string DisableSplashKey = "Menu:DisableSplash";
    public const string FramerateLimitKey = "Framerate:FramerateLimit";

    private static readonly HashSet<string> SupportedCompositeKeys =
    [
        ShowFpsKey,
        MenuScaleKey,
        FpsOverlayTypeKey,
        FpsOverlayPosKey,
        FpsScaleKey,
        DisableSplashKey,
        FramerateLimitKey
    ];

    public static IReadOnlyDictionary<string, string> Materialize(
        OptiScalerCommonIniSettingsDocument? document)
    {
        return Materialize(document, gpuBundleKey: null, includeFsr411ManagedEntries: false);
    }

    public static IReadOnlyDictionary<string, string> Materialize(
        OptiScalerCommonIniSettingsDocument? document,
        string? gpuBundleKey)
    {
        return Materialize(document, gpuBundleKey, includeFsr411ManagedEntries: true);
    }

    private static IReadOnlyDictionary<string, string> Materialize(
        OptiScalerCommonIniSettingsDocument? document,
        string? gpuBundleKey,
        bool includeFsr411ManagedEntries)
    {
        var normalizedDocument = NormalizeDocument(document);
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in normalizedDocument.Entries)
        {
            var compositeKey = BuildCompositeKey(entry.Section, entry.Key);
            if (!IsSupportedCompositeKey(compositeKey))
            {
                continue;
            }

            var value = (entry.Value ?? "").Trim();
            if (string.IsNullOrWhiteSpace(value)
                || string.Equals(value, AutoValue, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            result[compositeKey] = value;
        }

        if (includeFsr411ManagedEntries)
        {
            foreach (var entry in OptiScalerFsr411Policy.BuildManagedEntries(
                         normalizedDocument.Fsr411Mode,
                         gpuBundleKey))
            {
                var compositeKey = BuildCompositeKey(entry.Section, entry.Key);
                var value = (entry.Value ?? "").Trim();
                if (string.IsNullOrWhiteSpace(value))
                {
                    continue;
                }

                if (string.Equals(value, AutoValue, StringComparison.OrdinalIgnoreCase)
                    && !CanWriteAuto(compositeKey))
                {
                    continue;
                }

                result[compositeKey] = value;
            }
        }

        return result;
    }

    public static OptiScalerCommonIniSettingsDocument NormalizeDocument(
        OptiScalerCommonIniSettingsDocument? document)
    {
        if (document is null)
        {
            return new OptiScalerCommonIniSettingsDocument();
        }

        var entriesByCompositeKey = new Dictionary<string, OptiScalerCommonIniEntry>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in document.Entries)
        {
            var section = (entry.Section ?? "").Trim();
            var key = (entry.Key ?? "").Trim();
            var value = (entry.Value ?? "").Trim();
            var compositeKey = BuildCompositeKey(section, key);
            if (!IsSupportedCompositeKey(compositeKey)
                || string.IsNullOrWhiteSpace(value)
                || string.Equals(value, AutoValue, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            entriesByCompositeKey[compositeKey] = new OptiScalerCommonIniEntry
            {
                Section = section,
                Key = key,
                Value = value
            };
        }

        return new OptiScalerCommonIniSettingsDocument
        {
            Version = 1,
            Fsr411Mode = OptiScalerFsr411Policy.NormalizeMode(document.Fsr411Mode),
            Entries = entriesByCompositeKey
                .OrderBy(static pair => pair.Key, StringComparer.OrdinalIgnoreCase)
                .Select(static pair => pair.Value)
                .ToArray()
        };
    }

    public static string BuildCompositeKey(string? section, string? key)
    {
        var normalizedSection = (section ?? "").Trim();
        var normalizedKey = (key ?? "").Trim();
        return string.IsNullOrWhiteSpace(normalizedSection) || string.IsNullOrWhiteSpace(normalizedKey)
            ? ""
            : $"{normalizedSection}:{normalizedKey}";
    }

    public static bool IsSupportedCompositeKey(string? compositeKey)
    {
        var normalized = (compositeKey ?? "").Trim();
        return !string.IsNullOrWhiteSpace(normalized)
               && SupportedCompositeKeys.Contains(normalized);
    }

    public static bool CanWriteAuto(string? compositeKey)
    {
        var normalized = (compositeKey ?? "").Trim();
        return string.Equals(normalized, OptiScalerFsr411IniKeys.Fsr4ForceModelCompositeKey, StringComparison.OrdinalIgnoreCase)
               || string.Equals(normalized, OptiScalerFsr411IniKeys.LoadCustomAmdxc64OnRdna2CompositeKey, StringComparison.OrdinalIgnoreCase);
    }
}
