using OptiClick.Wpf.Install.Config;
using OptiClick.Wpf.Install.Flow;
using OptiClick.Wpf.Localization;
using OptiClick.Wpf.Logging;
using OptiClick.Wpf.Shell.RuntimeData;
using OptiClick.Wpf.ViewModels.Shell;
using OptiClick.Wpf.ViewModels.DependencyComposition;

namespace OptiClick.Wpf.ViewModels;

internal static partial class MainViewModelFallbackDependencyResolver
{
    public static MainViewModelCompositionDependencies Resolve(
        MainViewModelRequiredDependencies requiredDependencies,
        MainViewModelRuntimeDependencies? runtime = null,
        MainViewModelScanDependencies? scan = null,
        MainViewModelInstallDependencies? install = null,
        MainViewModelAppDependencies? app = null,
        bool allowFallbackResolution = true)
    {
        var required = ValidateRequired(requiredDependencies);
        var runtimeDependencies = runtime ?? new MainViewModelRuntimeDependencies();
        var scanDependencies = scan ?? new MainViewModelScanDependencies();
        var installDependencies = install ?? new MainViewModelInstallDependencies();
        var appDependencies = app ?? new MainViewModelAppDependencies();
        var appDependenciesWithFallback = appDependencies;
        if (allowFallbackResolution && appDependenciesWithFallback.AppStringsProvider is null)
        {
            appDependenciesWithFallback = appDependenciesWithFallback with
            {
                AppStringsProvider = new AppStringsProvider()
            };
        }

        ValidateFallbackPolicy(
            runtimeDependencies,
            scanDependencies,
            installDependencies,
            appDependenciesWithFallback,
            allowFallbackResolution);

        var appLogger = appDependenciesWithFallback.AppLogger ?? NullAppLogger.Instance;

        var runtimeComposition = ResolveRuntimeDependencies(
            required.RuntimeContextProvider,
            runtimeDependencies,
            appLogger,
            allowFallbackResolution);

        var scanComposition = ResolveScanDependencies(
            scanDependencies,
            runtimeDependencies);

        var installComposition = ResolveInstallDependencies(
            installDependencies,
            appLogger,
            allowFallbackResolution);

        var appComposition = ResolveAppDependencies(
            appDependenciesWithFallback,
            required,
            appLogger,
            runtimeDependencies,
            runtimeComposition,
            scanComposition,
            installComposition,
            allowFallbackResolution);

        return MainViewModelFallbackResolvedDependencyComposer.Compose(
            required,
            runtimeComposition,
            scanComposition,
            installComposition,
            appComposition,
            appLogger);
    }
}
