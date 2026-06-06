using OptiClick.Core.Runtime;
using OptiClick.Wpf.Install.Flow;
using OptiClick.Wpf.Install.Planning;
using OptiClick.Wpf.Localization;
using OptiClick.Wpf.Shell.RuntimeData;
using OptiClick.Wpf.Services;
using OptiClick.Wpf.Shell.Games;
using OptiClick.Wpf.Shell.Runtime;
using OptiClick.Wpf.Shell.Scan;
using OptiClick.Wpf.Shell.Selection;

namespace OptiClick.Wpf.ViewModels;

public sealed class MainViewModelFlowRequestFactory
{
    public RuntimeCatalogFlowRequest BuildRuntimeCatalogRequest(
        RuntimeContext latestRuntimeContext,
        AppLanguage selectedLanguage,
        AppStrings strings)
    {
        return new RuntimeCatalogFlowRequest
        {
            LatestRuntimeContext = latestRuntimeContext,
            SelectedLanguage = selectedLanguage,
            Strings = strings
        };
    }

    public ScanFlowRequest BuildScanRequest(
        IReadOnlyList<string> scanFolders,
        ShellGameCatalog latestRemoteCatalog,
        RuntimeContext latestRuntimeContext,
        AppStrings strings,
        IReadOnlyDictionary<string, ShellGameMatchResult> matchByGameId,
        IReadOnlyDictionary<string, string> targetPathByGameId,
        IReadOnlyDictionary<string, object?> moduleDownloadLinks,
        string latestRemoteCatalogErrorCode)
    {
        return new ScanFlowRequest
        {
            ScanFolders = scanFolders,
            LatestRemoteCatalog = latestRemoteCatalog,
            LatestRuntimeContext = latestRuntimeContext,
            Strings = strings,
            CurrentMatchByGameId = matchByGameId,
            CurrentTargetPathByGameId = targetPathByGameId,
            ModuleDownloadLinks = moduleDownloadLinks,
            LatestRemoteCatalogErrorCode = latestRemoteCatalogErrorCode
        };
    }

    public GameSelectionFlowRequest BuildGameSelectionRequest(
        GameCardViewModel selectedCard,
        int selectedIndex,
        IReadOnlyList<GameCardViewModel> games,
        ShellInstallSelectionState previousSelectionState,
        IReadOnlyDictionary<string, ShellGameMatchResult> matchByGameId,
        IReadOnlyDictionary<string, string> targetPathByGameId,
        IReadOnlyDictionary<string, object?> moduleDownloadLinks,
        ArchiveReadinessSnapshot latestArchiveReadiness,
        AppLanguage selectedLanguage,
        bool isInstallExecutionInProgress,
        bool isAppUpdateInProgress,
        bool multiGpuBlocked,
        bool gpuSelectionPending,
        string latestRemoteCatalogErrorCode = "")
    {
        return new GameSelectionFlowRequest
        {
            SelectedCard = selectedCard,
            SelectedIndex = selectedIndex,
            Games = games,
            PreviousSelectionState = previousSelectionState,
            MatchByGameId = matchByGameId,
            TargetPathByGameId = targetPathByGameId,
            ModuleDownloadLinks = moduleDownloadLinks,
            LatestArchiveReadiness = latestArchiveReadiness,
            SelectedLanguage = selectedLanguage,
            IsInstallExecutionInProgress = isInstallExecutionInProgress,
            IsAppUpdateInProgress = isAppUpdateInProgress,
            MultiGpuBlocked = multiGpuBlocked,
            GpuSelectionPending = gpuSelectionPending,
            LatestRemoteCatalogErrorCode = latestRemoteCatalogErrorCode
        };
    }

    public InstallFlowRequest BuildInstallRequest(
        GameCardViewModel selectedGame,
        int selectedIndex,
        RuntimeContext latestRuntimeContext,
        ArchiveReadinessSnapshot latestArchiveReadiness,
        ShellInstallSelectionState selectionState,
        IReadOnlyDictionary<string, ShellGameMatchResult> matchByGameId,
        IReadOnlyDictionary<string, string> targetPathByGameId,
        IReadOnlyDictionary<string, object?> moduleDownloadLinks,
        IReadOnlyDictionary<string, string> commonOptiScalerIniSettings,
        bool isWindowsSupported,
        bool isInstallExecutionInProgress,
        bool isAppUpdateInProgress,
        AppStrings strings,
        string latestRemoteCatalogErrorCode = "")
    {
        return new InstallFlowRequest
        {
            SelectedGame = selectedGame,
            SelectedIndex = selectedIndex,
            LatestRuntimeContext = latestRuntimeContext,
            LatestArchiveReadiness = latestArchiveReadiness,
            SelectionState = selectionState,
            MatchByGameId = matchByGameId,
            TargetPathByGameId = targetPathByGameId,
            ModuleDownloadLinks = moduleDownloadLinks,
            CommonOptiScalerIniSettings = commonOptiScalerIniSettings
                                           ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
            IsWindowsSupported = isWindowsSupported,
            IsInstallExecutionInProgress = isInstallExecutionInProgress,
            IsAppUpdateInProgress = isAppUpdateInProgress,
            Strings = strings,
            LatestRemoteCatalogErrorCode = latestRemoteCatalogErrorCode
        };
    }

    public AppUpdateFlowRequest BuildAppUpdateRequest(
        RemoteRuntimeData latestRuntimeData,
        string currentVersion,
        AppStrings strings)
    {
        return new AppUpdateFlowRequest
        {
            LatestRuntimeData = latestRuntimeData,
            CurrentVersion = currentVersion,
            Strings = strings
        };
    }
}
