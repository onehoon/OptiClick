using OptiClick.Core.Abstractions;
using System.Net.Http;
using OptiClick.Wpf.Configuration;
using OptiClick.Wpf.Diagnostics;
using OptiClick.Wpf.Install.Planning;
using OptiClick.Wpf.Install.Config;
using OptiClick.Wpf.Install.Precheck;
using OptiClick.Wpf.Install.Archives;
using OptiClick.Wpf.Install.Execution;
using OptiClick.Wpf.Install.Flow;
using OptiClick.Wpf.Install.FileSystem;
using OptiClick.Wpf.Install.Gates;
using OptiClick.Wpf.Install.Uninstall;
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
using OptiClick.Wpf.ViewModels;
using OptiClickShell;

namespace OptiClick.Wpf.Composition;

public sealed partial class AppCompositionRoot
{
    public IInstallPlanBuilder CreateInstallPlanBuilder()
    {
        return new InstallPlanBuilder();
    }

    public IComponentInstallParityReviewBuilder CreateComponentInstallParityReviewBuilder()
    {
        return new ComponentInstallParityReviewBuilder();
    }

    public IExecutableScanService CreateExecutableScanService(IAppLogger? logger = null)
    {
        return new ExecutableScanService(logger);
    }

    public IShellGameExeMatchIndexBuilder CreateShellGameExeMatchIndexBuilder()
    {
        return new ShellGameExeMatchIndexBuilder();
    }

    public IShellGameScanMatcher CreateShellGameScanMatcher(IAppLogger? logger = null)
    {
        return new ShellGameScanMatcher(logger);
    }

    public IShellGameScanPipeline CreateShellGameScanPipeline(
        IExecutableScanService executableScanService,
        IShellGameScanMatcher scanMatcher,
        IShellGameExeMatchIndexBuilder? matchIndexBuilder = null,
        IAppLogger? logger = null)
    {
        return new ShellGameScanPipeline(
            executableScanService,
            scanMatcher,
            matchIndexBuilder ?? CreateShellGameExeMatchIndexBuilder(),
            logger);
    }

    public ArchiveCachePaths CreateArchiveCachePaths()
    {
        return ArchiveCachePaths.CreateDefault(CreateAppLocalDataPathProvider());
    }

    public IArchiveDownloadManifestStore CreateArchiveDownloadManifestStore(string manifestRoot)
    {
        return new ArchiveDownloadManifestStore(manifestRoot);
    }

    public IArchiveDownloader CreateArchiveDownloader(HttpClient? httpClient = null)
    {
        return new ArchiveDownloader(httpClient ?? new HttpClient());
    }

    public IArchiveExtractor CreateArchiveExtractor()
    {
        return new WindowsTarArchiveExtractor(
            new ArchiveProcessRunner(),
            new OverrideableOperatingSystemSupportPolicy(
                new Windows11OnlyOperatingSystemSupportPolicy()));
    }

    public IVersionedArchivePreparationService CreateVersionedArchivePreparationService(
        IArchiveDownloader archiveDownloader,
        IArchiveDownloadManifestStore manifestStore,
        IArchiveExtractor archiveExtractor)
    {
        return new VersionedArchivePreparationService(
            archiveDownloader,
            manifestStore,
            archiveExtractor);
    }

    public IInstallFileSystem CreateInstallFileSystem()
    {
        return new InstallFileSystem();
    }

    public IFileSignatureDetectors CreateFileSignatureDetectors(IInstallFileSystem fileSystem)
    {
        return new FileSignatureDetectors(fileSystem);
    }

    public IArchiveSourceReader CreateArchiveSourceReader(
        IInstallFileSystem fileSystem,
        IArchiveDownloader downloader,
        IArchiveExtractor extractor)
    {
        var tempRoot = CreateAppLocalDataPathProvider().InstallExecutionTempDirectory;
        return new ArchiveSourceReader(
            fileSystem,
            downloader,
            extractor,
            options: new OptiClick.Infrastructure.Archives.ArchiveSourceReaderOptions
            {
                InstallExecutionTempRoot = tempRoot
            });
    }

    public IDllPayloadInstaller CreateDllPayloadInstaller(
        IInstallFileSystem fileSystem,
        IArchiveSourceReader archiveSourceReader)
    {
        return new DllPayloadInstaller(fileSystem, archiveSourceReader);
    }

    public IProxyDllNameResolver CreateProxyDllNameResolver(
        IInstallFileSystem fileSystem,
        IFileSignatureDetectors detectors)
    {
        return new ProxyDllNameResolver(fileSystem, detectors);
    }

    public IOptiScalerCoreInstaller CreateOptiScalerCoreInstaller(
        IInstallFileSystem fileSystem,
        IFileSignatureDetectors detectors,
        IProxyDllNameResolver proxyResolver,
        IAppLogger? logger = null)
    {
        return new OptiScalerCoreInstaller(fileSystem, detectors, proxyResolver, logger);
    }

    public IReFrameworkInstaller CreateReFrameworkInstaller(
        IDllPayloadInstaller dllPayloadInstaller,
        IInstallFileSystem fileSystem,
        IFileSignatureDetectors detectors)
    {
        return new ReFrameworkInstaller(dllPayloadInstaller, fileSystem, detectors);
    }

    public ISpecialKInstaller CreateSpecialKInstaller(
        IDllPayloadInstaller dllPayloadInstaller,
        IInstallFileSystem fileSystem,
        IFileSignatureDetectors detectors)
    {
        return new SpecialKInstaller(dllPayloadInstaller, fileSystem, detectors);
    }

    public IUltimateAsiLoaderInstaller CreateUltimateAsiLoaderInstaller(
        IDllPayloadInstaller dllPayloadInstaller,
        IInstallFileSystem fileSystem,
        IFileSignatureDetectors detectors)
    {
        return new UltimateAsiLoaderInstaller(dllPayloadInstaller, fileSystem, detectors);
    }

    public IOptiPatcherInstaller CreateOptiPatcherInstaller(
        IArchiveSourceReader archiveSourceReader,
        IInstallFileSystem fileSystem)
    {
        return new OptiPatcherInstaller(archiveSourceReader, fileSystem);
    }

    public IUnreal5Installer CreateUnreal5Installer(
        IArchiveSourceReader archiveSourceReader,
        IArchiveExtractor archiveExtractor,
        IInstallFileSystem fileSystem)
    {
        return new Unreal5Installer(archiveSourceReader, archiveExtractor, fileSystem);
    }

    public IExtraBundleInstaller CreateExtraBundleInstaller(
        IArchiveDownloader downloader,
        IArchiveExtractor extractor,
        IInstallFileSystem fileSystem)
    {
        var tempRoot = CreateAppLocalDataPathProvider().InstallExecutionTempDirectory;
        return new ExtraBundleInstaller(downloader, extractor, fileSystem, tempRoot);
    }

    public IFsr4Installer CreateFsr4Installer(IArchiveExtractor extractor, IInstallFileSystem fileSystem)
    {
        return new Fsr4Installer(extractor, fileSystem);
    }

    public IComponentInstallCoordinator CreateComponentInstallCoordinator(
        IOptiScalerCoreInstaller coreInstaller,
        IExtraBundleInstaller extraBundleInstaller,
        IUltimateAsiLoaderInstaller ualInstaller,
        ISpecialKInstaller specialKInstaller,
        IReFrameworkInstaller reFrameworkInstaller,
        IOptiPatcherInstaller optiPatcherInstaller,
        IUnreal5Installer unreal5Installer,
        IFsr4Installer fsr4Installer,
        IAppLogger? logger = null)
    {
        return new ComponentInstallCoordinator(
            coreInstaller,
            extraBundleInstaller,
            ualInstaller,
            specialKInstaller,
            reFrameworkInstaller,
            optiPatcherInstaller,
            unreal5Installer,
            fsr4Installer,
            logger);
    }

    public ProfilePathResolver CreateProfilePathResolver()
    {
        return new ProfilePathResolver();
    }

    public IniProfileEditor CreateIniProfileEditor(ProfilePathResolver profilePathResolver)
    {
        return new IniProfileEditor(profilePathResolver);
    }

    public UnrealIniProfileEditor CreateUnrealIniProfileEditor(ProfilePathResolver profilePathResolver)
    {
        return new UnrealIniProfileEditor(profilePathResolver);
    }

    public XmlProfileEditor CreateXmlProfileEditor(ProfilePathResolver profilePathResolver)
    {
        return new XmlProfileEditor(profilePathResolver);
    }

    public JsonProfileEditor CreateJsonProfileEditor(ProfilePathResolver profilePathResolver)
    {
        return new JsonProfileEditor(profilePathResolver);
    }

    public RegistryProfileApplier CreateRegistryProfileApplier(IRegistryWriter registryWriter)
    {
        return new RegistryProfileApplier(registryWriter);
    }

    public IRegistryWriter CreateNoOpRegistryWriter()
    {
        return new NoOpRegistryWriter();
    }

    public IRegistryWriter CreateWindowsRegistryWriter()
    {
        return new WindowsRegistryWriter();
    }

    public IConfigProfileApplier CreateConfigProfileApplier(
        IniProfileEditor iniProfileEditor,
        UnrealIniProfileEditor unrealIniProfileEditor,
        XmlProfileEditor xmlProfileEditor,
        JsonProfileEditor jsonProfileEditor,
        RegistryProfileApplier registryProfileApplier)
    {
        return new ConfigProfileApplier(
            iniProfileEditor,
            unrealIniProfileEditor,
            xmlProfileEditor,
            jsonProfileEditor,
            registryProfileApplier);
    }

    public IFileVersionInfoReader CreateFileVersionInfoReader()
    {
        return new WindowsFileVersionInfoReader();
    }

    public IBinaryOwnerDetector CreateBinaryOwnerDetector(
        IFileSignatureDetectors fileSignatureDetectors,
        IFileVersionInfoReader fileVersionInfoReader)
    {
        return new BinaryOwnerDetector(fileSignatureDetectors, fileVersionInfoReader);
    }

    public IModPrecheckScanner CreateModPrecheckScanner(
        IInstallFileSystem fileSystem,
        IBinaryOwnerDetector binaryOwnerDetector)
    {
        return new ModPrecheckScanner(fileSystem, binaryOwnerDetector);
    }

    public ModConflictFindingBuilder CreateModConflictFindingBuilder()
    {
        return new ModConflictFindingBuilder();
    }

    public ModConflictNoticeBuilder CreateModConflictNoticeBuilder()
    {
        return new ModConflictNoticeBuilder();
    }

    public ReFrameworkLegacyFindingService CreateReFrameworkLegacyFindingService()
    {
        return new ReFrameworkLegacyFindingService();
    }

    public IInstallPrecheckHandler CreateBaseInstallPrecheckHandler(
        IModPrecheckScanner modPrecheckScanner,
        ModConflictFindingBuilder findingBuilder,
        ReFrameworkLegacyFindingService reFrameworkLegacyFindingService,
        IProxyDllNameResolver proxyDllNameResolver)
    {
        return new BaseInstallPrecheckHandler(
            modPrecheckScanner,
            findingBuilder,
            reFrameworkLegacyFindingService,
            proxyDllNameResolver);
    }

    public InstallPrecheckHandlerRegistry CreateInstallPrecheckHandlerRegistry(IInstallPrecheckHandler baseHandler)
    {
        return new InstallPrecheckHandlerRegistry(baseHandler);
    }

    public InstallSelectionPrecheckFlow CreateInstallSelectionPrecheckFlow()
    {
        return new InstallSelectionPrecheckFlow();
    }

    public IInstallButtonStateResolver CreateInstallButtonStateResolver()
    {
        return new InstallButtonStateResolver();
    }

    public IInstallButtonPresentationResolver CreateInstallButtonPresentationResolver()
    {
        return new InstallButtonPresentationResolver();
    }

    public IInstallEntryGateResolver CreateInstallEntryGateResolver()
    {
        return new InstallEntryGateResolver();
    }

    public IWritePermissionProbe CreateWritePermissionProbe()
    {
        return new WritePermissionProbe();
    }

    public IInstallStartGateResolver CreateInstallStartGateResolver()
    {
        return new InstallStartGateResolver(CreateWritePermissionProbe());
    }

    public IInstallRejectionPresentationResolver CreateInstallRejectionPresentationResolver()
    {
        return new InstallRejectionPresentationResolver();
    }

    public IInstallResultPresentationResolver CreateInstallResultPresentationResolver()
    {
        return new InstallResultPresentationResolver();
    }

    public IInstallStatusResolver CreateInstallStatusResolver(
        IInstallFileSystem fileSystem,
        IFileVersionInfoReader versionInfoReader)
    {
        return new InstallStatusResolver(fileSystem, versionInfoReader);
    }

    public IOptiClickUninstallPlanBuilder CreateOptiClickUninstallPlanBuilder(
        IInstallFileSystem fileSystem,
        IFileSignatureDetectors signatureDetectors,
        IFileVersionInfoReader versionInfoReader,
        IAppLogger? logger = null)
    {
        return new OptiClickUninstallPlanBuilder(fileSystem, signatureDetectors, versionInfoReader, logger);
    }

    public IOptiClickUninstallExecutor CreateOptiClickUninstallExecutor(
        IInstallFileSystem fileSystem,
        IFileSignatureDetectors signatureDetectors,
        IFileVersionInfoReader versionInfoReader,
        IAppLogger? logger = null)
    {
        return new OptiClickUninstallExecutor(fileSystem, signatureDetectors, versionInfoReader, logger);
    }

    public IInstallSummaryStringsResolver CreateInstallSummaryStringsResolver()
    {
        return new DefaultInstallSummaryStringsResolver(new AppStringsProvider());
    }

    public IInstallSummaryViewModelBuilder CreateInstallSummaryViewModelBuilder(IInstallSummaryStringsResolver stringsResolver)
    {
        return new InstallSummaryViewModelBuilder(stringsResolver);
    }

    public ICardInstallStatusUpdateResolver CreateCardInstallStatusUpdateResolver()
    {
        return new CardInstallStatusUpdateResolver();
    }

    public IInstallUiStateInputBuilder CreateInstallUiStateInputBuilder()
    {
        return new InstallUiStateInputBuilder();
    }

    public IShellInstallSelectionBridge CreateShellInstallSelectionBridge(IAppStringsProvider? stringsProvider = null)
    {
        var fileSystem = CreateInstallFileSystem();
        var fileSignatures = CreateFileSignatureDetectors(fileSystem);
        var versionInfoReader = CreateFileVersionInfoReader();
        var binaryOwnerDetector = CreateBinaryOwnerDetector(fileSignatures, versionInfoReader);
        var modPrecheckScanner = CreateModPrecheckScanner(fileSystem, binaryOwnerDetector);
        var findingBuilder = CreateModConflictFindingBuilder();
        var legacyFindingService = CreateReFrameworkLegacyFindingService();
        var proxyResolver = CreateProxyDllNameResolver(fileSystem, fileSignatures);
        var precheckHandler = CreateBaseInstallPrecheckHandler(
            modPrecheckScanner,
            findingBuilder,
            legacyFindingService,
            proxyResolver);

        var summaryBuilder = CreateInstallSummaryViewModelBuilder(CreateInstallSummaryStringsResolver());
        return new ShellInstallSelectionBridge(
            CreateInstallPrecheckHandlerRegistry(precheckHandler),
            CreateInstallSelectionPrecheckFlow(),
            CreateModConflictNoticeBuilder(),
            new ShellGameActionAvailabilityResolver(),
            CreateInstallUiStateInputBuilder(),
            CreateInstallButtonStateResolver(),
            CreateInstallButtonPresentationResolver(),
            summaryBuilder,
            stringsProvider ?? new AppStringsProvider());
    }
}

