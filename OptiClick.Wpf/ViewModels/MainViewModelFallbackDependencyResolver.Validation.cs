using OptiClick.Wpf.ViewModels.Sections;
using OptiClick.Wpf.ViewModels.Shell;
using OptiClick.Wpf.Shell.Navigation;
using OptiClick.Wpf.ViewModels.DependencyComposition;

namespace OptiClick.Wpf.ViewModels;

internal static partial class MainViewModelFallbackDependencyResolver
{
    private static MainViewModelRequiredDependencies ValidateRequired(MainViewModelRequiredDependencies? required)
    {
        return MainViewModelFallbackDependencyValidationComposer.ValidateRequired(required);
    }

    private static void ValidateFallbackPolicy(
        MainViewModelRuntimeDependencies runtimeDependencies,
        MainViewModelScanDependencies scanDependencies,
        MainViewModelInstallDependencies installDependencies,
        MainViewModelAppDependencies appDependencies,
        bool allowFallbackResolution)
    {
        MainViewModelFallbackDependencyValidationComposer.ValidateFallbackPolicy(
            runtimeDependencies,
            scanDependencies,
            installDependencies,
            appDependencies,
            allowFallbackResolution);
    }

    private static void EnsureShellChromeNavigationState(
        ShellChromeViewModels shellChrome,
        ShellNavigationState navigationState)
    {
        MainViewModelFallbackAppDependencyComposer.EnsureShellChromeNavigationState(shellChrome, navigationState);
    }
}
