namespace OptiClick.Core.Install;

public static class OptiScalerLegacyCleanupPolicy
{
    // Legacy OptiScaler used these filenames directly; filename-based cleanup is intentional.
    public static readonly IReadOnlyList<string> TargetFileNames =
    [
        "nvapi64.dll",
        "nvngx.dll"
    ];
}
