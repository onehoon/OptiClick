namespace OptiClick.Core.Scan;

public sealed class DetectedExecutable
{
    public string FileName { get; init; } = "";
    public string FullPath { get; init; } = "";
}

public sealed class ExecutableScanResult
{
    public string FolderPath { get; init; } = "";
    public IReadOnlyList<DetectedExecutable> Executables { get; init; } = [];
    public int ScannedExecutableCount { get; init; }
    public int SkippedDirectoryCount { get; init; }
}

public interface IExecutableScanService
{
    Task<ExecutableScanResult> ScanAsync(string folderPath, CancellationToken cancellationToken = default);

    Task<ExecutableScanResult> ScanAsync(
        string folderPath,
        IReadOnlySet<string> allowedExeNames,
        CancellationToken cancellationToken = default);
}
