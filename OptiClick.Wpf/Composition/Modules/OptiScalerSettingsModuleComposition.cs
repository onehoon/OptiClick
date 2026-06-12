using OptiClick.Wpf.ViewModels;
using OptiClick.Wpf.ViewModels.DependencyComposition;

namespace OptiClick.Wpf.Composition.Modules;

internal sealed record OptiScalerSettingsModuleCompositionServices
{
    public required MainOptiScalerSettingsController MainOptiScalerSettingsController { get; init; }
}

internal static class OptiScalerSettingsModuleComposition
{
    public static OptiScalerSettingsModuleCompositionServices Compose(
        MainViewModelAppDependencies appDependencies,
        MainViewModelAppFallbackServices fallbackServices)
    {
        ArgumentNullException.ThrowIfNull(appDependencies);
        ArgumentNullException.ThrowIfNull(fallbackServices);

        var mainOptiScalerSettingsController = appDependencies.MainOptiScalerSettingsController
                                               ?? new MainOptiScalerSettingsController(
                                                   fallbackServices.OptiScalerSettingsApplicationService);

        return new OptiScalerSettingsModuleCompositionServices
        {
            MainOptiScalerSettingsController = mainOptiScalerSettingsController
        };
    }
}
