using OptiClick.Core.Abstractions;
using OptiClick.Wpf.Configuration;
using OptiClick.Wpf.Shell.Runtime.DeviceIdentity;
using OptiClick.Wpf.Shell.RuntimeData;
using OptiClick.Wpf.Services;
using OptiClick.Wpf.Shell.Games;
using OptiClick.Wpf.Shell.Games.GpuBundle;
using OptiClick.Wpf.Shell.Runtime;
using System.Net.Http;

namespace OptiClick.Wpf.Composition;

public sealed record RuntimeCompositionServices
{
    public required IRuntimeContextProvider RuntimeContextProvider { get; init; }
    public required IRemoteRuntimeDataLoader RuntimeDataLoader { get; init; }
    public required IRemoteCatalogPipeline RemoteCatalogPipeline { get; init; }
    public required DeviceIdentityResolver DeviceIdentityResolver { get; init; }
    public required IRemoteDeviceIdentityRulesLoader DeviceIdentityRulesLoader { get; init; }
    public required IShellGameCardViewModelFactory ShellGameCardViewModelFactory { get; init; }
    public required RuntimeContextFlowController RuntimeContextFlowController { get; init; }
    public required DeviceIdentityRulesFlowController DeviceIdentityRulesFlowController { get; init; }
    public required RuntimeCatalogFlowController RuntimeCatalogFlowController { get; init; }
    public required RuntimeEndpointStatusPresenter RuntimeEndpointStatusPresenter { get; init; }
    public required ModuleDownloadLinkMapBuilder ModuleDownloadLinkMapBuilder { get; init; }
    public required IRemoteGpuBundleManifestClient GpuBundleManifestClient { get; init; }
    public required IGpuBundleManifestRuleResolver GpuBundleManifestRuleResolver { get; init; }
}

public sealed class RuntimeComposition
{
    private readonly AppCompositionRoot _root;

    public RuntimeComposition(AppCompositionRoot root)
    {
        _root = root ?? throw new ArgumentNullException(nameof(root));
    }

    public RuntimeCompositionServices CreateRuntimeServices(AppSharedServices app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var sharedRemoteHttpClient = new HttpClient();
        var runtimeDataLoader = _root.CreateRemoteRuntimeDataLoader(
            _root.CreateRemoteRuntimeDataClient(
                app.RemoteEndpointProvider,
                sharedRemoteHttpClient,
                app.AppLogger,
                app.AppVersionProvider),
            _root.CreateRemoteRuntimeDataParser());
        var gpuBundleManifestClient = _root.CreateRemoteGpuBundleManifestClient(
            sharedRemoteHttpClient,
            app.AppLogger,
            app.AppVersionProvider);
        var gpuBundleRuntimeLoader = new RemoteGpuBundleRuntimeLoader(
            gpuBundleManifestClient,
            _root.CreateGpuBundleManifestRuleResolver(),
            _root.CreateRemoteGpuBundleClient(
                sharedRemoteHttpClient,
                app.AppLogger,
                app.AppVersionProvider),
            _root.CreateRemoteGpuBundleParser(),
            app.AppVersionProvider,
            app.AppLogger);
        var remoteCatalogPipeline = new RemoteCatalogPipeline(
            runtimeDataLoader,
            gpuBundleRuntimeLoader,
            _root.CreateGpuBundleGameDatabaseMerger());
        var gpuBundleManifestRuleResolver = _root.CreateGpuBundleManifestRuleResolver();
        var runtimeDataCardFactory = _root.CreateShellGameCardViewModelFactory(
            _root.CreateShellGameCardStateResolver(),
            app.StringsProvider);
        var deviceIdentityRulesOptionsLoader = new DeviceIdentityRulesOptionsLoader(AppContext.BaseDirectory);
        var deviceIdentityRulesProvider = new InMemoryDeviceIdentityRulesProvider();
        var deviceIdentityResolver = new DeviceIdentityResolver(deviceIdentityRulesProvider);
        var deviceIdentityRulesCacheStore = new DeviceIdentityRulesCacheStore(app.LocalDataPathProvider, app.AppLogger);
        var deviceIdentityRulesClient = _root.CreateRemoteDeviceIdentityRulesClient(
            deviceIdentityRulesOptionsLoader.Load(),
            sharedRemoteHttpClient);
        var deviceIdentityRulesLoader = _root.CreateRemoteDeviceIdentityRulesLoader(
            deviceIdentityRulesClient,
            _root.CreateDeviceIdentityRulesParser(),
            deviceIdentityRulesProvider,
            deviceIdentityRulesCacheStore,
            app.AppLogger);
        var moduleDownloadLinkMapBuilder = new ModuleDownloadLinkMapBuilder();
        var runtimeCatalogDialogPresenter = new RuntimeCatalogDialogPresenter();
        var runtimeOverrideFactory = new RuntimeTestEnvironmentOverrideProviderFactory();
        var gpuProvider = runtimeOverrideFactory.ResolveGpuProvider(new WindowsGpuInfoProvider(app.AppLogger));
        var deviceProvider = runtimeOverrideFactory.ResolveDeviceProvider(new WindowsDeviceInfoProvider());
        var runtimeContextProvider = new RuntimeContextProvider(
            gpuProvider,
            deviceProvider,
            app.LanguageProvider,
            app.RemoteEndpointProvider,
            app.AppLogger);
        var runtimeContextFlowController = new RuntimeContextFlowController(runtimeContextProvider);
        var deviceIdentityRulesFlowController = new DeviceIdentityRulesFlowController(deviceIdentityRulesLoader);
        var runtimeCatalogFlowController = new RuntimeCatalogFlowController(
            remoteCatalogPipeline,
            moduleDownloadLinkMapBuilder,
            runtimeCatalogDialogPresenter);

        return new RuntimeCompositionServices
        {
            RuntimeContextProvider = runtimeContextProvider,
            RuntimeDataLoader = runtimeDataLoader,
            RemoteCatalogPipeline = remoteCatalogPipeline,
            DeviceIdentityResolver = deviceIdentityResolver,
            DeviceIdentityRulesLoader = deviceIdentityRulesLoader,
            ShellGameCardViewModelFactory = runtimeDataCardFactory,
            RuntimeContextFlowController = runtimeContextFlowController,
            DeviceIdentityRulesFlowController = deviceIdentityRulesFlowController,
            RuntimeCatalogFlowController = runtimeCatalogFlowController,
            RuntimeEndpointStatusPresenter = new RuntimeEndpointStatusPresenter(),
            ModuleDownloadLinkMapBuilder = moduleDownloadLinkMapBuilder,
            GpuBundleManifestClient = gpuBundleManifestClient,
            GpuBundleManifestRuleResolver = gpuBundleManifestRuleResolver
        };
    }
}
