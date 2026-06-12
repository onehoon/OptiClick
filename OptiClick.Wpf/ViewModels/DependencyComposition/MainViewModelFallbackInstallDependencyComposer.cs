using OptiClick.Core.Abstractions;
using OptiClick.Wpf.Install.Execution;
using OptiClick.Wpf.Install.Archives;
using OptiClick.Wpf.Composition.Modules;
using OptiClick.Wpf.Install.Planning;
using OptiClick.Wpf.Install.Presentation;
using OptiClick.Wpf.Install.UiState;
using OptiClick.Wpf.Shell.Selection;

namespace OptiClick.Wpf.ViewModels.DependencyComposition;

internal static class MainViewModelFallbackInstallDependencyComposer
{
    public static InstallDependencyComposition Compose(
        MainViewModelInstallDependencies installDependencies,
        MainViewModelInstallFallbackServices fallbackServices)
    {
        ArgumentNullException.ThrowIfNull(fallbackServices);

        var resolvedInstallSelectionRequestBuilder = installDependencies.InstallSelectionRequestBuilder ?? new InstallSelectionRequestBuilder(installDependencies.InstallStatusResolver);
        var gameSelectionFlowController = installDependencies.GameSelectionFlowController
                                          ?? new GameSelectionFlowController(
                                              installDependencies.InstallSelectionBridge,
                                              resolvedInstallSelectionRequestBuilder);
        var resolvedInstallPlanInputBuilder = installDependencies.InstallPlanInputBuilder ?? new InstallPlanInputBuilder();
        var resolvedComponentInstallContextBuilder = installDependencies.ComponentInstallContextBuilder ?? new ComponentInstallContextBuilder();
        var resolvedInstallCompletionMessageBuilder = installDependencies.InstallCompletionMessageBuilder ?? new InstallCompletionMessageBuilder();
        var installPopupPresenter = installDependencies.InstallPopupPresenter ?? new InstallPopupPresenter();
        var installRejectionPresentationResolver = installDependencies.InstallRejectionPresentationResolver
                                                   ?? new InstallRejectionPresentationResolver();
        var archiveReadinessFlowController = installDependencies.ArchiveReadinessFlowController
                                                 ?? new ArchiveReadinessFlowController(installDependencies.ArchivePreparationCoordinator);
        var configApplyComposition = ConfigApplyCompositionFactory.Create(new ConfigApplyCompositionRequest
        {
            ConfigProfileApplier = installDependencies.ConfigProfileApplier,
            IniProfileEditor = installDependencies.IniProfileEditor,
            ConfigApplyFlowController = installDependencies.ConfigApplyFlowController,
            InstallResultApplier = installDependencies.InstallResultApplier,
            InstallResultPresentationResolver = installDependencies.InstallResultPresentationResolver,
            InstallCompletionMessageBuilder = resolvedInstallCompletionMessageBuilder
        });
        var resolvedComponentInstallCoordinator = installDependencies.ComponentInstallCoordinator
                                                  ?? fallbackServices.ComponentInstallCoordinator;
        var resolvedInstallStartGateResolver = installDependencies.InstallStartGateResolver
                                               ?? fallbackServices.InstallStartGateResolver;
        var installFlowComposition = InstallFlowCompositionFactory.Create(new InstallFlowCompositionRequest
        {
            InstallPlanBuilder = installDependencies.InstallPlanBuilder ?? fallbackServices.InstallPlanBuilder,
            ComponentInstallParityReviewBuilder = installDependencies.ComponentInstallParityReviewBuilder
                                                  ?? fallbackServices.ComponentInstallParityReviewBuilder,
            InstallPlanInputBuilder = resolvedInstallPlanInputBuilder,
            InstallStartGateResolver = resolvedInstallStartGateResolver,
            ComponentInstallContextBuilder = resolvedComponentInstallContextBuilder,
            ComponentInstallCoordinator = resolvedComponentInstallCoordinator,
            InstallResultApplier = configApplyComposition.InstallResultApplier,
            InstallPopupPresenter = installPopupPresenter,
            InstallRejectionPresentationResolver = installRejectionPresentationResolver
        });
        var installFlowController = installDependencies.InstallFlowController
                                    ?? installFlowComposition.InstallFlowController;
        var optiClickUninstallPlanBuilder = fallbackServices.OptiClickUninstallPlanBuilder;
        var optiClickUninstallExecutor = fallbackServices.OptiClickUninstallExecutor;

        return new InstallDependencyComposition
        {
            GameSelectionFlowController = gameSelectionFlowController,
            ArchiveReadinessFlowController = archiveReadinessFlowController,
            InstallPopupPresenter = installPopupPresenter,
            InstallFlowController = installFlowController,
            OptiClickUninstallPlanBuilder = optiClickUninstallPlanBuilder,
            OptiClickUninstallExecutor = optiClickUninstallExecutor
        };
    }
}
