using OptiClick.Core.OptiScaler;

namespace OptiClick.Core.OptiScaler;

public interface IOptiScalerSectionSettingsCodec
{
    string NormalizeShowFpsMode(string? value);

    string NormalizeAutoValue(string? value);

    string NormalizeFpsOverlayTypeSelection(string? value);

    string NormalizeFpsOverlayPositionSelection(string? value);

    string NormalizeFramerateLimitSelection(string? value);

    string NormalizeVariant(string? value);

    bool IsFpsOverlayEnabled(string? value);

    OptiScalerCommonIniSettingsDocument ToDocument(OptiScalerCommonIniSettingsDraft draft);
}

public sealed class CoreOptiScalerSectionSettingsCodec : IOptiScalerSectionSettingsCodec
{
    public string NormalizeShowFpsMode(string? value)
    {
        return OptiScalerCommonIniSettingsMapper.NormalizeShowFpsMode(value);
    }

    public string NormalizeAutoValue(string? value)
    {
        return OptiScalerCommonIniSettingsMapper.NormalizeAutoValue(value);
    }

    public string NormalizeFpsOverlayTypeSelection(string? value)
    {
        return OptiScalerCommonIniSettingsMapper.NormalizeFpsOverlayTypeSelection(value);
    }

    public string NormalizeFpsOverlayPositionSelection(string? value)
    {
        return OptiScalerCommonIniSettingsMapper.NormalizeFpsOverlayPositionSelection(value);
    }

    public string NormalizeFramerateLimitSelection(string? value)
    {
        return OptiScalerCommonIniSettingsMapper.NormalizeFramerateLimitSelection(value);
    }

    public string NormalizeVariant(string? value)
    {
        return OptiScalerCommonIniSettingsDraftState.NormalizeVariant(value);
    }

    public bool IsFpsOverlayEnabled(string? value)
    {
        return OptiScalerCommonIniSettingsMapper.IsFpsOverlayEnabled(value);
    }

    public OptiScalerCommonIniSettingsDocument ToDocument(OptiScalerCommonIniSettingsDraft draft)
    {
        return OptiScalerCommonIniSettingsMapper.ToDocument(draft);
    }
}
