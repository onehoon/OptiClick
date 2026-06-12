using OptiClick.Core.Abstractions;
using OptiClick.Wpf.Install.Gates;
using OptiClick.Wpf.Install.Presentation;
using OptiClick.Wpf.Logging;
using OptiClick.Wpf.Composition.Modules;
using OptiClick.Wpf.Shell.Games;
using OptiClick.Wpf.Shell.Games.GpuBundle;
using OptiClick.Wpf.Shell.Gpu;
using OptiClick.Wpf.Shell.Runtime;
using OptiClick.Wpf.Shell.Runtime.DeviceIdentity;
using OptiClick.Wpf.Shell.RuntimeData;
using OptiClick.Wpf.ViewModels.DependencyComposition;

namespace OptiClick.Wpf.ViewModels;

internal static partial class MainViewModelFallbackDependencyResolver
{
    private static RuntimeDependencyComposition ResolveRuntimeDependencies(
        IRuntimeContextProvider runtimeContextProvider,
        MainViewModelRuntimeDependencies runtimeDependencies,
        IAppLogger appLogger,
        bool allowFallbackResolution)
    {
        var fallbackServices = allowFallbackResolution
            ? RuntimeModuleComposition.ComposeFallbackServices(
                runtimeContextProvider,
                runtimeDependencies,
                appLogger)
            : RuntimeModuleComposition.ComposeExplicitServices(runtimeDependencies);

        return MainViewModelFallbackRuntimeDependencyComposer.Compose(
            runtimeContextProvider,
            runtimeDependencies,
            appLogger,
            fallbackServices);
    }
}
