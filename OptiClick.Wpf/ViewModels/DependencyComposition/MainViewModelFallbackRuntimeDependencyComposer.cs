using OptiClick.Core.Abstractions;
using OptiClick.Wpf.Composition.Modules;
using OptiClick.Wpf.Install.Gates;
using OptiClick.Wpf.Logging;
using OptiClick.Wpf.Shell.Games;
using OptiClick.Wpf.Shell.Games.GpuBundle;
using OptiClick.Wpf.Shell.Gpu;
using OptiClick.Wpf.Shell.Runtime;
using OptiClick.Wpf.Shell.Runtime.DeviceIdentity;
using OptiClick.Wpf.Shell.RuntimeData;
using OptiClick.Wpf.Services;

namespace OptiClick.Wpf.ViewModels.DependencyComposition;

internal static class MainViewModelFallbackRuntimeDependencyComposer
{
    public static RuntimeDependencyComposition Compose(
        IRuntimeContextProvider runtimeContextProvider,
        MainViewModelRuntimeDependencies runtimeDependencies,
        IAppLogger appLogger,
        RuntimeDependencyFallbackServices fallbackServices)
    {
        ArgumentNullException.ThrowIfNull(runtimeContextProvider);
        ArgumentNullException.ThrowIfNull(runtimeDependencies);
        ArgumentNullException.ThrowIfNull(appLogger);
        ArgumentNullException.ThrowIfNull(fallbackServices);

        var operatingSystemSupportPolicy = runtimeDependencies.OperatingSystemSupportPolicy
                                           ?? fallbackServices.OperatingSystemSupportPolicy;
        var deviceIdentityResolver = runtimeDependencies.DeviceIdentityResolver ?? fallbackServices.DeviceIdentityResolver;
        var resolvedModuleDownloadLinkMapBuilder = runtimeDependencies.ModuleDownloadLinkMapBuilder
                                                   ?? fallbackServices.ModuleDownloadLinkMapBuilder;
        var runtimeContextFlowController = runtimeDependencies.RuntimeContextFlowController
                                           ?? fallbackServices.RuntimeContextFlowController;
        var deviceIdentityRulesFlowController = runtimeDependencies.DeviceIdentityRulesFlowController
                                                ?? fallbackServices.DeviceIdentityRulesFlowController;
        var runtimeCatalogFlowController = runtimeDependencies.RuntimeCatalogFlowController
                                           ?? ResolveRuntimeCatalogFlowController(
                                               runtimeDependencies,
                                               fallbackServices,
                                               resolvedModuleDownloadLinkMapBuilder);
        var runtimeEndpointStatusPresenter = runtimeDependencies.RuntimeEndpointStatusPresenter
                                             ?? fallbackServices.RuntimeEndpointStatusPresenter;
        var runtimeCatalogUiFlowController = runtimeDependencies.RuntimeCatalogUiFlowController
                                             ?? fallbackServices.RuntimeCatalogUiFlowController;
        var gpuBundleManifestClient = runtimeDependencies.GpuBundleManifestClient
                                      ?? fallbackServices.GpuBundleManifestClient;
        var gpuBundleManifestRuleResolver = runtimeDependencies.GpuBundleManifestRuleResolver
                                            ?? fallbackServices.GpuBundleManifestRuleResolver;
        var gpuSelectionCoordinator = runtimeDependencies.GpuSelectionCoordinator
                                      ?? fallbackServices.GpuSelectionCoordinator;
        var runtimeCatalogCoordinator = runtimeDependencies.RuntimeCatalogCoordinator
                                        ?? new RuntimeCatalogCoordinator(
                                            runtimeCatalogFlowController,
                                            runtimeEndpointStatusPresenter);

        return new RuntimeDependencyComposition
        {
            OperatingSystemSupportPolicy = operatingSystemSupportPolicy,
            ShellGameCardViewModelFactory = runtimeDependencies.ShellGameCardViewModelFactory,
            RuntimeContextFlowController = runtimeContextFlowController,
            DeviceIdentityRulesFlowController = deviceIdentityRulesFlowController,
            RuntimeCatalogFlowController = runtimeCatalogFlowController,
            RuntimeEndpointStatusPresenter = runtimeEndpointStatusPresenter,
            RuntimeCatalogUiFlowController = runtimeCatalogUiFlowController,
            GpuSelectionCoordinator = gpuSelectionCoordinator,
            RuntimeCatalogCoordinator = runtimeCatalogCoordinator,
            GpuBundleManifestClient = gpuBundleManifestClient,
            GpuBundleManifestRuleResolver = gpuBundleManifestRuleResolver,
            DeviceIdentityResolver = deviceIdentityResolver
        };
    }

    private static RuntimeCatalogFlowController ResolveRuntimeCatalogFlowController(
        MainViewModelRuntimeDependencies runtimeDependencies,
        RuntimeDependencyFallbackServices fallbackServices,
        ModuleDownloadLinkMapBuilder moduleDownloadLinkMapBuilder)
    {
        var canUseFallback = runtimeDependencies.RemoteCatalogPipeline is null
                             && ReferenceEquals(
                                 moduleDownloadLinkMapBuilder,
                                 fallbackServices.ModuleDownloadLinkMapBuilder);
        if (canUseFallback)
        {
            return fallbackServices.RuntimeCatalogFlowController;
        }

        return new RuntimeCatalogFlowController(
            runtimeDependencies.RemoteCatalogPipeline,
            moduleDownloadLinkMapBuilder,
            fallbackServices.RuntimeCatalogDialogPresenter);
    }
}
