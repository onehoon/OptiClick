using OptiClick.Wpf.Composition;
using OptiClick.Wpf.Logging;
using OptiClick.Wpf.ViewModels.DependencyComposition;

namespace OptiClick.Wpf.ViewModels;

internal static partial class MainViewModelFallbackDependencyResolver
{
    private static InstallDependencyComposition ResolveInstallDependencies(
        MainViewModelInstallDependencies installDependencies,
        IAppLogger appLogger,
        bool allowFallbackResolution)
    {
        var fallbackServices = allowFallbackResolution
            ? MainViewModelInstallFallbackFactory.Create(installDependencies, appLogger)
            : MainViewModelInstallFallbackFactory.CreateExplicit(installDependencies);
        return MainViewModelFallbackInstallDependencyComposer.Compose(
            installDependencies,
            fallbackServices);
    }
}
