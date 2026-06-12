using System.Net.Http;
using OptiClick.Core.Abstractions;
using OptiClick.Infrastructure.Remote;
using OptiClick.Wpf.Install.Presentation;
using OptiClick.Wpf.Logging;
using OptiClick.Wpf.Shell.Flow;
using OptiClick.Wpf.Shell.Games;
using OptiClick.Wpf.Shell.Games.GpuBundle;
using OptiClick.Wpf.Shell.Gpu;
using OptiClick.Wpf.Shell.Runtime;
using OptiClick.Wpf.Shell.RuntimeData;
using OptiClick.Wpf.Services;
using OptiClick.Wpf.ViewModels;
using OptiClick.Wpf.ViewModels.DependencyComposition;

namespace OptiClick.Wpf.Composition.Modules;

internal sealed record RuntimeDependencyFallbackServices
{
    public required IOperatingSystemSupportPolicy OperatingSystemSupportPolicy { get; init; }
    public required IDeviceIdentityResolver DeviceIdentityResolver { get; init; }
    public required ModuleDownloadLinkMapBuilder ModuleDownloadLinkMapBuilder { get; init; }
    public required RuntimeContextFlowController RuntimeContextFlowController { get; init; }
    public required DeviceIdentityRulesFlowController DeviceIdentityRulesFlowController { get; init; }
    public required RuntimeCatalogFlowController RuntimeCatalogFlowController { get; init; }
    public required RuntimeCatalogDialogPresenter RuntimeCatalogDialogPresenter { get; init; }
    public required RuntimeEndpointStatusPresenter RuntimeEndpointStatusPresenter { get; init; }
    public required MainRuntimeCatalogUiFlowController RuntimeCatalogUiFlowController { get; init; }
    public required IRemoteGpuBundleManifestClient GpuBundleManifestClient { get; init; }
    public required IGpuBundleManifestRuleResolver GpuBundleManifestRuleResolver { get; init; }
    public required GpuSelectionCoordinator GpuSelectionCoordinator { get; init; }
}

internal sealed record RuntimeModuleCompositionServices
{
    public required RuntimeHeaderPresenter RuntimeHeaderPresenter { get; init; }
    public required RuntimeSummaryStateController RuntimeSummaryStateController { get; init; }
    public required RuntimeContextCoordinator RuntimeContextCoordinator { get; init; }
}

internal static class RuntimeModuleComposition
{
    public static RuntimeDependencyFallbackServices ComposeFallbackServices(
        IRuntimeContextProvider runtimeContextProvider,
        MainViewModelRuntimeDependencies runtimeDependencies,
        IAppLogger appLogger)
    {
        ArgumentNullException.ThrowIfNull(runtimeContextProvider);
        ArgumentNullException.ThrowIfNull(runtimeDependencies);
        ArgumentNullException.ThrowIfNull(appLogger);

        var moduleDownloadLinkMapBuilder = new ModuleDownloadLinkMapBuilder();
        var runtimeContextFlowController = new RuntimeContextFlowController(runtimeContextProvider);
        var deviceIdentityRulesFlowController = new DeviceIdentityRulesFlowController(
            runtimeDependencies.DeviceIdentityRulesLoader);
        var runtimeCatalogDialogPresenter = new RuntimeCatalogDialogPresenter();
        var runtimeCatalogFlowController = new RuntimeCatalogFlowController(
            runtimeDependencies.RemoteCatalogPipeline,
            moduleDownloadLinkMapBuilder,
            runtimeCatalogDialogPresenter);
        var runtimeEndpointStatusPresenter = new RuntimeEndpointStatusPresenter();
        var fallbackGpuManifestRequestUriBuilder = new GpuBundleManifestRequestUriBuilder();
        var gpuBundleManifestClient = new CachedRemoteGpuBundleManifestClient(
            new RemoteGpuBundleManifestClient(
                new HttpClient(),
                fallbackGpuManifestRequestUriBuilder,
                new RemoteGpuBundleManifestParser(),
                appLogger),
            fallbackGpuManifestRequestUriBuilder);

        return new RuntimeDependencyFallbackServices
        {
            OperatingSystemSupportPolicy = new OverrideableOperatingSystemSupportPolicy(
                new Windows11OnlyOperatingSystemSupportPolicy()),
            DeviceIdentityResolver = new PassthroughDeviceIdentityResolver(),
            ModuleDownloadLinkMapBuilder = moduleDownloadLinkMapBuilder,
            RuntimeContextFlowController = runtimeContextFlowController,
            DeviceIdentityRulesFlowController = deviceIdentityRulesFlowController,
            RuntimeCatalogFlowController = runtimeCatalogFlowController,
            RuntimeCatalogDialogPresenter = runtimeCatalogDialogPresenter,
            RuntimeEndpointStatusPresenter = runtimeEndpointStatusPresenter,
            RuntimeCatalogUiFlowController = new MainRuntimeCatalogUiFlowController(),
            GpuBundleManifestClient = gpuBundleManifestClient,
            GpuBundleManifestRuleResolver = new GpuBundleManifestRuleResolver(),
            GpuSelectionCoordinator = new GpuSelectionCoordinator(GpuSelectionCoordinator.DefaultMaxSupportedGpuCount)
        };
    }

    public static RuntimeDependencyFallbackServices ComposeExplicitServices(
        MainViewModelRuntimeDependencies runtimeDependencies)
    {
        ArgumentNullException.ThrowIfNull(runtimeDependencies);

        return new RuntimeDependencyFallbackServices
        {
            OperatingSystemSupportPolicy = Require(
                runtimeDependencies.OperatingSystemSupportPolicy,
                nameof(MainViewModelRuntimeDependencies.OperatingSystemSupportPolicy)),
            DeviceIdentityResolver = Require(
                runtimeDependencies.DeviceIdentityResolver,
                nameof(MainViewModelRuntimeDependencies.DeviceIdentityResolver)),
            ModuleDownloadLinkMapBuilder = ResolveExplicitShellLocalDefault(
                runtimeDependencies.ModuleDownloadLinkMapBuilder,
                static () => new ModuleDownloadLinkMapBuilder()),
            RuntimeContextFlowController = Require(
                runtimeDependencies.RuntimeContextFlowController,
                nameof(MainViewModelRuntimeDependencies.RuntimeContextFlowController)),
            DeviceIdentityRulesFlowController = Require(
                runtimeDependencies.DeviceIdentityRulesFlowController,
                nameof(MainViewModelRuntimeDependencies.DeviceIdentityRulesFlowController)),
            RuntimeCatalogFlowController = Require(
                runtimeDependencies.RuntimeCatalogFlowController,
                nameof(MainViewModelRuntimeDependencies.RuntimeCatalogFlowController)),
            RuntimeCatalogDialogPresenter = CreateExplicitShellLocalDefault(
                static () => new RuntimeCatalogDialogPresenter()),
            RuntimeEndpointStatusPresenter = Require(
                runtimeDependencies.RuntimeEndpointStatusPresenter,
                nameof(MainViewModelRuntimeDependencies.RuntimeEndpointStatusPresenter)),
            RuntimeCatalogUiFlowController = ResolveExplicitShellLocalDefault(
                runtimeDependencies.RuntimeCatalogUiFlowController,
                static () => new MainRuntimeCatalogUiFlowController()),
            GpuBundleManifestClient = Require(
                runtimeDependencies.GpuBundleManifestClient,
                nameof(MainViewModelRuntimeDependencies.GpuBundleManifestClient)),
            GpuBundleManifestRuleResolver = Require(
                runtimeDependencies.GpuBundleManifestRuleResolver,
                nameof(MainViewModelRuntimeDependencies.GpuBundleManifestRuleResolver)),
            GpuSelectionCoordinator = ResolveExplicitShellLocalDefault(
                runtimeDependencies.GpuSelectionCoordinator,
                static () => new GpuSelectionCoordinator(GpuSelectionCoordinator.DefaultMaxSupportedGpuCount))
        };
    }

    private static T ResolveExplicitShellLocalDefault<T>(T? dependency, Func<T> createDefault)
        where T : class
    {
        return dependency ?? CreateExplicitShellLocalDefault(createDefault);
    }

    private static T CreateExplicitShellLocalDefault<T>(Func<T> createDefault)
        where T : class
    {
        // Explicit composition still permits lightweight shell helpers that have no external side effects.
        return createDefault();
    }

    private static T Require<T>(T? dependency, string dependencyName)
        where T : class
    {
        return dependency
               ?? throw new InvalidOperationException(
                   $"MainViewModel runtime dependency '{dependencyName}' must be provided when fallback resolution is disabled.");
    }

    public static RuntimeModuleCompositionServices Compose(
        MainViewModelAppDependencies appDependencies,
        MainViewModelRuntimeDependencies runtimeDependencies,
        RuntimeDependencyComposition runtimeComposition,
        FlowLogDispatcher flowLogDispatcher)
    {
        ArgumentNullException.ThrowIfNull(appDependencies);
        ArgumentNullException.ThrowIfNull(runtimeDependencies);
        ArgumentNullException.ThrowIfNull(runtimeComposition);
        ArgumentNullException.ThrowIfNull(flowLogDispatcher);

        var runtimeHeaderPresenter = appDependencies.RuntimeHeaderPresenter ?? new RuntimeHeaderPresenter();
        var runtimeSummaryStateController = appDependencies.RuntimeSummaryStateController
                                                ?? new RuntimeSummaryStateController(
                                                    runtimeComposition.DeviceIdentityResolver,
                                                    runtimeHeaderPresenter);
        var runtimeContextCoordinator = runtimeDependencies.RuntimeContextCoordinator
                                        ?? new RuntimeContextCoordinator(
                                            runtimeComposition.RuntimeContextFlowController,
                                            runtimeSummaryStateController,
                                            flowLogDispatcher,
                                            runtimeComposition.GpuSelectionCoordinator);

        return new RuntimeModuleCompositionServices
        {
            RuntimeHeaderPresenter = runtimeHeaderPresenter,
            RuntimeSummaryStateController = runtimeSummaryStateController,
            RuntimeContextCoordinator = runtimeContextCoordinator
        };
    }
}
