namespace OptiClick.Wpf.Shell.Startup;

public enum CoverCacheBootstrapState
{
    NotRequired,
    Pending,
    Downloading,
    Extracting,
    Completed,
    FailedFallbackEnabled
}
