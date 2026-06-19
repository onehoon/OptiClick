using OptiClick.Core.Abstractions;
using System.Net.Http;
using OptiClick.Wpf.Configuration;
using OptiClick.Infrastructure.Downloads;
using OptiClick.Infrastructure.Remote;
using OptiClick.Wpf.Diagnostics;
using OptiClick.Wpf.Install.Planning;
using OptiClick.Wpf.Install.Config;
using OptiClick.Wpf.Install.Precheck;
using OptiClick.Wpf.Install.Archives;
using OptiClick.Wpf.Install.Execution;
using OptiClick.Wpf.Install.Flow;
using OptiClick.Wpf.Install.FileSystem;
using OptiClick.Wpf.Install.Gates;
using OptiClick.Wpf.Install.Presentation;
using OptiClick.Wpf.Shell.Runtime.DeviceIdentity;
using OptiClick.Wpf.Shell.Games.GpuBundle;
using OptiClick.Wpf.Shell.RuntimeData;
using OptiClick.Wpf.Install.UiState;
using OptiClick.Wpf.Localization;
using OptiClick.Wpf.Logging;
using OptiClick.Wpf.Shell.Games;
using OptiClick.Wpf.Shell.Games.Actions;
using OptiClick.Wpf.Shell.Flow;
using OptiClick.Wpf.Shell.Runtime;
using OptiClick.Wpf.Shell.Scan;
using OptiClick.Wpf.Shell.Selection;
using OptiClick.Wpf.Services;
using OptiClick.Infrastructure.FileSystem;
using OptiClick.Wpf.ViewModels;
using OptiClickShell;

namespace OptiClick.Wpf.Composition;

public sealed partial class AppCompositionRoot
{
    public IDeviceIdentityRulesParser CreateDeviceIdentityRulesParser()
    {
        return new DeviceIdentityRulesParser();
    }

    public IRemoteDeviceIdentityRulesClient CreateRemoteDeviceIdentityRulesClient(
        DeviceIdentityRulesOptions options,
        HttpClient? httpClient = null)
    {
        return new RemoteDeviceIdentityRulesClient(
            options.Endpoint,
            options.Enabled,
            httpClient ?? new HttpClient());
    }

    public IRemoteDeviceIdentityRulesLoader CreateRemoteDeviceIdentityRulesLoader(
        IRemoteDeviceIdentityRulesClient client,
        IDeviceIdentityRulesParser parser,
        IDeviceIdentityRulesProvider rulesProvider,
        IDeviceIdentityRulesCacheStore? cacheStore = null,
        IAppLogger? logger = null)
    {
        return new RemoteDeviceIdentityRulesLoader(client, parser, rulesProvider, cacheStore, logger);
    }

    public IRemoteRuntimeDataParser CreateRemoteRuntimeDataParser()
    {
        return new RemoteRuntimeDataParser();
    }

    public IRuntimeDataResourceResolver CreateRuntimeDataResourceResolver()
    {
        return new RuntimeDataResourceResolver();
    }

    public IRuntimeDataProfileResolver CreateRuntimeDataProfileResolver()
    {
        return new RuntimeDataProfileResolver();
    }

    public IRemoteGpuBundleParser CreateRemoteGpuBundleParser()
    {
        return new RemoteGpuBundleParser();
    }

    public IRemoteGpuBundleManifestParser CreateRemoteGpuBundleManifestParser()
    {
        return new RemoteGpuBundleManifestParser();
    }

    public IRemoteGpuBundleManifestClient CreateRemoteGpuBundleManifestClient(
        HttpClient? httpClient = null,
        IAppLogger? logger = null,
        IAppVersionProvider? appVersionProvider = null)
    {
        var requestUriBuilder = CreateGpuBundleManifestRequestUriBuilder();
        var inner = new RemoteGpuBundleManifestClient(
            httpClient ?? new HttpClient(),
            requestUriBuilder,
            CreateRemoteGpuBundleManifestParser(),
            logger,
            appVersionProvider: appVersionProvider is null ? null : (() => appVersionProvider.GetCurrentVersion()));
        return new CachedRemoteGpuBundleManifestClient(inner, requestUriBuilder);
    }

    public IGpuBundleManifestRequestUriBuilder CreateGpuBundleManifestRequestUriBuilder()
    {
        return new GpuBundleManifestRequestUriBuilder();
    }

    public IGpuBundleManifestRuleResolver CreateGpuBundleManifestRuleResolver()
    {
        return new GpuBundleManifestRuleResolver();
    }

    public IGpuBundleRequestUriBuilder CreateGpuBundleRequestUriBuilder()
    {
        return new GpuBundleRequestUriBuilder();
    }

    public IRemoteGpuBundleClient CreateRemoteGpuBundleClient(
        HttpClient? httpClient = null,
        IAppLogger? logger = null,
        IAppVersionProvider? appVersionProvider = null)
    {
        return new RemoteGpuBundleClient(
            httpClient ?? new HttpClient(),
            CreateGpuBundleRequestUriBuilder(),
            logger,
            appVersionProvider: appVersionProvider is null ? null : (() => appVersionProvider.GetCurrentVersion()));
    }

    public IRemoteGpuBundleRuntimeLoader CreateRemoteGpuBundleRuntimeLoader(IAppLogger? logger = null)
    {
        return CreateRemoteGpuBundleRuntimeLoader(
            logger,
            appVersionProvider: null,
            httpClient: null);
    }

    public IRemoteGpuBundleRuntimeLoader CreateRemoteGpuBundleRuntimeLoader(
        IAppLogger? logger,
        IAppVersionProvider? appVersionProvider,
        HttpClient? httpClient)
    {
        var sharedHttpClient = httpClient ?? new HttpClient();
        var effectiveAppVersionProvider = appVersionProvider ?? CreateAppVersionProvider();
        return new RemoteGpuBundleRuntimeLoader(
            CreateRemoteGpuBundleManifestClient(sharedHttpClient, logger, effectiveAppVersionProvider),
            CreateGpuBundleManifestRuleResolver(),
            CreateRemoteGpuBundleClient(sharedHttpClient, logger, effectiveAppVersionProvider),
            CreateRemoteGpuBundleParser(),
            effectiveAppVersionProvider,
            logger);
    }

    public IGpuBundleGameDatabaseMerger CreateGpuBundleGameDatabaseMerger()
    {
        return new GpuBundleGameDatabaseMerger();
    }

    public IRemoteRuntimeDataClient CreateRemoteRuntimeDataClient(
        IRemoteEndpointProvider remoteEndpointProvider,
        HttpClient? httpClient = null,
        IAppLogger? logger = null,
        IAppVersionProvider? appVersionProvider = null)
    {
        return new RemoteRuntimeDataClient(
            remoteEndpointProvider,
            httpClient ?? new HttpClient(),
            logger,
            appVersionProvider: appVersionProvider is null ? null : (() => appVersionProvider.GetCurrentVersion()));
    }

    public IRemoteRuntimeDataLoader CreateRemoteRuntimeDataLoader(
        IRemoteRuntimeDataClient remoteRuntimeDataClient,
        IRemoteRuntimeDataParser remoteRuntimeDataParser)
    {
        return new RemoteRuntimeDataLoader(remoteRuntimeDataClient, remoteRuntimeDataParser);
    }

    public IRuntimeDataShellGameMapper CreateRuntimeDataShellGameMapper()
    {
        return new RuntimeDataShellGameMapper();
    }

    public IRemoteCatalogPipeline CreateRemoteCatalogPipeline(IRemoteRuntimeDataLoader runtimeDataLoader)
    {
        return new RemoteCatalogPipeline(
            runtimeDataLoader,
            CreateRemoteGpuBundleRuntimeLoader(),
            CreateGpuBundleGameDatabaseMerger());
    }

    public IRemoteCatalogPipeline CreateRemoteCatalogPipeline(IRemoteRuntimeDataLoader runtimeDataLoader, IAppLogger? logger)
    {
        return CreateRemoteCatalogPipeline(
            runtimeDataLoader,
            logger,
            httpClient: null,
            appVersionProvider: null);
    }

    public IRemoteCatalogPipeline CreateRemoteCatalogPipeline(
        IRemoteRuntimeDataLoader runtimeDataLoader,
        IAppLogger? logger,
        HttpClient? httpClient,
        IAppVersionProvider? appVersionProvider)
    {
        return new RemoteCatalogPipeline(
            runtimeDataLoader,
            CreateRemoteGpuBundleRuntimeLoader(logger, appVersionProvider, httpClient),
            CreateGpuBundleGameDatabaseMerger());
    }

    public IRemoteDataContractSmokeRunner CreateRemoteDataContractSmokeRunner()
    {
        var languageProvider = new SystemAppLanguageProvider(CreateAppLogger());
        var remoteOptionsLoader = new RemoteDataOptionsLoader();
        var remoteEndpointProvider = new ConfigurationRemoteEndpointProvider(remoteOptionsLoader.Load());
        var runtimeOverrideFactory = new RuntimeTestEnvironmentOverrideProviderFactory();
        var gpuProvider = runtimeOverrideFactory.ResolveGpuProvider(new WindowsGpuInfoProvider(CreateAppLogger()));
        var deviceProvider = runtimeOverrideFactory.ResolveDeviceProvider(new WindowsDeviceInfoProvider());
        var runtimeContextProvider = new RuntimeContextProvider(
            gpuProvider,
            deviceProvider,
            languageProvider,
            remoteEndpointProvider,
            CreateAppLogger());

        var runtimeDataLoader = CreateRemoteRuntimeDataLoader(
            CreateRemoteRuntimeDataClient(remoteEndpointProvider, logger: CreateAppLogger()),
            CreateRemoteRuntimeDataParser());

        return new RemoteDataContractSmokeRunner(
            runtimeContextProvider,
            runtimeDataLoader,
            CreateRemoteGpuBundleManifestClient(),
            CreateGpuBundleManifestRuleResolver(),
            CreateRemoteGpuBundleClient(),
            CreateRemoteGpuBundleParser(),
            CreateGpuBundleGameDatabaseMerger());
    }

    public IShellGameCardStateResolver CreateShellGameCardStateResolver()
    {
        return new ShellGameCardStateResolver();
    }

    public IShellGameCardViewModelFactory CreateShellGameCardViewModelFactory(
        IShellGameCardStateResolver stateResolver,
        IAppStringsProvider stringsProvider)
    {
        return new ShellGameCardViewModelFactory(stateResolver, stringsProvider);
    }
}



