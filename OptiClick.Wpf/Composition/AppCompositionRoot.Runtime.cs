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
using OptiClick.Infrastructure.Security;
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
        IAppVersionProvider? appVersionProvider = null,
        IOptiClickApiRequestAuthenticator? authenticator = null,
        IOptiClickApiTicketStore? ticketStore = null,
        IOptiClickServerClock? serverClock = null)
    {
        var requestUriBuilder = CreateGpuBundleManifestRequestUriBuilder();
        var inner = new RemoteGpuBundleManifestClient(
            httpClient ?? new HttpClient(),
            requestUriBuilder,
            CreateRemoteGpuBundleManifestParser(),
            logger,
            appVersionProvider: appVersionProvider is null ? null : (() => appVersionProvider.GetCurrentVersion()),
            authenticator: authenticator,
            ticketStore: ticketStore,
            serverClock: serverClock);
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
        IAppVersionProvider? appVersionProvider = null,
        IOptiClickApiRequestAuthenticator? authenticator = null,
        IOptiClickApiTicketStore? ticketStore = null,
        IOptiClickServerClock? serverClock = null)
    {
        return new RemoteGpuBundleClient(
            httpClient ?? new HttpClient(),
            CreateGpuBundleRequestUriBuilder(),
            logger,
            appVersionProvider: appVersionProvider is null ? null : (() => appVersionProvider.GetCurrentVersion()),
            authenticator: authenticator,
            ticketStore: ticketStore,
            serverClock: serverClock);
    }

    [Obsolete("Use the overload that receives an authenticator and ticket store for production remote API calls.")]
    public IRemoteGpuBundleRuntimeLoader CreateRemoteGpuBundleRuntimeLoader(IAppLogger? logger = null)
    {
        var securityComposition = CreateRemoteApiSecurityComposition(logger, appVersionProvider: null, httpClient: null);
        return CreateRemoteGpuBundleRuntimeLoaderCore(
            securityComposition.Logger,
            securityComposition.AppVersionProvider,
            securityComposition.SharedHttpClient,
            securityComposition.SecurityServices.ApiRequestAuthenticator,
            securityComposition.SecurityServices.TicketStore,
            securityComposition.SecurityServices.ServerClock);
    }

    [Obsolete("Use the overload that receives an authenticator and ticket store for production remote API calls.")]
    public IRemoteGpuBundleRuntimeLoader CreateRemoteGpuBundleRuntimeLoader(
        IAppLogger? logger,
        IAppVersionProvider? appVersionProvider,
        HttpClient? httpClient)
    {
        var securityComposition = CreateRemoteApiSecurityComposition(logger, appVersionProvider, httpClient);
        return CreateRemoteGpuBundleRuntimeLoaderCore(
            securityComposition.Logger,
            securityComposition.AppVersionProvider,
            securityComposition.SharedHttpClient,
            securityComposition.SecurityServices.ApiRequestAuthenticator,
            securityComposition.SecurityServices.TicketStore,
            securityComposition.SecurityServices.ServerClock);
    }

    public IRemoteGpuBundleRuntimeLoader CreateRemoteGpuBundleRuntimeLoader(
        IAppLogger? logger,
        IAppVersionProvider? appVersionProvider,
        HttpClient? httpClient,
        IOptiClickApiRequestAuthenticator authenticator,
        IOptiClickApiTicketStore ticketStore)
    {
        ArgumentNullException.ThrowIfNull(authenticator);
        ArgumentNullException.ThrowIfNull(ticketStore);

        return CreateRemoteGpuBundleRuntimeLoaderCore(
            logger,
            appVersionProvider,
            httpClient,
            authenticator,
            ticketStore);
    }

    private IRemoteGpuBundleRuntimeLoader CreateRemoteGpuBundleRuntimeLoaderCore(
        IAppLogger? logger,
        IAppVersionProvider? appVersionProvider,
        HttpClient? httpClient,
        IOptiClickApiRequestAuthenticator? authenticator,
        IOptiClickApiTicketStore? ticketStore,
        IOptiClickServerClock? serverClock = null)
    {
        var sharedHttpClient = httpClient ?? new HttpClient();
        var effectiveAppVersionProvider = appVersionProvider ?? CreateAppVersionProvider();
        return new RemoteGpuBundleRuntimeLoader(
            CreateRemoteGpuBundleManifestClient(sharedHttpClient, logger, effectiveAppVersionProvider, authenticator, ticketStore, serverClock),
            CreateGpuBundleManifestRuleResolver(),
            CreateRemoteGpuBundleClient(sharedHttpClient, logger, effectiveAppVersionProvider, authenticator, ticketStore, serverClock),
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
        IAppVersionProvider? appVersionProvider = null,
        IOptiClickApiRequestAuthenticator? authenticator = null,
        IOptiClickServerClock? serverClock = null)
    {
        return new RemoteRuntimeDataClient(
            remoteEndpointProvider,
            httpClient ?? new HttpClient(),
            logger,
            appVersionProvider: appVersionProvider is null ? null : (() => appVersionProvider.GetCurrentVersion()),
            authenticator: authenticator,
            serverClock: serverClock);
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

    [Obsolete("Use the overload that receives an authenticator and ticket store for production remote API calls.")]
    public IRemoteCatalogPipeline CreateRemoteCatalogPipeline(IRemoteRuntimeDataLoader runtimeDataLoader)
    {
        var securityComposition = CreateRemoteApiSecurityComposition(logger: null, appVersionProvider: null, httpClient: null);
        return new RemoteCatalogPipeline(
            runtimeDataLoader,
            CreateRemoteGpuBundleRuntimeLoaderCore(
                securityComposition.Logger,
                securityComposition.AppVersionProvider,
                securityComposition.SharedHttpClient,
                securityComposition.SecurityServices.ApiRequestAuthenticator,
                securityComposition.SecurityServices.TicketStore,
                securityComposition.SecurityServices.ServerClock),
            CreateGpuBundleGameDatabaseMerger());
    }

    [Obsolete("Use the overload that receives an authenticator and ticket store for production remote API calls.")]
    public IRemoteCatalogPipeline CreateRemoteCatalogPipeline(IRemoteRuntimeDataLoader runtimeDataLoader, IAppLogger? logger)
    {
        var securityComposition = CreateRemoteApiSecurityComposition(logger, appVersionProvider: null, httpClient: null);
        return CreateRemoteCatalogPipelineCore(
            runtimeDataLoader,
            securityComposition.Logger,
            securityComposition.SharedHttpClient,
            securityComposition.AppVersionProvider,
            securityComposition.SecurityServices.ApiRequestAuthenticator,
            securityComposition.SecurityServices.TicketStore,
            securityComposition.SecurityServices.ServerClock);
    }

    [Obsolete("Use the overload that receives an authenticator and ticket store for production remote API calls.")]
    public IRemoteCatalogPipeline CreateRemoteCatalogPipeline(
        IRemoteRuntimeDataLoader runtimeDataLoader,
        IAppLogger? logger,
        HttpClient? httpClient,
        IAppVersionProvider? appVersionProvider)
    {
        var securityComposition = CreateRemoteApiSecurityComposition(logger, appVersionProvider, httpClient);
        return CreateRemoteCatalogPipelineCore(
            runtimeDataLoader,
            securityComposition.Logger,
            securityComposition.SharedHttpClient,
            securityComposition.AppVersionProvider,
            securityComposition.SecurityServices.ApiRequestAuthenticator,
            securityComposition.SecurityServices.TicketStore,
            securityComposition.SecurityServices.ServerClock);
    }

    public IRemoteCatalogPipeline CreateRemoteCatalogPipeline(
        IRemoteRuntimeDataLoader runtimeDataLoader,
        IAppLogger? logger,
        HttpClient? httpClient,
        IAppVersionProvider? appVersionProvider,
        IOptiClickApiRequestAuthenticator authenticator,
        IOptiClickApiTicketStore ticketStore)
    {
        ArgumentNullException.ThrowIfNull(authenticator);
        ArgumentNullException.ThrowIfNull(ticketStore);

        return CreateRemoteCatalogPipelineCore(
            runtimeDataLoader,
            logger,
            httpClient,
            appVersionProvider,
            authenticator,
            ticketStore);
    }

    private IRemoteCatalogPipeline CreateRemoteCatalogPipelineCore(
        IRemoteRuntimeDataLoader runtimeDataLoader,
        IAppLogger? logger,
        HttpClient? httpClient,
        IAppVersionProvider? appVersionProvider,
        IOptiClickApiRequestAuthenticator? authenticator,
        IOptiClickApiTicketStore? ticketStore,
        IOptiClickServerClock? serverClock = null)
    {
        return new RemoteCatalogPipeline(
            runtimeDataLoader,
            CreateRemoteGpuBundleRuntimeLoaderCore(logger, appVersionProvider, httpClient, authenticator, ticketStore, serverClock),
            CreateGpuBundleGameDatabaseMerger());
    }

    private (HttpClient SharedHttpClient, IAppVersionProvider AppVersionProvider, IAppLogger Logger, AppSecurityServices SecurityServices)
        CreateRemoteApiSecurityComposition(
            IAppLogger? logger,
            IAppVersionProvider? appVersionProvider,
            HttpClient? httpClient)
    {
        var sharedHttpClient = httpClient ?? new HttpClient();
        var effectiveLogger = logger ?? CreateAppLogger();
        var effectiveAppVersionProvider = appVersionProvider ?? CreateAppVersionProvider();
        var remoteDataOptions = new RemoteDataOptionsLoader().Load();
        var securityServices = CreateAppSecurityServices(
            remoteDataOptions,
            CreateAppLocalDataPathProvider(),
            effectiveLogger,
            effectiveAppVersionProvider,
            sharedHttpClient);

        return (sharedHttpClient, effectiveAppVersionProvider, effectiveLogger, securityServices);
    }

    public IRemoteDataContractSmokeRunner CreateRemoteDataContractSmokeRunner()
    {
        var logger = CreateAppLogger();
        var appVersionProvider = CreateAppVersionProvider();
        var localDataPathProvider = CreateAppLocalDataPathProvider();
        var sharedHttpClient = new HttpClient();
        var languageProvider = new SystemAppLanguageProvider(logger);
        var remoteOptionsLoader = new RemoteDataOptionsLoader();
        var remoteDataOptions = remoteOptionsLoader.Load();
        var remoteEndpointProvider = new ConfigurationRemoteEndpointProvider(remoteDataOptions);
        var securityServices = CreateAppSecurityServices(
            remoteDataOptions,
            localDataPathProvider,
            logger,
            appVersionProvider,
            sharedHttpClient);
        var runtimeOverrideFactory = new RuntimeTestEnvironmentOverrideProviderFactory();
        var gpuProvider = runtimeOverrideFactory.ResolveGpuProvider(new WindowsGpuInfoProvider(logger));
        var deviceProvider = runtimeOverrideFactory.ResolveDeviceProvider(new WindowsDeviceInfoProvider(logger));
        var runtimeContextProvider = new RuntimeContextProvider(
            gpuProvider,
            deviceProvider,
            languageProvider,
            remoteEndpointProvider,
            logger);

        var runtimeDataLoader = CreateRemoteRuntimeDataLoader(
            CreateRemoteRuntimeDataClient(
                remoteEndpointProvider,
                sharedHttpClient,
                logger,
                appVersionProvider,
                securityServices.ApiRequestAuthenticator,
                securityServices.ServerClock),
            CreateRemoteRuntimeDataParser());

        return new RemoteDataContractSmokeRunner(
            runtimeContextProvider,
            runtimeDataLoader,
            CreateRemoteGpuBundleManifestClient(
                sharedHttpClient,
                logger,
                appVersionProvider,
                securityServices.ApiRequestAuthenticator,
                securityServices.TicketStore,
                securityServices.ServerClock),
            CreateGpuBundleManifestRuleResolver(),
            CreateRemoteGpuBundleClient(
                sharedHttpClient,
                logger,
                appVersionProvider,
                securityServices.ApiRequestAuthenticator,
                securityServices.TicketStore,
                securityServices.ServerClock),
            CreateRemoteGpuBundleParser(),
            CreateGpuBundleGameDatabaseMerger(),
            () => appVersionProvider.GetCurrentVersion());
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



