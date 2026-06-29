using OptiClick.Wpf.Install.Flow;
using OptiClick.Wpf.Services;
using OptiClick.Wpf.Shell.Actions;
using OptiClick.Wpf.Shell.Runtime;
using OptiClick.Wpf.Shell.Scan;
using OptiClick.Wpf.Shell.Support;

namespace OptiClick.Wpf.ViewModels;

public sealed class MainViewModelResultApplier
{
    public MainViewModelStateUpdate CreateShellCommandStateUpdate(ShellCommandActionResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return new MainViewModelStateUpdate
        {
            SettingsStatusText = string.IsNullOrWhiteSpace(result.SettingsStatusText)
                ? null
                : result.SettingsStatusText,
            DialogRequest = result.DialogRequest,
            ShouldQueuePendingStartupNotice = result.ShouldQueuePendingStartupNotice
        };
    }

    public MainViewModelStateUpdate CreateSupportActionStateUpdate(SupportActionResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return new MainViewModelStateUpdate
        {
            SettingsStatusText = result.StatusText,
            DialogRequest = result.DialogRequest,
            SupportLogAsWarning = !result.IsSuccess,
            SupportLogCategory = result.LogCategory ?? "",
            SupportLogMessage = result.LogMessage ?? ""
        };
    }

    public MainViewModelStateUpdate CreateAppUpdateStateUpdate(AppUpdateExecutionFlowResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return new MainViewModelStateUpdate
        {
            SettingsStatusText = result.StatusText,
            DialogRequest = result.DialogRequest,
            ShouldShutdown = result.ShouldShutdown
        };
    }

    public MainViewModelStateUpdate CreateInstallStateUpdate(InstallFlowResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return new MainViewModelStateUpdate
        {
            SettingsStatusText = result.StatusText,
            PopupRequest = result.PopupRequest
        };
    }

    public MainViewModelStateUpdate CreateRuntimeCatalogStateUpdate(
        RuntimeCatalogFlowResult result,
        string normalizedErrorCode)
    {
        ArgumentNullException.ThrowIfNull(result);

        return new MainViewModelStateUpdate
        {
            SettingsStatusText = string.IsNullOrWhiteSpace(result.SettingsStatusText)
                ? null
                : result.SettingsStatusText,
            ScanStatusText = string.IsNullOrWhiteSpace(result.ScanStatusText)
                ? null
                : result.ScanStatusText,
            RemoteCatalogErrorCode = result.IsSuccess
                ? ""
                : normalizedErrorCode,
            RemoteCatalogDetailErrorCode = "",
            RuntimeData = result.ShouldApplyRemoteDataState ? result.RuntimeData : null,
            RemoteCatalog = result.ShouldApplyRemoteDataState ? result.Catalog : null,
            ModuleDownloadLinks = result.ShouldApplyRemoteDataState ? result.ModuleDownloadLinks : null,
            OptiScalerVariantCatalog = result.ShouldApplyRemoteDataState ? result.OptiScalerVariantCatalog : null,
            GpuBundleKey = result.ShouldApplyRemoteDataState ? result.GpuBundleKey : null,
            ShouldResetRemoteCatalogDialogGate = result.ResetRemoteCatalogDialogGate,
            ShouldRefreshVisibleGames = result.ShouldRefreshVisibleGames,
            ShouldRefreshArchiveReadiness = result.ShouldRefreshArchiveReadiness,
            DialogRequest = result.DialogRequest
        };
    }

    public MainViewModelStateUpdate CreateScanStateUpdate(OptiClick.Wpf.Shell.Scan.ScanFlowResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return new MainViewModelStateUpdate
        {
            ScanStatusText = string.IsNullOrWhiteSpace(result.StatusText)
                ? null
                : result.StatusText,
            MatchByGameId = result.MatchByGameId,
            TargetPathByGameId = result.TargetPathByGameId,
            VisibleGames = result.VisibleGames.Count > 0 || result.DidRun || result.ShouldRecomputeSelection
                ? result.VisibleGames
                : null,
            ShouldRecomputeSelection = result.ShouldRecomputeSelection,
            ShouldNavigateHome = result.ShouldNavigateHome,
            DialogRequest = result.DialogRequest
        };
    }

    public MainViewModelStateUpdate CreateScanFolderActionStateUpdate(ScanFolderActionResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return new MainViewModelStateUpdate
        {
            ScanStatusText = string.IsNullOrWhiteSpace(result.StatusText)
                ? null
                : result.StatusText,
            FlowLogs = result.Logs,
            FlowLogFallbackCategory = MainViewModelLogCategories.Scan,
            DialogRequest = result.DialogRequest,
            ScanFolderStateUpdate = result.StateUpdate
        };
    }
}
