namespace OptiClick.Core.Install;

public static class OptiScalerLegacyCleanupPolicy
{
    public static readonly IReadOnlyList<string> TargetFileNames =
    [
        "nvapi64.dll",
        "nvngx.dll"
    ];
}
