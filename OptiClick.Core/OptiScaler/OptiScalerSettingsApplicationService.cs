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

    public OptiScalerSettingsPersistenceResult SaveCommonIniSettings(OptiScalerCommonIniSettingsDocument settings)
    {
        return _commonIniSettingsStore.Save(NormalizeDocument(settings));
    }

    public OptiScalerSettingsApplyResult ApplySettings(OptiScalerSettingsApplyRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var normalizedVariant = OptiScalerVariantPreference.NormalizeOrDefault(request.SelectedVariantPreference);
        var normalizedSettings = NormalizeDocument(request.CommonIniSettings);
        var saveResult = _commonIniSettingsStore.Save(normalizedSettings);
        if (!saveResult.IsSuccess)
        {
            return new OptiScalerSettingsApplyResult
            {
                IsSuccess = false,
                ErrorCode = saveResult.ErrorCode,
                ErrorMessage = saveResult.ErrorMessage,
                SelectedVariantPreference = normalizedVariant,
                CommonIniSettings = normalizedSettings
            };
        }

        _variantPreferenceWriter?.WriteVariantPreference(request.LanguagePreference, normalizedVariant);

        return new OptiScalerSettingsApplyResult
        {
            IsSuccess = true,
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
