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

internal static class MainWindowRuntimeFeatureComposer
{
    public static MainRuntimeResolvedDependencies Compose(
        RuntimeCompositionServices runtime,
        FlowLogDispatcher flowLogDispatcher)
    {
        var runtimeCatalogUiFlowController = new MainRuntimeCatalogUiFlowController();
        var gpuSelectionCoordinator = new GpuSelectionCoordinator(
            GpuSelectionCoordinator.DefaultMaxSupportedGpuCount);
        var runtimeHeaderPresenter = new RuntimeHeaderPresenter();
        var runtimeSummaryStateController = new RuntimeSummaryStateController(
            runtime.DeviceIdentityResolver,
            runtimeHeaderPresenter);
        var runtimeContextCoordinator = new RuntimeContextCoordinator(
            runtime.RuntimeContextFlowController,
            runtimeSummaryStateController,
            flowLogDispatcher,
            gpuSelectionCoordinator);

        return new MainRuntimeResolvedDependencies
        {
            OperatingSystemSupportPolicy = new OverrideableOperatingSystemSupportPolicy(
                new Windows11OnlyOperatingSystemSupportPolicy()),
            ShellGameCardViewModelFactory = runtime.ShellGameCardViewModelFactory,
            RuntimeContextFlowController = runtime.RuntimeContextFlowController,
            DeviceIdentityRulesFlowController = runtime.DeviceIdentityRulesFlowController,
            RuntimeCatalogFlowController = runtime.RuntimeCatalogFlowController,
            RuntimeEndpointStatusPresenter = runtime.RuntimeEndpointStatusPresenter,
            RuntimeCatalogUiFlowController = runtimeCatalogUiFlowController,
            GpuSelectionCoordinator = gpuSelectionCoordinator,
            RuntimeContextCoordinator = runtimeContextCoordinator,
            RuntimeCatalogCoordinator = new RuntimeCatalogCoordinator(
                runtime.RuntimeCatalogFlowController,
                runtime.RuntimeEndpointStatusPresenter),
            GpuBundleManifestClient = runtime.GpuBundleManifestClient,
            GpuBundleManifestRuleResolver = runtime.GpuBundleManifestRuleResolver,
            RuntimeHeaderPresenter = runtimeHeaderPresenter,
            RuntimeSummaryStateController = runtimeSummaryStateController
        };
    }
}
