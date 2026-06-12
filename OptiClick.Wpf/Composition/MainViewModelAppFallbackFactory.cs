using OptiClick.Core.OptiScaler;
using OptiClick.Infrastructure.FileSystem;
using OptiClick.Infrastructure.OptiScaler;
using OptiClick.Infrastructure.Storage;
using OptiClick.Wpf.Logging;
using OptiClick.Wpf.Services;
using OptiClick.Wpf.ViewModels;
using OptiClick.Wpf.ViewModels.DependencyComposition;

namespace OptiClick.Wpf.Composition;

internal static class MainViewModelAppFallbackFactory
{
    public static MainViewModelAppFallbackServices Create(
        MainViewModelAppDependencies appDependencies,
        IAppLogger appLogger)
    {
        ArgumentNullException.ThrowIfNull(appDependencies);
        ArgumentNullException.ThrowIfNull(appLogger);

        var localDataPathProvider = appDependencies.LocalDataPathProvider ?? new AppLocalDataPathProvider();
        var userSettingsStore = appDependencies.UserSettingsStore ?? new AppUserSettingsStore(localDataPathProvider, appLogger);
        var firstRunStateStore = appDependencies.FirstRunStateStore ?? new FirstRunStateStore(localDataPathProvider, appLogger);
        var appVersionProvider = appDependencies.AppVersionProvider ?? new AssemblyAppVersionProvider();
        var appUpdateVersionComparer = appDependencies.AppUpdateVersionComparer ?? new AppUpdateVersionComparer();
        var appUpdateService = appDependencies.AppUpdateService ?? new AppUpdateService(appUpdateVersionComparer);
        var appUpdateExecutionService = appDependencies.AppUpdateExecutionService
                                        ?? new AppUpdateExecutionService(
                                            localDataPathProvider: localDataPathProvider,
                                            logger: appLogger);
        var externalUrlLauncher = appDependencies.ExternalUrlLauncher ?? new ExternalUrlLauncher(appLogger);
        var optiScalerCommonIniSettingsStore = new OptiScalerCommonIniSettingsJsonStore(
            localDataPathProvider,
            appLogger);
        var optiScalerSettingsApplicationService = new OptiScalerSettingsApplicationService(
            optiScalerCommonIniSettingsStore,
            new AppUserSettingsOptiScalerPreferenceWriter(userSettingsStore));

        return new MainViewModelAppFallbackServices
        {
            LocalDataPathProvider = localDataPathProvider,
            UserSettingsStore = userSettingsStore,
            FirstRunStateStore = firstRunStateStore,
            AppVersionProvider = appVersionProvider,
            AppUpdateService = appUpdateService,
            AppUpdateExecutionService = appUpdateExecutionService,
            ExternalUrlLauncher = externalUrlLauncher,
            OptiScalerSettingsApplicationService = optiScalerSettingsApplicationService
        };
    }

    public static MainViewModelAppFallbackServices CreateExplicit(
        MainViewModelAppDependencies appDependencies)
    {
        ArgumentNullException.ThrowIfNull(appDependencies);

        return new MainViewModelAppFallbackServices
        {
            LocalDataPathProvider = Require(
                appDependencies.LocalDataPathProvider,
                nameof(MainViewModelAppDependencies.LocalDataPathProvider)),
            UserSettingsStore = Require(
                appDependencies.UserSettingsStore,
                nameof(MainViewModelAppDependencies.UserSettingsStore)),
            FirstRunStateStore = Require(
                appDependencies.FirstRunStateStore,
                nameof(MainViewModelAppDependencies.FirstRunStateStore)),
            AppVersionProvider = Require(
                appDependencies.AppVersionProvider,
                nameof(MainViewModelAppDependencies.AppVersionProvider)),
            AppUpdateService = Require(
                appDependencies.AppUpdateService,
                nameof(MainViewModelAppDependencies.AppUpdateService)),
            AppUpdateExecutionService = Require(
                appDependencies.AppUpdateExecutionService,
                nameof(MainViewModelAppDependencies.AppUpdateExecutionService)),
            ExternalUrlLauncher = Require(
                appDependencies.ExternalUrlLauncher,
                nameof(MainViewModelAppDependencies.ExternalUrlLauncher)),
            OptiScalerSettingsApplicationService = Require(
                appDependencies.OptiScalerSettingsApplicationService,
                nameof(MainViewModelAppDependencies.OptiScalerSettingsApplicationService))
        };
    }

    private static T Require<T>(T? dependency, string dependencyName)
        where T : class
    {
        return dependency
               ?? throw new InvalidOperationException(
                   $"MainViewModel app dependency '{dependencyName}' must be provided when fallback resolution is disabled.");
    }
}
