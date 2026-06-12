using OptiClick.Wpf.Install.Execution;
using OptiClick.Wpf.Install.Flow;
using OptiClick.Wpf.Logging;
using OptiClick.Wpf.ViewModels;
using OptiClick.Wpf.ViewModels.DependencyComposition;

namespace OptiClick.Wpf.Composition.Modules;

internal sealed record InstallModuleCompositionServices
{
    public required InstallExecutionCoordinator InstallExecutionCoordinator { get; init; }
    public required UninstallFlowCoordinator UninstallFlowCoordinator { get; init; }
    public required MainInstallArchiveReadinessController MainInstallArchiveReadinessController { get; init; }
    public required MainInstallPreparationController MainInstallPreparationController { get; init; }
    public required MainInstallExecutionBridge MainInstallExecutionBridge { get; init; }
    public required MainInstallInteractionController MainInstallInteractionController { get; init; }
    public required MainUninstallInteractionController MainUninstallInteractionController { get; init; }
    public required MainInstallCompletionController MainInstallCompletionController { get; init; }
}

internal static class InstallModuleComposition
{
    public static InstallModuleCompositionServices Compose(
        MainViewModelAppDependencies appDependencies,
        InstallDependencyComposition installComposition,
        ShellModuleCompositionServices shellComposition,
        IAppLogger appLogger)
    {
        ArgumentNullException.ThrowIfNull(appDependencies);
        ArgumentNullException.ThrowIfNull(installComposition);
        ArgumentNullException.ThrowIfNull(shellComposition);
        ArgumentNullException.ThrowIfNull(appLogger);

        var installExecutionCoordinator = appDependencies.InstallExecutionCoordinator
                                          ?? new InstallExecutionCoordinator(installComposition.InstallFlowController);
        var uninstallFlowCoordinator = appDependencies.UninstallFlowCoordinator
                                       ?? new UninstallFlowCoordinator(
                                           installComposition.OptiClickUninstallPlanBuilder,
                                           installComposition.OptiClickUninstallExecutor,
                                           shellComposition.ShellDialogServices.DialogPresenter,
                                           appLogger);
        var mainInstallArchiveReadinessController = appDependencies.MainInstallArchiveReadinessController
                                                    ?? new MainInstallArchiveReadinessController();
        var mainInstallPreparationController = appDependencies.MainInstallPreparationController
                                               ?? new MainInstallPreparationController();
        var mainInstallExecutionBridge = appDependencies.MainInstallExecutionBridge
                                         ?? new MainInstallExecutionBridge();
        var mainInstallInteractionController = appDependencies.MainInstallInteractionController
                                               ?? new MainInstallInteractionController();
        var mainUninstallInteractionController = appDependencies.MainUninstallInteractionController
                                                 ?? new MainUninstallInteractionController();
        var mainInstallCompletionController = appDependencies.MainInstallCompletionController
                                              ?? new MainInstallCompletionController();

        return new InstallModuleCompositionServices
        {
            InstallExecutionCoordinator = installExecutionCoordinator,
            UninstallFlowCoordinator = uninstallFlowCoordinator,
            MainInstallArchiveReadinessController = mainInstallArchiveReadinessController,
            MainInstallPreparationController = mainInstallPreparationController,
            MainInstallExecutionBridge = mainInstallExecutionBridge,
            MainInstallInteractionController = mainInstallInteractionController,
            MainUninstallInteractionController = mainUninstallInteractionController,
            MainInstallCompletionController = mainInstallCompletionController
        };
    }
}
