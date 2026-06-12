using System.Collections.Generic;

namespace OptiClick.Core.OptiScaler;

public sealed record OptiScalerIniApplyContext
{
    public IReadOnlyDictionary<string, string> GameOptiScalerIniSettings { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    public OptiScalerCommonIniSettingsDocument CommonOptiScalerIniSettings { get; init; } =
        new();

    public IReadOnlyDictionary<string, string> NormalizeGameOptiScalerIniSettings()
    {
        return NormalizeIniSettings(GameOptiScalerIniSettings);
    }

    public IReadOnlyDictionary<string, string> NormalizeCommonOptiScalerIniSettings()
    {
        return NormalizeIniSettings(
            OptiScalerCommonIniSettingsMaterializer.Materialize(CommonOptiScalerIniSettings));
    }

    public static IReadOnlyDictionary<string, string> NormalizeIniSettings(IReadOnlyDictionary<string, string>? settings)
    {
        if (settings is null || settings.Count == 0)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        var normalized = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in settings)
        {
            var normalizedKey = (key ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(normalizedKey))
            {
                continue;
            }

            normalized[normalizedKey] = value ?? string.Empty;
        }

        return normalized;
    }
}
