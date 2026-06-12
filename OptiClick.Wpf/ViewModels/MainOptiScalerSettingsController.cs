using System;
using OptiClick.Core.OptiScaler;

namespace OptiClick.Wpf.ViewModels;

internal sealed class MainOptiScalerSettingsController
{
    private readonly IOptiScalerSettingsApplicationService _settingsApplicationService;

    public MainOptiScalerSettingsController(IOptiScalerSettingsApplicationService settingsApplicationService)
    {
        _settingsApplicationService = settingsApplicationService
                                      ?? throw new ArgumentNullException(nameof(settingsApplicationService));
    }

    public OptiScalerCommonIniSettingsDocument LoadCommonIniSettings()
    {
        return _settingsApplicationService.LoadCommonIniSettings();
    }

    public OptiScalerIniApplyContext CreateIniApplyContext()
    {
        return _settingsApplicationService.CreateIniApplyContext();
    }

    public OptiScalerSettingsApplyResult ApplySettings(MainOptiScalerSettingsContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return _settingsApplicationService.ApplySettings(new OptiScalerSettingsApplyRequest
        {
            SelectedVariantPreference = context.SelectedOptiScalerVariantOption,
            LanguagePreference = context.LanguagePreference,
            CommonIniSettings = context.CommonIniSettings
        });
    }

    public void SaveCommonIniSettings(OptiScalerCommonIniSettingsDocument commonIniSettings)
    {
        _settingsApplicationService.SaveCommonIniSettings(commonIniSettings);
    }
}

internal sealed record MainOptiScalerSettingsContext
{
    public required string SelectedOptiScalerVariantOption { get; init; }
    public required string LanguagePreference { get; init; }
    public required OptiScalerCommonIniSettingsDocument CommonIniSettings { get; init; }
}
