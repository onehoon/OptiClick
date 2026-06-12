using System.Collections.Generic;

namespace OptiClick.Core.OptiScaler;

public sealed class OptiScalerSettingsApplicationService : IOptiScalerSettingsApplicationService
{
    private readonly IOptiScalerCommonIniSettingsStore _commonIniSettingsStore;
    private readonly IOptiScalerVariantPreferenceWriter? _variantPreferenceWriter;

    public OptiScalerSettingsApplicationService(
        IOptiScalerCommonIniSettingsStore commonIniSettingsStore,
        IOptiScalerVariantPreferenceWriter? variantPreferenceWriter = null)
    {
        _commonIniSettingsStore = commonIniSettingsStore
                                  ?? throw new ArgumentNullException(nameof(commonIniSettingsStore));
        _variantPreferenceWriter = variantPreferenceWriter;
    }

    public OptiScalerCommonIniSettingsDocument LoadCommonIniSettings()
    {
        return NormalizeDocument(_commonIniSettingsStore.Load());
    }

    public void SaveCommonIniSettings(OptiScalerCommonIniSettingsDocument settings)
    {
        _commonIniSettingsStore.Save(NormalizeDocument(settings));
    }

    public OptiScalerSettingsApplyResult ApplySettings(OptiScalerSettingsApplyRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var normalizedVariant = OptiScalerVariantPreference.NormalizeOrDefault(request.SelectedVariantPreference);
        var normalizedSettings = NormalizeDocument(request.CommonIniSettings);
        _commonIniSettingsStore.Save(normalizedSettings);
        _variantPreferenceWriter?.WriteVariantPreference(request.LanguagePreference, normalizedVariant);

        return new OptiScalerSettingsApplyResult
        {
            SelectedVariantPreference = normalizedVariant,
            CommonIniSettings = normalizedSettings
        };
    }

    public OptiScalerIniApplyContext CreateIniApplyContext(
        IReadOnlyDictionary<string, string>? gameOptiScalerIniSettings = null)
    {
        return new OptiScalerIniApplyContext
        {
            GameOptiScalerIniSettings = gameOptiScalerIniSettings
                                        ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
            CommonOptiScalerIniSettings = LoadCommonIniSettings()
        };
    }

    private static OptiScalerCommonIniSettingsDocument NormalizeDocument(
        OptiScalerCommonIniSettingsDocument? settings)
    {
        return OptiScalerCommonIniSettingsMaterializer.NormalizeDocument(settings);
    }
}
