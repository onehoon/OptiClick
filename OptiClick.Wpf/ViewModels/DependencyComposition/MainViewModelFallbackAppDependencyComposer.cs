using OptiClick.Core.Abstractions;
using OptiClick.Wpf.Composition.Modules;
using OptiClick.Wpf.Install.Archives;
using OptiClick.Wpf.Install.Execution;
using OptiClick.Wpf.Install.Flow;
using OptiClick.Wpf.Install.Presentation;
using OptiClick.Wpf.Logging;
using OptiClick.Wpf.Services;
using OptiClick.Wpf.Shell.Actions;
using OptiClick.Wpf.Shell.Dialogs;
using OptiClick.Wpf.Shell.Flow;
using OptiClick.Wpf.Shell.Games;
using OptiClick.Wpf.Shell.Gpu;
using OptiClick.Wpf.Shell.Localization;
using OptiClick.Wpf.Shell.Navigation;
using OptiClick.Wpf.Shell.Runtime;
using OptiClick.Wpf.Shell.Scan;
using OptiClick.Wpf.Shell.Settings;
using OptiClick.Wpf.Shell.Startup;
using OptiClick.Wpf.Shell.Support;
using OptiClick.Wpf.Shell.Selection;
using OptiClick.Wpf.Shell.Update;
using OptiClick.Wpf.Shell.Wiki;
using OptiClick.Wpf.ViewModels;
using OptiClick.Wpf.ViewModels.Sections;
using OptiClick.Wpf.ViewModels.Shell;

namespace OptiClick.Wpf.ViewModels.DependencyComposition;

internal static class MainViewModelFallbackAppDependencyComposer
{
    public static AppDependencyComposition Compose(
        MainViewModelAppDependencies appDependencies,
        MainViewModelRequiredDependencies requiredDependencies,
        IAppLogger appLogger,
        MainViewModelAppFallbackServices fallbackServices,
        MainViewModelRuntimeDependencies runtimeDependencies,
        RuntimeDependencyComposition runtimeComposition,
        ScanDependencyComposition scanComposition,
        InstallDependencyComposition installComposition)
    {
        ArgumentNullException.ThrowIfNull(fallbackServices);

        var localDataPathProvider = fallbackServices.LocalDataPathProvider;
        var appStringsProvider = appDependencies.AppStringsProvider
            ?? throw new InvalidOperationException(
                $"MainViewModel dependency '{nameof(MainViewModelAppDependencies.AppStringsProvider)}' must be resolved before composing app dependencies.");
        var firstRunStateStore = fallbackServices.FirstRunStateStore;
        var startupComposition = StartupModuleComposition.Compose(
            appDependencies,
            fallbackServices);
        var shellComposition = ShellModuleComposition.Compose(
            appDependencies,
            requiredDependencies,
            fallbackServices,
            installComposition,
            startupComposition,
            appLogger);
        var runtimeModuleComposition = RuntimeModuleComposition.Compose(
            appDependencies,
            runtimeDependencies,
            runtimeComposition,
            shellComposition.ShellUiServices.FlowLogDispatcher);
        var installModuleComposition = InstallModuleComposition.Compose(
            appDependencies,
            installComposition,
            shellComposition,
            appLogger);

        var appVersionProvider = fallbackServices.AppVersionProvider;
        var resolvedAppUpdateService = fallbackServices.AppUpdateService;
        var resolvedAppUpdateExecutionService = fallbackServices.AppUpdateExecutionService;
        var resolvedExternalUrlLauncher = fallbackServices.ExternalUrlLauncher;
        var resolvedAppUpdateDialogPresenter = appDependencies.AppUpdateDialogPresenter ?? new AppUpdateDialogPresenter();
        var appUpdateFlowController = appDependencies.AppUpdateFlowController ?? new AppUpdateFlowController(
            resolvedAppUpdateService,
            resolvedAppUpdateExecutionService,
            resolvedExternalUrlLauncher,
            resolvedAppUpdateDialogPresenter);
        var appUpdateCoordinator = appDependencies.AppUpdateCoordinator ?? new AppUpdateCoordinator(appUpdateFlowController);
        var settingsComposition = OptiScalerSettingsModuleComposition.Compose(
            appDependencies,
            fallbackServices);

        return new AppDependencyComposition
        {
            AppVersionProvider = appVersionProvider,
            AppUpdateFlowController = appUpdateFlowController,
            AppUpdateCoordinator = appUpdateCoordinator,
            ShellUiServices = shellComposition.ShellUiServices,
            ShellDialogServices = shellComposition.ShellDialogServices,
            ShellSelectionServices = shellComposition.ShellSelectionServices,
            ShellSupportServices = shellComposition.ShellSupportServices,
            ShellSectionServices = shellComposition.ShellSectionServices,
            RuntimeServices = runtimeModuleComposition,
            StartupServices = startupComposition,
            InstallServices = installModuleComposition,
            OptiScalerSettingsServices = settingsComposition,
            LocalDataPathProvider = localDataPathProvider,
            AppStringsProvider = appStringsProvider,
            FirstRunStateStore = firstRunStateStore,
            ExternalUrlLauncher = resolvedExternalUrlLauncher
        };
    }

    public static void EnsureShellChromeNavigationState(
        ShellChromeViewModels shellChrome,
        ShellNavigationState navigationState)
    {
        if (ReferenceEquals(shellChrome.NavigationState, navigationState))
        {
            return;
        }

        throw new InvalidOperationException(
            "MainViewModel dependency 'ShellChrome.NavigationState' must reference the same instance as 'NavigationState'.");
    }
}
