using OptiClick.Core.OptiScaler;
using OptiClick.Core.Runtime;
using OptiClick.Core.Install;
using OptiClick.Wpf.Install.Flow;
using OptiClick.Wpf.Install.Planning;
using OptiClick.Wpf.Localization;
using OptiClick.Wpf.Services;
using OptiClick.Wpf.Shell.Games;
using OptiClick.Wpf.Shell.Runtime;
using OptiClick.Wpf.Shell.RuntimeData;
using OptiClick.Wpf.Shell.Scan;
using OptiClick.Wpf.Shell.Selection;

namespace OptiClick.Wpf.ViewModels;

internal sealed class MainRuntimeFlowRequestFactory
{
    public RuntimeCatalogFlowRequest BuildRuntimeCatalogRequest(
        RuntimeContext latestRuntimeContext,
        AppLanguage selectedLanguage,
        RuntimeCatalogFlowText text)
    {
        return new RuntimeCatalogFlowRequest
        {
            LatestRuntimeContext = latestRuntimeContext,
            SelectedLanguage = selectedLanguage,
            Text = text
        };
    }
}

internal sealed class MainScanFlowRequestFactory
{
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
        return new ScanFlowRequest
        {
            ScanFolders = scanFolders,
            LatestRemoteCatalog = latestRemoteCatalog,
            LatestRuntimeContext = latestRuntimeContext,
            Text = text,
            CurrentMatchByGameId = matchByGameId,
            CurrentTargetPathByGameId = targetPathByGameId,
            ModuleDownloadLinks = moduleDownloadLinks,
            LatestRemoteCatalogErrorCode = latestRemoteCatalogErrorCode
        };
    }
}

internal sealed class MainGameSelectionFlowRequestFactory
{
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
            LatestOptiScalerVariantCatalog = latestOptiScalerVariantCatalog ?? OptiScalerVariantCatalog.Empty,
            PreferredOptiScalerVariant = (preferredOptiScalerVariant ?? "").Trim(),
            SelectedLanguage = selectedLanguage,
            IsInstallExecutionInProgress = isInstallExecutionInProgress,
            IsAppUpdateInProgress = isAppUpdateInProgress,
            MultiGpuBlocked = multiGpuBlocked,
            GpuSelectionPending = gpuSelectionPending,
            LatestRemoteCatalogErrorCode = latestRemoteCatalogErrorCode
        };
    }
}

internal sealed class MainInstallFlowRequestFactory
{
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
        var resolvedGameInputs = ResolveGameInputs(selectedGame, selectionState.ResolvedInputs);
        var descriptorContext = resolvedGameInputs.OptiScalerIniApplyContext ?? new OptiScalerIniApplyContext();
        var resolvedContext = (optiScalerIniApplyContext ?? descriptorContext) with
        {
            GameOptiScalerIniSettings = descriptorContext.GameOptiScalerIniSettings,
            GpuBundleKey = resolvedGameInputs.ExecutionDescriptor.GpuBundleKey
        };
        return new InstallFlowRequest
        {
            ExecutionContext = new InstallExecutionContext
            {
                ExecutionDescriptor = resolvedGameInputs.ExecutionDescriptor,
                ProfileRows = resolvedGameInputs.ProfileRows,
                OptiScalerIniApplyContext = resolvedContext,
                IsEnabled = resolvedGameInputs.IsEnabled,
                LatestRuntimeContext = latestRuntimeContext,
                LatestArchiveReadiness = latestArchiveReadiness,
                SelectionSnapshot = InstallFlowSelectionSnapshotMapper.FromSelectionState(selectionState),
                ModuleDownloadLinks = moduleDownloadLinks,
                IsWindowsSupported = isWindowsSupported,
                IsInstallExecutionInProgress = isInstallExecutionInProgress,
                IsAppUpdateInProgress = isAppUpdateInProgress,
                LatestRemoteCatalogErrorCode = latestRemoteCatalogErrorCode
            },
            Text = text,
            InstallPostPopupMessage = selectionState.InstallPostPopupMessage
        };
    }

    private static ResolvedInstallGameInputs ResolveGameInputs(
        ShellGameCardModel selectedGame,
        ResolvedInstallGameInputs? resolvedInputs)
    {
        return ShellInstallDescriptorInputFactory.ResolveInputs(selectedGame, resolvedInputs);
    }
}

internal sealed class MainUninstallFlowRequestFactory
{
    public UninstallFlowCoordinatorRequest BuildUninstallRequest(
        ShellGameCardModel selectedGame,
        string targetPath,
        UninstallFlowSelectionSnapshot selectionSnapshot,
        ResolvedInstallGameInputs resolvedGameInputs,
        UninstallFlowCoordinatorUiActions uiActions,
        UninstallFlowText text)
    {
        resolvedGameInputs = ResolveGameInputs(selectedGame, resolvedGameInputs);
        return new UninstallFlowCoordinatorRequest
        {
            ExecutionDescriptor = resolvedGameInputs.ExecutionDescriptor,
            EngineIniProfileRows = resolvedGameInputs.EngineIniProfileRows,
            SelectedGameId = resolvedGameInputs.GameId,
            TargetPath = targetPath,
            Text = text,
            SelectionSnapshot = selectionSnapshot,
            UiActions = uiActions
        };
    }

    private static ResolvedInstallGameInputs ResolveGameInputs(
        ShellGameCardModel selectedGame,
        ResolvedInstallGameInputs? resolvedInputs)
    {
        return ShellInstallDescriptorInputFactory.ResolveInputs(selectedGame, resolvedInputs);
    }
}

internal sealed class MainAppUpdateFlowRequestFactory
{
    public AppUpdateFlowRequest BuildAppUpdateRequest(AppUpdateFlowText text, AppLanguage language)
    {
        return new AppUpdateFlowRequest
        {
            Text = text,
            Language = language
        };
    }
}
