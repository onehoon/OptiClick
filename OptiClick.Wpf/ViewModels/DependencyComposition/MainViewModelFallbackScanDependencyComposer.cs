using OptiClick.Wpf.Install.Gates;
using OptiClick.Wpf.Services;
using OptiClick.Wpf.Shell.Scan;
using OptiClick.Wpf.ViewModels.Sections.Scan;
using OptiClick.Infrastructure.Scan;

namespace OptiClick.Wpf.ViewModels.DependencyComposition;

internal static class MainViewModelFallbackScanDependencyComposer
{
    public static ScanDependencyComposition Compose(
        MainViewModelScanDependencies scanDependencies,
        MainViewModelRuntimeDependencies runtimeDependencies)
    {
        var scanFileSystemProbe = scanDependencies.ScanFileSystemProbe ?? new ScanFileSystemProbe();
        var scanFlowController = scanDependencies.ScanFlowController ?? new ScanFlowController(scanDependencies.ScanPipeline, runtimeDependencies.ShellGameCardViewModelFactory);
        var scanFolderListController = scanDependencies.ScanFolderListController
                                       ?? new ScanFolderListController(
                                           scanDependencies.ScanFolderManifestStore,
                                           scanFileSystemProbe);
        var scanVisibleGameResolver = new ScanVisibleGameResolver();
        var scanFolderActionController = scanDependencies.ScanFolderActionController
                                         ?? new ScanFolderActionController(
                                             scanFolderListController,
                                             scanDependencies.FolderPickerService);
        var scanResultCoordinatorFactory = scanDependencies.ScanResultCoordinatorFactory ?? new ScanResultCoordinatorFactory();
        var scanOrchestratorFactory = scanDependencies.ScanOrchestratorFactory ?? new ScanOrchestratorFactory();

        return new ScanDependencyComposition
        {
            FolderPickerService = scanDependencies.FolderPickerService,
            ScanFolderDiscoveryService = scanDependencies.ScanFolderDiscoveryService,
            ScanFileSystemProbe = scanFileSystemProbe,
            ScanFlowController = scanFlowController,
            ScanFolderListController = scanFolderListController,
            ScanFolderActionController = scanFolderActionController,
            ScanResultCoordinatorFactory = scanResultCoordinatorFactory,
            ScanOrchestratorFactory = scanOrchestratorFactory,
            ScanVisibleGameResolver = scanVisibleGameResolver
        };
    }
}
