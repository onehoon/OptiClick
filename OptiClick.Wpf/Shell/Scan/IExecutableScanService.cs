namespace OptiClick.Wpf.Shell.Scan;

public interface IExecutableScanService
{
    Task<ShellScanResult> ScanAsync(string folderPath, CancellationToken cancellationToken = default);

    Task<ShellScanResult> ScanAsync(
        string folderPath,
        IReadOnlySet<string> allowedExeNames,
        CancellationToken cancellationToken = default);
}
