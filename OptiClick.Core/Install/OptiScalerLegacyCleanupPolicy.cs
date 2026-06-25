namespace OptiClick.Core.Install;

public static class OptiScalerLegacyCleanupPolicy
{
    // Legacy OptiScaler/fakenvapi used these filenames directly; filename-based cleanup is intentional.
    public static readonly IReadOnlyList<string> TargetFileNames =
    [
        "nvapi64.dll",
        "nvngx.dll",
        "fakenvapi.dll",
        "fakenvapi.ini",
        "fakenvapi.log"
    ];
}
