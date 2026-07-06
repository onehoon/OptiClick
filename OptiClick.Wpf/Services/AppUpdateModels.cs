namespace OptiClick.Wpf.Services;

public sealed record AppUpdateInfo(
    string CurrentVersion,
    string LatestVersion,
    string Notes,
    Velopack.UpdateInfo VelopackUpdateInfo);
