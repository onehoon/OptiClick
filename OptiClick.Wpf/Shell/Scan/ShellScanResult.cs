namespace OptiClick.Wpf.Shell.Scan;

public sealed class ShellScanResult
{
    public string FolderPath { get; init; } = "";
    public IReadOnlyList<ShellDetectedExecutable> Executables { get; init; } = [];
    public int ScannedExecutableCount { get; init; }
    public int SkippedDirectoryCount { get; init; }
}
