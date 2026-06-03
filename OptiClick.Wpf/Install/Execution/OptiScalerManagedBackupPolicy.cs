namespace OptiClick.Wpf.Install.Execution;

public static class OptiScalerManagedBackupPolicy
{
    public static readonly IReadOnlyList<string> TargetFileNames =
    [
        "OptiScaler.asi",
        "OptiScaler.dll",
        "dxgi.dll",
        "winmm.dll",
        "d3d12.dll",
        "dbghelp.dll",
        "version.dll",
        "wininet.dll",
        "winhttp.dll"
    ];
}
