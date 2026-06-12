using OptiClick.Wpf.Composition;
using OptiClick.Wpf.Composition.Modules;
using OptiClick.Wpf.Configuration;
using OptiClick.Wpf.Install.Archives;
using OptiClick.Wpf.Install.Execution;
using OptiClick.Wpf.Install.FileSystem;
using OptiClick.Wpf.Install.Flow;
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
using OptiClick.Wpf.Shell.Selection;
using OptiClick.Wpf.Shell.Settings;
using OptiClick.Wpf.Shell.Startup;
using OptiClick.Wpf.Shell.Update;
using OptiClick.Wpf.Shell.Wiki;
using OptiClick.Wpf.ViewModels;
using OptiClick.Wpf.ViewModels.Sections;
using OptiClick.Wpf.ViewModels.Sections.Scan;
using OptiClick.Wpf.ViewModels.Shell;

namespace OptiClick.Wpf.Composition.Features;

internal static class MainWindowInstallFeatureComposer
{
    public static MainInstallResolvedDependencies Compose(
        InstallCompositionServices install,
        DialogPresenter dialogPresenter,
        IAppLogger appLogger,
        AppSharedServices app)
    {
        return new MainInstallResolvedDependencies
        {
            GameSelectionFlowController = install.GameSelectionFlowController,
            ArchiveReadinessFlowController = install.ArchiveReadinessFlowController,
            InstallFlowController = install.InstallFlowController,
            InstallPopupPresenter = install.InstallPopupPresenter,
            OptiClickUninstallPlanBuilder = install.OptiClickUninstallPlanBuilder,
            OptiClickUninstallExecutor = install.OptiClickUninstallExecutor,
            InstallExecutionCoordinator = new InstallExecutionCoordinator(install.InstallFlowController),
            UninstallFlowCoordinator = new UninstallFlowCoordinator(
                install.OptiClickUninstallPlanBuilder,
                install.OptiClickUninstallExecutor,
                dialogPresenter,
                appLogger),
            MainInstallArchiveReadinessController = new MainInstallArchiveReadinessController(),
            MainInstallPreparationController = new MainInstallPreparationController(),
            MainInstallExecutionBridge = new MainInstallExecutionBridge(),
            MainInstallInteractionController = new MainInstallInteractionController(),
            MainUninstallInteractionController = new MainUninstallInteractionController(),
            MainInstallCompletionController = new MainInstallCompletionController(),
            MainOptiScalerSettingsController = new MainOptiScalerSettingsController(
                app.OptiScalerSettingsApplicationService)
        };
    }
}
