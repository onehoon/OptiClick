namespace OptiClick.Wpf.Shell.Scan;

public interface IShellGameScanPipeline
{
    Task<ShellGameScanPipelineResult> ScanAsync(
        ShellGameScanRequest request,
        CancellationToken cancellationToken = default);
}
