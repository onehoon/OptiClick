using OptiClick.Wpf.Localization;

namespace OptiClick.Wpf.Shell.Scan;

public sealed record ScanFlowText
{
    public required string NavScan { get; init; }
    public required string ScanServiceMissing { get; init; }
    public required string ScanStartupSkippedCatalogNotReady { get; init; }
    public required string ScanStartupNoFolders { get; init; }
    public required string ScanNoFolderSelected { get; init; }
    public required string RuntimeCatalogNotReadyForScan { get; init; }
    public required string RuntimeCatalogPipelineMissingTitle { get; init; }
    public required string RuntimeCatalogNotReadyScanBlocked { get; init; }
    public required string ScanStartupMatchedGamesFromExecutables { get; init; }
    public required string ScanStartupNoSupportedGames { get; init; }
    public required string ScanNoExecutableFound { get; init; }
    public required string ScanNoSupportedGamesMatchedFromExecutables { get; init; }
    public required string ScanLastScanMatchedGamesFromExecutables { get; init; }
    public required string ScanStartupFailedTryAgain { get; init; }
    public required string ScanFailedSeeLog { get; init; }

    public static ScanFlowText FromAppStrings(AppStrings strings)
    {
        ArgumentNullException.ThrowIfNull(strings);
        return new ScanFlowText
        {
            NavScan = strings.NavScan,
            ScanServiceMissing = strings.ScanServiceMissing,
            ScanStartupSkippedCatalogNotReady = strings.ScanStartupSkippedCatalogNotReady,
            ScanStartupNoFolders = strings.ScanStartupNoFolders,
            ScanNoFolderSelected = strings.ScanNoFolderSelected,
            RuntimeCatalogNotReadyForScan = strings.RuntimeCatalogNotReadyForScan,
            RuntimeCatalogPipelineMissingTitle = strings.RuntimeCatalogPipelineMissingTitle,
            RuntimeCatalogNotReadyScanBlocked = strings.RuntimeCatalogNotReadyScanBlocked,
            ScanStartupMatchedGamesFromExecutables = strings.ScanStartupMatchedGamesFromExecutables,
            ScanStartupNoSupportedGames = strings.ScanStartupNoSupportedGames,
            ScanNoExecutableFound = strings.ScanNoExecutableFound,
            ScanNoSupportedGamesMatchedFromExecutables = strings.ScanNoSupportedGamesMatchedFromExecutables,
            ScanLastScanMatchedGamesFromExecutables = strings.ScanLastScanMatchedGamesFromExecutables,
            ScanStartupFailedTryAgain = strings.ScanStartupFailedTryAgain,
            ScanFailedSeeLog = strings.ScanFailedSeeLog
        };
    }
}
