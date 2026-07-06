using OptiClick.Core.Runtime;
using OptiClick.Core.Install;
using OptiClick.Wpf.Install.Flow;
using OptiClick.Wpf.Install.Planning;
using OptiClick.Wpf.Localization;
using OptiClick.Core.OptiScaler;
using OptiClick.Wpf.Shell.Games;
using OptiClick.Wpf.Shell.RuntimeData;
using OptiClick.Wpf.Services;
using OptiClick.Wpf.Shell.Runtime;
using OptiClick.Wpf.Shell.Scan;
using OptiClick.Wpf.Shell.Selection;

namespace OptiClick.Wpf.ViewModels;

public sealed class MainViewModelFlowRequestFactory
{
    private readonly MainRuntimeFlowRequestFactory _runtime = new();
    private readonly MainScanFlowRequestFactory _scan = new();
    private readonly MainGameSelectionFlowRequestFactory _selection = new();
    private readonly MainInstallFlowRequestFactory _install = new();
    private readonly MainUninstallFlowRequestFactory _uninstall = new();
    private readonly MainAppUpdateFlowRequestFactory _appUpdate = new();

    public RuntimeCatalogFlowRequest BuildRuntimeCatalogRequest(
        RuntimeContext latestRuntimeContext,
        AppLanguage selectedLanguage,
        RuntimeCatalogFlowText text)
    {
        return _runtime.BuildRuntimeCatalogRequest(
            latestRuntimeContext,
            selectedLanguage,
            text);
    }

    public ScanFlowRequest BuildScanRequest(
        IReadOnlyList<string> scanFolders,
        ShellGameCatalog latestRemoteCatalog,
        RuntimeContext latestRuntimeContext,
        ScanFlowText text,
        IReadOnlyDictionary<string, ShellGameMatchResult> matchByGameId,
        IReadOnlyDictionary<string, string> targetPathByGameId,
        ModuleDownloadLinkContext moduleDownloadLinks,
        string latestRemoteCatalogErrorCode)
    {
        return _scan.BuildScanRequest(
            scanFolders,
            latestRemoteCatalog,
            latestRuntimeContext,
            text,
            matchByGameId,
            targetPathByGameId,
            moduleDownloadLinks,
            latestRemoteCatalogErrorCode);
    }

    public GameSelectionFlowRequest BuildGameSelectionRequest(
        ShellGameCardModel selectedCard,
        int selectedIndex,
        IReadOnlyList<ShellGameCardModel> games,
        ShellInstallSelectionState previousSelectionState,
        IReadOnlyDictionary<string, ShellGameMatchResult> matchByGameId,
        IReadOnlyDictionary<string, string> targetPathByGameId,
        ModuleDownloadLinkContext moduleDownloadLinks,
        ArchiveReadinessSnapshot latestArchiveReadiness,
        AppLanguage selectedLanguage,
        bool isInstallExecutionInProgress,
        bool isAppUpdateInProgress,
        bool multiGpuBlocked,
        bool gpuSelectionPending,
        string latestRemoteCatalogErrorCode = "",
        OptiScalerVariantCatalog? latestOptiScalerVariantCatalog = null,
        string preferredOptiScalerVariant = "")
    {
        return _selection.BuildGameSelectionRequest(
            selectedCard,
            selectedIndex,
            games,
            previousSelectionState,
            matchByGameId,
            targetPathByGameId,
            moduleDownloadLinks,
            latestArchiveReadiness,
            selectedLanguage,
            isInstallExecutionInProgress,
            isAppUpdateInProgress,
            multiGpuBlocked,
            gpuSelectionPending,
            latestRemoteCatalogErrorCode,
            latestOptiScalerVariantCatalog,
            preferredOptiScalerVariant);
    }

    public InstallFlowRequest BuildInstallRequest(
        ShellGameCardModel selectedGame,
        RuntimeContext latestRuntimeContext,
        ArchiveReadinessSnapshot latestArchiveReadiness,
        ShellInstallSelectionState selectionState,
        ModuleDownloadLinkContext moduleDownloadLinks,
        OptiScalerIniApplyContext optiScalerIniApplyContext,
        bool isWindowsSupported,
        bool isInstallExecutionInProgress,
        bool isAppUpdateInProgress,
        InstallFlowText text,
        string latestRemoteCatalogErrorCode = "")
    {
        return _install.BuildInstallRequest(
            selectedGame,
            latestRuntimeContext,
            latestArchiveReadiness,
            selectionState,
            moduleDownloadLinks,
            optiScalerIniApplyContext,
            isWindowsSupported,
            isInstallExecutionInProgress,
            isAppUpdateInProgress,
            text,
            latestRemoteCatalogErrorCode);
    }

    public UninstallFlowCoordinatorRequest BuildUninstallRequest(
        ShellGameCardModel selectedGame,
        string targetPath,
        UninstallFlowSelectionSnapshot selectionSnapshot,
        ResolvedInstallGameInputs resolvedGameInputs,
        UninstallFlowCoordinatorUiActions uiActions,
        UninstallFlowText text)
    {
        return _uninstall.BuildUninstallRequest(
            selectedGame,
            targetPath,
            selectionSnapshot,
            resolvedGameInputs,
            uiActions,
            text);
    }

    public AppUpdateFlowRequest BuildAppUpdateRequest(AppUpdateFlowText text, AppLanguage language)
    {
        return _appUpdate.BuildAppUpdateRequest(text, language);
    }
}
