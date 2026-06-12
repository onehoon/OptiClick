using OptiClick.Core.Abstractions;
using OptiClick.Wpf.Composition;
using OptiClick.Wpf.Install.Presentation;
using OptiClick.Wpf.Logging;
using OptiClick.Wpf.Shell.Selection;
using OptiClick.Wpf.ViewModels.DependencyComposition;

namespace OptiClick.Wpf.ViewModels;

internal static partial class MainViewModelFallbackDependencyResolver
{
    private static AppDependencyComposition ResolveAppDependencies(
        MainViewModelAppDependencies appDependencies,
        MainViewModelRequiredDependencies requiredDependencies,
        IAppLogger appLogger,
        MainViewModelRuntimeDependencies runtimeDependencies,
        RuntimeDependencyComposition runtimeComposition,
        ScanDependencyComposition scanComposition,
        InstallDependencyComposition installComposition,
        bool allowFallbackResolution)
    {
        var fallbackServices = allowFallbackResolution
            ? MainViewModelAppFallbackFactory.Create(appDependencies, appLogger)
            : MainViewModelAppFallbackFactory.CreateExplicit(appDependencies);
        return MainViewModelFallbackAppDependencyComposer.Compose(
            appDependencies,
            requiredDependencies,
            appLogger,
            fallbackServices,
            runtimeDependencies,
            runtimeComposition,
            scanComposition,
            installComposition);
    }
}
