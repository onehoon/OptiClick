using OptiClick.Wpf.Shell.Runtime;
using OptiClick.Wpf.Shell.Scan;

namespace OptiClick.Wpf.ViewModels;

internal static class MainSelectionScanPortFactory
{
    public static MainSelectionScanRuntimePort CreateRuntimePort(RuntimeShellState runtimeShellState)
    {
        ArgumentNullException.ThrowIfNull(runtimeShellState);

        return new MainSelectionScanRuntimePort
        {
            ReadRemoteCatalog = () => runtimeShellState.LatestRemoteCatalog,
            ReadRuntimeContext = () => runtimeShellState.LatestRuntimeContext,
            ReadModuleDownloadLinks = () => runtimeShellState.ModuleDownloadLinks,
            ReadArchiveReadiness = () => runtimeShellState.LatestArchiveReadiness,
            ReadRemoteCatalogErrorCode = () => runtimeShellState.LatestRemoteCatalogErrorCode
        };
    }

    public static MainSelectionScanScannedGamePort CreateScannedGamePort(ScannedGameState scannedGameState)
    {
        ArgumentNullException.ThrowIfNull(scannedGameState);

        return new MainSelectionScanScannedGamePort
        {
            ReadMatchesByGameId = () => scannedGameState.MatchByGameId,
            ReadTargetPathsByGameId = () => scannedGameState.TargetPathByGameId,
            ContainsGameId = scannedGameState.ContainsGameId
        };
    }
}
