using OptiClick.Wpf.Install.Archives;
using OptiClick.Core.Install;
using OptiClick.Infrastructure.Install.Config;
using OptiClick.Wpf.Install.Config;
using OptiClick.Wpf.Install.Execution;
using OptiClick.Wpf.Install.FileSystem;
using OptiClick.Wpf.Install.Flow;
using OptiClick.Wpf.Install.Gates;
using OptiClick.Wpf.Install.Planning;
using OptiClick.Wpf.Install.Presentation;
using OptiClick.Wpf.Install.UiState;
using OptiClick.Wpf.Install.Uninstall;
using OptiClick.Wpf.Composition.Modules;
using OptiClick.Wpf.Shell.Selection;

namespace OptiClick.Wpf.Composition;

public sealed record InstallCompositionServices
{
    public required IInstallStatusResolver InstallStatusResolver { get; init; }
    public required IShellInstallSelectionBridge InstallSelectionBridge { get; init; }
    public required GameSelectionFlowController GameSelectionFlowController { get; init; }
    public required IInstallPlanBuilder InstallPlanBuilder { get; init; }
    public required IComponentInstallCoordinator ComponentInstallCoordinator { get; init; }
    public required IComponentInstallParityReviewBuilder ComponentInstallParityReviewBuilder { get; init; }
    public required IConfigProfileApplier ConfigProfileApplier { get; init; }
    public required IniProfileEditor IniProfileEditor { get; init; }
    public required IInstallStartGateResolver InstallStartGateResolver { get; init; }
    public required IArchivePreparationCoordinator ArchivePreparationCoordinator { get; init; }
    public required IInstallRejectionPresentationResolver InstallRejectionPresentationResolver { get; init; }
    public required IInstallResultPresentationResolver InstallResultPresentationResolver { get; init; }
    public required InstallSelectionRequestBuilder InstallSelectionRequestBuilder { get; init; }
    public required InstallPlanInputBuilder InstallPlanInputBuilder { get; init; }
    public required ComponentInstallContextBuilder ComponentInstallContextBuilder { get; init; }
    public required InstallPopupPresenter InstallPopupPresenter { get; init; }
    public required InstallCompletionMessageBuilder InstallCompletionMessageBuilder { get; init; }
    public required ArchiveReadinessFlowController ArchiveReadinessFlowController { get; init; }
    public required ConfigApplyFlowController ConfigApplyFlowController { get; init; }
    public required IInstallResultApplier InstallResultApplier { get; init; }
    public required InstallFlowController InstallFlowController { get; init; }
    public required IOptiClickUninstallPlanBuilder OptiClickUninstallPlanBuilder { get; init; }
    public required IOptiClickUninstallExecutor OptiClickUninstallExecutor { get; init; }
}

public sealed class InstallComposition
{
    private sealed record ArchivePreparationServices
    {
        public required IArchiveDownloader ArchiveDownloader { get; init; }
        public required IArchiveExtractor ArchiveExtractor { get; init; }
        public required IArchivePreparationCoordinator ArchivePreparationCoordinator { get; init; }
        public required IOptiScalerVariantArchiveSyncService OptiScalerVariantArchiveSyncService { get; init; }
        public required IOptiScalerPayloadOptiPatcherInjector OptiScalerPayloadOptiPatcherInjector { get; init; }
        public required ArchiveCachePaths ArchiveCachePaths { get; init; }
    }

    private sealed record ConfigProfileServices
    {
        public required IniProfileEditor IniProfileEditor { get; init; }
        public required IConfigProfileApplier ConfigProfileApplier { get; init; }
    }

    private readonly AppCompositionRoot _root;

    public InstallComposition(AppCompositionRoot root)
    {
        _root = root ?? throw new ArgumentNullException(nameof(root));
    }

    public InstallCompositionServices CreateInstallServices(AppSharedServices app)
    {
        ArgumentNullException.ThrowIfNull(app);

        // Archive preparation infrastructure
        var archivePreparation = CreateArchivePreparation(app);

        // Install filesystem and signature services
        var installFileSystem = _root.CreateInstallFileSystem();
        var versionInfoReader = _root.CreateFileVersionInfoReader();
        var installStatusResolver = _root.CreateInstallStatusResolver(installFileSystem, versionInfoReader);
        var signatures = _root.CreateFileSignatureDetectors(installFileSystem);
        var uninstallPlanBuilder = new LazyOptiClickUninstallPlanBuilder(() =>
            _root.CreateOptiClickUninstallPlanBuilder(
                installFileSystem,
                signatures,
                versionInfoReader,
                app.AppLogger));
        var uninstallExecutor = new LazyOptiClickUninstallExecutor(() =>
            _root.CreateOptiClickUninstallExecutor(
                installFileSystem,
                signatures,
                versionInfoReader,
                app.AppLogger));

        var componentInstallCoordinator = new LazyComponentInstallCoordinator(() =>
            CreateComponentInstallCoordinator(
                app,
                installFileSystem,
                signatures,
                archivePreparation.ArchiveDownloader,
                archivePreparation.ArchiveExtractor));

        // Config/profile apply services
        var configProfileServices = CreateConfigProfileServices();

        // Install flow/presentation services
        var installSelectionBridge = _root.CreateShellInstallSelectionBridge(app.StringsProvider);
        var installSelectionRequestBuilder = new InstallSelectionRequestBuilder(
            installStatusResolver,
            new RtssOverlayNoticeResolver(new RtssOverlayNoticeStateProvider()));
        var gameSelectionFlowController = new GameSelectionFlowController(installSelectionBridge, installSelectionRequestBuilder);
        var installPlanBuilder = _root.CreateInstallPlanBuilder();
        var componentInstallParityReviewBuilder = _root.CreateComponentInstallParityReviewBuilder();
        var installStartGateResolver = _root.CreateInstallStartGateResolver();
        var installRejectionPresentationResolver = _root.CreateInstallRejectionPresentationResolver();
        var installResultPresentationResolver = _root.CreateInstallResultPresentationResolver();
        var installPlanInputBuilder = new InstallPlanInputBuilder();
        var componentInstallContextBuilder = new ComponentInstallContextBuilder();
        var installPopupPresenter = new InstallPopupPresenter();
        var installCompletionMessageBuilder = new InstallCompletionMessageBuilder();
        var optiPatcherInjectionCoordinator = new OptiScalerPayloadOptiPatcherInjectionCoordinator(
            archivePreparation.OptiScalerPayloadOptiPatcherInjector);
        var archiveReadinessFlowController = new ArchiveReadinessFlowController(
            archivePreparation.ArchivePreparationCoordinator,
            archivePreparation.OptiScalerVariantArchiveSyncService,
            optiPatcherInjectionCoordinator);
        var configApplyComposition = ConfigApplyCompositionFactory.Create(new ConfigApplyCompositionRequest
        {
            ConfigProfileApplier = configProfileServices.ConfigProfileApplier,
            IniProfileEditor = configProfileServices.IniProfileEditor,
            InstallResultPresentationResolver = installResultPresentationResolver,
            InstallCompletionMessageBuilder = installCompletionMessageBuilder
        });
        var installFlowComposition = InstallFlowCompositionFactory.Create(new InstallFlowCompositionRequest
        {
            InstallPlanBuilder = installPlanBuilder,
            ComponentInstallParityReviewBuilder = componentInstallParityReviewBuilder,
            InstallPlanInputBuilder = installPlanInputBuilder,
            InstallStartGateResolver = installStartGateResolver,
            ComponentInstallContextBuilder = componentInstallContextBuilder,
            ComponentInstallCoordinator = componentInstallCoordinator,
            InstallResultApplier = configApplyComposition.InstallResultApplier,
            InstallPopupPresenter = installPopupPresenter,
            InstallRejectionPresentationResolver = installRejectionPresentationResolver
        });

        return new InstallCompositionServices
        {
            InstallStatusResolver = installStatusResolver,
            InstallSelectionBridge = installSelectionBridge,
            GameSelectionFlowController = gameSelectionFlowController,
            InstallPlanBuilder = installPlanBuilder,
            ComponentInstallCoordinator = componentInstallCoordinator,
            ComponentInstallParityReviewBuilder = componentInstallParityReviewBuilder,
            ConfigProfileApplier = configProfileServices.ConfigProfileApplier,
            IniProfileEditor = configProfileServices.IniProfileEditor,
            InstallStartGateResolver = installStartGateResolver,
            ArchivePreparationCoordinator = archivePreparation.ArchivePreparationCoordinator,
            InstallRejectionPresentationResolver = installRejectionPresentationResolver,
            InstallResultPresentationResolver = installResultPresentationResolver,
            InstallSelectionRequestBuilder = installSelectionRequestBuilder,
            InstallPlanInputBuilder = installPlanInputBuilder,
            ComponentInstallContextBuilder = componentInstallContextBuilder,
            InstallPopupPresenter = installPopupPresenter,
            InstallCompletionMessageBuilder = installCompletionMessageBuilder,
            ArchiveReadinessFlowController = archiveReadinessFlowController,
            ConfigApplyFlowController = configApplyComposition.ConfigApplyFlowController,
            InstallResultApplier = configApplyComposition.InstallResultApplier,
            InstallFlowController = installFlowComposition.InstallFlowController,
            OptiClickUninstallPlanBuilder = uninstallPlanBuilder,
            OptiClickUninstallExecutor = uninstallExecutor
        };
    }

    private ArchivePreparationServices CreateArchivePreparation(AppSharedServices app)
    {
        var archiveCachePaths = ArchiveCachePaths.CreateDefault(app.LocalDataPathProvider);
        var archiveDownloader = _root.CreateArchiveDownloader(
            requestPreparer: app.SecurityServices.ArchiveDownloadRequestPreparer,
            serverClock: app.SecurityServices.ServerClock,
            logger: app.AppLogger);
        var archiveExtractor = _root.CreateArchiveExtractor();
        var archiveManifestStore = _root.CreateArchiveDownloadManifestStore(archiveCachePaths.ManifestRoot);
        var optiScalerPayloadCacheService = new OptiScalerPayloadCacheService(
            archiveDownloader,
            archiveExtractor,
            archiveManifestStore,
            new OptiScalerPayloadValidator());
        var archivePreparationCoordinator = new ArchivePreparationCoordinator(
            archiveCachePaths,
            new VersionedArchivePreparationService(archiveDownloader, archiveManifestStore, archiveExtractor),
            new OptiPatcherArchivePreparationService(archiveDownloader, archiveExtractor, archiveManifestStore));
        var optiScalerPayloadOptiPatcherInjector = new OptiScalerPayloadOptiPatcherInjector();
        var optiScalerVariantManifestStore = new OptiScalerVariantManifestStore(
            archiveCachePaths.ManifestRoot,
            app.AppLogger);
        var optiScalerVariantArchiveSyncService = new OptiScalerVariantArchiveSyncService(
            archiveCachePaths,
            optiScalerPayloadCacheService,
            optiScalerVariantManifestStore,
            archiveManifestStore,
            new OptiScalerPayloadValidator(),
            app.AppLogger);
        return new ArchivePreparationServices
        {
            ArchiveDownloader = archiveDownloader,
            ArchiveExtractor = archiveExtractor,
            ArchivePreparationCoordinator = archivePreparationCoordinator,
            OptiScalerVariantArchiveSyncService = optiScalerVariantArchiveSyncService,
            OptiScalerPayloadOptiPatcherInjector = optiScalerPayloadOptiPatcherInjector,
            ArchiveCachePaths = archiveCachePaths
        };
    }

    private IComponentInstallCoordinator CreateComponentInstallCoordinator(
        AppSharedServices app,
        IInstallFileSystem installFileSystem,
        IFileSignatureDetectors signatures,
        IArchiveDownloader archiveDownloader,
        IArchiveExtractor archiveExtractor)
    {
        var archiveSourceReader = _root.CreateArchiveSourceReader(
            installFileSystem,
            archiveDownloader,
            archiveExtractor);
        var dllPayloadInstaller = _root.CreateDllPayloadInstaller(installFileSystem, archiveSourceReader);
        var proxyDllNameResolver = _root.CreateProxyDllNameResolver(installFileSystem, signatures);

        return _root.CreateComponentInstallCoordinator(
            _root.CreateOptiScalerCoreInstaller(installFileSystem, signatures, proxyDllNameResolver, app.AppLogger),
            _root.CreateExtraBundleInstaller(archiveDownloader, archiveExtractor, installFileSystem),
            _root.CreateSpecialKInstaller(dllPayloadInstaller, installFileSystem, signatures),
            _root.CreateReFrameworkInstaller(dllPayloadInstaller, installFileSystem, signatures),
            _root.CreateUnreal5Installer(archiveSourceReader, archiveExtractor, installFileSystem),
            app.AppLogger);
    }

    private ConfigProfileServices CreateConfigProfileServices()
    {
        var profilePathResolver = _root.CreateProfilePathResolver();
        var iniProfileEditor = _root.CreateIniProfileEditor(profilePathResolver);
        var configProfileApplier = new LazyConfigProfileApplier(() =>
            _root.CreateConfigProfileApplier(
                iniProfileEditor,
                _root.CreateUnrealIniProfileEditor(profilePathResolver),
                _root.CreateXmlProfileEditor(profilePathResolver),
                _root.CreateJsonProfileEditor(profilePathResolver),
                _root.CreateRegistryProfileApplier(_root.CreateWindowsRegistryWriter())));

        return new ConfigProfileServices
        {
            IniProfileEditor = iniProfileEditor,
            ConfigProfileApplier = configProfileApplier
        };
    }

    private sealed class LazyComponentInstallCoordinator : IComponentInstallCoordinator
    {
        private readonly Lazy<IComponentInstallCoordinator> _inner;

        public LazyComponentInstallCoordinator(Func<IComponentInstallCoordinator> factory)
        {
            _inner = new Lazy<IComponentInstallCoordinator>(factory);
        }

        public Task<ComponentInstallResult> ExecuteAsync(ComponentInstallContext context, CancellationToken cancellationToken = default)
        {
            return _inner.Value.ExecuteAsync(context, cancellationToken);
        }
    }

    private sealed class LazyConfigProfileApplier : IConfigProfileApplier
    {
        private readonly Lazy<IConfigProfileApplier> _inner;

        public LazyConfigProfileApplier(Func<IConfigProfileApplier> factory)
        {
            _inner = new Lazy<IConfigProfileApplier>(factory);
        }

        public ConfigProfileApplyResult Apply(ConfigProfileApplyContext context)
        {
            return _inner.Value.Apply(context);
        }
    }

    private sealed class LazyOptiClickUninstallPlanBuilder : IOptiClickUninstallPlanBuilder
    {
        private readonly Lazy<IOptiClickUninstallPlanBuilder> _inner;

        public LazyOptiClickUninstallPlanBuilder(Func<IOptiClickUninstallPlanBuilder> factory)
        {
            _inner = new Lazy<IOptiClickUninstallPlanBuilder>(factory);
        }

        public OptiClick.Infrastructure.Install.Uninstall.UninstallPlan BuildPlan(string targetPath)
        {
            return _inner.Value.BuildPlan(targetPath);
        }

        public OptiClick.Infrastructure.Install.Uninstall.UninstallPlan BuildPlan(OptiClickUninstallPlanBuildRequest request)
        {
            return _inner.Value.BuildPlan(request);
        }
    }

    private sealed class LazyOptiClickUninstallExecutor : IOptiClickUninstallExecutor
    {
        private readonly Lazy<IOptiClickUninstallExecutor> _inner;

        public LazyOptiClickUninstallExecutor(Func<IOptiClickUninstallExecutor> factory)
        {
            _inner = new Lazy<IOptiClickUninstallExecutor>(factory);
        }

        public Task<OptiClick.Infrastructure.Install.Uninstall.UninstallExecutionResult> ExecuteAsync(
            OptiClick.Infrastructure.Install.Uninstall.UninstallPlan plan,
            CancellationToken cancellationToken = default)
        {
            return _inner.Value.ExecuteAsync(plan, cancellationToken);
        }
    }
}
