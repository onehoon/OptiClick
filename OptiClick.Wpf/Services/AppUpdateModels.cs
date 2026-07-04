namespace OptiClick.Wpf.Services;

public enum AppUpdatePackageType
{
    Unknown = 0,
    Exe,
    Zip,
    SevenZip
}

public sealed record AppUpdateInfo(
    string CurrentVersion,
    string LatestVersion,
    string DisplayVersion,
    string DownloadUrl,
    string DownloadFileName,
    string Note,
    AppUpdatePackageType PackageType,
    bool IsForced,
    string Sha256 = "");

public sealed record AppUpdatePreparedFile(
    string DownloadedOriginalPath,
    string ResolvedUpdateExePath,
    string ResolvedUpdateExeFileName,
    string FinalCopiedExePath);
