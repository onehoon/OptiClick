using OptiClick.Core.Scan;
using OptiClick.Wpf.Services;
using OptiClick.Wpf.Shell.Scan;

namespace OptiClick.Wpf.Composition;

public sealed record ScanCompositionServices
{
    public required IFolderPickerService FolderPickerService { get; init; }
    public required IScanFolderDiscoveryService ScanFolderDiscoveryService { get; init; }
    public required IScanFolderManifestStore ScanFolderManifestStore { get; init; }
    public required IScanFileSystemProbe ScanFileSystemProbe { get; init; }
    public required IShellGameScanPipeline ScanPipeline { get; init; }
    public required ScanFlowController ScanFlowController { get; init; }
}

public sealed class ScanComposition
{
    private readonly AppCompositionRoot _root;

    public ScanComposition(AppCompositionRoot root)
    {
        _root = root ?? throw new ArgumentNullException(nameof(root));
    }

    public ScanCompositionServices CreateScanServices(
        AppSharedServices app,
        RuntimeCompositionServices runtime)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(runtime);

        var scanFileSystemProbe = _root.CreateScanFileSystemProbe();
        var executableScanService = _root.CreateExecutableScanService(app.AppLogger);
        var scanMatcher = _root.CreateShellGameScanMatcher(app.AppLogger, scanFileSystemProbe);
        var scanPipeline = _root.CreateShellGameScanPipeline(
            executableScanService,
            scanMatcher,
            _root.CreateShellGameExeMatchIndexBuilder(),
            app.AppLogger);

        return new ScanCompositionServices
        {
            FolderPickerService = _root.CreateFolderPickerService(),
            ScanFolderDiscoveryService = _root.CreateScanFolderDiscoveryService(),
            ScanFolderManifestStore = _root.CreateScanFolderManifestStore(app.LocalDataPathProvider, app.AppLogger),
            ScanFileSystemProbe = scanFileSystemProbe,
            ScanPipeline = scanPipeline,
            ScanFlowController = new ScanFlowController(scanPipeline, runtime.ShellGameCardViewModelFactory)
        };
    }
}
