using OptiClick.Core.OptiScaler;

namespace OptiClick.Wpf.ViewModels.Sections.OptiScaler;

public interface IOptiScalerSectionSaveHandler
{
    OptiScalerSectionSaveResult Save(OptiScalerSectionSaveRequest request);
}

public sealed record OptiScalerSectionSaveRequest(
    string SelectedVariant,
    OptiScalerCommonIniSettingsDocument CommonIniSettings);

public sealed record OptiScalerSectionSaveResult(
    bool IsSuccess,
    string SelectedVariant,
    OptiScalerCommonIniSettingsDocument CommonIniSettings,
    string ErrorCode = "",
    string ErrorMessage = "");
