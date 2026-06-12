using OptiClick.Core.OptiScaler;

namespace OptiClick.Wpf.ViewModels.Ports.ShellInteractionContext.OptiScaler;

internal interface IMainOptiScalerSettingsInteractionAccess
{
    string NormalizeOptiScalerVariantPreference(string? preference);
    void ApplySavedOptiScalerSettings(
        string optiScalerVariant,
        OptiScalerCommonIniSettingsDocument? settings);
}
