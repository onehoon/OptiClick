using OptiClick.Wpf.Composition.Modules;

namespace OptiClick.Wpf.ViewModels.DependencyComposition;

internal static class MainViewModelFallbackDependencyValidationComposer
{
    public static MainViewModelRequiredDependencies ValidateRequired(MainViewModelRequiredDependencies? required)
    {
        ArgumentNullException.ThrowIfNull(required);
        ArgumentNullException.ThrowIfNull(required.DialogService);
        ArgumentNullException.ThrowIfNull(required.RuntimeContextProvider);
        ArgumentNullException.ThrowIfNull(required.LanguageProvider);
        ArgumentNullException.ThrowIfNull(required.MockDataProvider);
        return required;
    }

    public static void ValidateFallbackPolicy(
        MainViewModelRuntimeDependencies runtimeDependencies,
        MainViewModelScanDependencies scanDependencies,
        MainViewModelInstallDependencies installDependencies,
        MainViewModelAppDependencies appDependencies,
        bool allowFallbackResolution)
    {
        if (allowFallbackResolution)
        {
            return;
        }

        EnsureExplicitRuntimeDependencies(runtimeDependencies);
        EnsureExplicitScanDependencies(scanDependencies);
        EnsureExplicitInstallDependencies(installDependencies);
        EnsureExplicitAppDependencies(appDependencies);
    }

    private static void EnsureExplicitRuntimeDependencies(MainViewModelRuntimeDependencies runtimeDependencies)
    {
        EnsureExplicitDependency(runtimeDependencies.RuntimeContextFlowController, $"{nameof(MainViewModelRuntimeDependencies)}.{nameof(MainViewModelRuntimeDependencies.RuntimeContextFlowController)}");
        EnsureExplicitDependency(runtimeDependencies.DeviceIdentityRulesFlowController, $"{nameof(MainViewModelRuntimeDependencies)}.{nameof(MainViewModelRuntimeDependencies.DeviceIdentityRulesFlowController)}");
        EnsureExplicitDependency(runtimeDependencies.RuntimeCatalogFlowController, $"{nameof(MainViewModelRuntimeDependencies)}.{nameof(MainViewModelRuntimeDependencies.RuntimeCatalogFlowController)}");
        EnsureExplicitDependency(runtimeDependencies.RuntimeEndpointStatusPresenter, $"{nameof(MainViewModelRuntimeDependencies)}.{nameof(MainViewModelRuntimeDependencies.RuntimeEndpointStatusPresenter)}");
        EnsureExplicitDependency(runtimeDependencies.GpuBundleManifestClient, $"{nameof(MainViewModelRuntimeDependencies)}.{nameof(MainViewModelRuntimeDependencies.GpuBundleManifestClient)}");
        EnsureExplicitDependency(runtimeDependencies.GpuBundleManifestRuleResolver, $"{nameof(MainViewModelRuntimeDependencies)}.{nameof(MainViewModelRuntimeDependencies.GpuBundleManifestRuleResolver)}");
    }

    private static void EnsureExplicitScanDependencies(MainViewModelScanDependencies scanDependencies)
    {
        EnsureExplicitDependency(scanDependencies.ScanFlowController, $"{nameof(MainViewModelScanDependencies)}.{nameof(MainViewModelScanDependencies.ScanFlowController)}");
    }

    private static void EnsureExplicitInstallDependencies(MainViewModelInstallDependencies installDependencies)
    {
        EnsureExplicitDependency(installDependencies.ArchiveReadinessFlowController, $"{nameof(MainViewModelInstallDependencies)}.{nameof(MainViewModelInstallDependencies.ArchiveReadinessFlowController)}");
        EnsureExplicitDependency(installDependencies.InstallFlowController, $"{nameof(MainViewModelInstallDependencies)}.{nameof(MainViewModelInstallDependencies.InstallFlowController)}");
        EnsureExplicitDependency(installDependencies.InstallPopupPresenter, $"{nameof(MainViewModelInstallDependencies)}.{nameof(MainViewModelInstallDependencies.InstallPopupPresenter)}");
        EnsureExplicitDependency(installDependencies.OptiClickUninstallPlanBuilder, $"{nameof(MainViewModelInstallDependencies)}.{nameof(MainViewModelInstallDependencies.OptiClickUninstallPlanBuilder)}");
        EnsureExplicitDependency(installDependencies.OptiClickUninstallExecutor, $"{nameof(MainViewModelInstallDependencies)}.{nameof(MainViewModelInstallDependencies.OptiClickUninstallExecutor)}");
    }

    private static void EnsureExplicitAppDependencies(MainViewModelAppDependencies appDependencies)
    {
        var shellDependencyBundles = MainViewModelShellDependencyNormalizer.Normalize(appDependencies);
        var shellDialogs = shellDependencyBundles.Dialogs;

        EnsureExplicitDependency(appDependencies.StartupAnnouncementFlowController, nameof(MainViewModelAppDependencies.StartupAnnouncementFlowController));
        EnsureExplicitDependency(
            shellDialogs.InstallManagementDialogHost,
            nameof(MainViewModelAppDependencies.InstallManagementDialogHost));
        EnsureExplicitDependency(
            shellDialogs.InstallManagementDialogService,
            nameof(MainViewModelAppDependencies.InstallManagementDialogService));
        EnsureExplicitDependency(appDependencies.AppVersionProvider, nameof(MainViewModelAppDependencies.AppVersionProvider));
        EnsureExplicitDependency(appDependencies.AppUpdateFlowController, nameof(MainViewModelAppDependencies.AppUpdateFlowController));
        EnsureExplicitDependency(appDependencies.AppLogger, nameof(MainViewModelAppDependencies.AppLogger));
        EnsureExplicitDependency(appDependencies.LocalDataPathProvider, nameof(MainViewModelAppDependencies.LocalDataPathProvider));
        EnsureExplicitDependency(appDependencies.AppStringsProvider, nameof(MainViewModelAppDependencies.AppStringsProvider));
        EnsureExplicitDependency(appDependencies.FirstRunStateStore, nameof(MainViewModelAppDependencies.FirstRunStateStore));
        EnsureExplicitDependency(
            shellDialogs.DialogHost,
            nameof(MainViewModelAppDependencies.DialogHost));
        EnsureExplicitDependency(
            appDependencies.CoverCacheBootstrapService,
            nameof(MainViewModelAppDependencies.CoverCacheBootstrapService));
        EnsureExplicitDependency(
            appDependencies.StartupBackgroundTaskManager,
            nameof(MainViewModelAppDependencies.StartupBackgroundTaskManager));
        EnsureExplicitDependency(
            appDependencies.ArchiveReadinessRefreshCoordinator,
            nameof(MainViewModelAppDependencies.ArchiveReadinessRefreshCoordinator));
        EnsureExplicitDependency(
            appDependencies.ArchiveReadinessWarmupController,
            nameof(MainViewModelAppDependencies.ArchiveReadinessWarmupController));
        EnsureExplicitDependency(
            appDependencies.StartupPreparationCoordinator,
            nameof(MainViewModelAppDependencies.StartupPreparationCoordinator));
        EnsureExplicitDependency(
            appDependencies.StartupFlowCoordinator,
            nameof(MainViewModelAppDependencies.StartupFlowCoordinator));
        EnsureExplicitOptiScalerSettingsDependency(appDependencies);
    }

    private static void EnsureExplicitDependency(object? dependency, string dependencyName)
    {
        if (dependency is not null)
        {
            return;
        }

        throw new InvalidOperationException(
            $"MainViewModel dependency '{dependencyName}' must be explicitly provided when fallback resolution is disabled.");
    }

    private static void EnsureExplicitOptiScalerSettingsDependency(MainViewModelAppDependencies appDependencies)
    {
        if (appDependencies.OptiScalerSettingsApplicationService is not null
            || appDependencies.MainOptiScalerSettingsController is not null)
        {
            return;
        }

        throw new InvalidOperationException(
            $"MainViewModel dependency '{nameof(MainViewModelAppDependencies.OptiScalerSettingsApplicationService)}' must be explicitly provided when fallback resolution is disabled.");
    }
}
