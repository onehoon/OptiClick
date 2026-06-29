using System;
using System.Collections.Generic;
using OptiClick.Wpf.Install.UiState;
using OptiClick.Wpf.Models;
using OptiClick.Wpf.Shell.Actions;
using OptiClick.Wpf.Shell.Flow;
using OptiClick.Wpf.Shell.Games;
using OptiClick.Wpf.Shell.Runtime;
using OptiClick.Wpf.Shell.RuntimeData;
using OptiClick.Wpf.Shell.Scan;

namespace OptiClick.Wpf.ViewModels;

public sealed class MainViewModelStateApplier
{
    private readonly Action<ScanFolderStateUpdate> _applyScanFolderStateUpdate;
    private readonly Action<string?> _setSettingsStatusText;
    private readonly Action<string?> _setScanStatusText;
    private readonly Action _queuePendingStartupNotice;
    private readonly Action<string, string> _setRemoteCatalogError;
    private readonly Action<RemoteRuntimeData?, ShellGameCatalog?, ModuleDownloadLinkContext?, OptiScalerVariantCatalog?, string?> _applyRuntimeData;
    private readonly Action<IReadOnlyDictionary<string, ShellGameMatchResult>> _replaceMatchByGameId;
    private readonly Action<IReadOnlyDictionary<string, string>> _replaceTargetPathByGameId;
    private readonly Action<IReadOnlyList<GameCardViewModel>> _replaceVisibleGames;
    private readonly Action<bool, bool, string?, string?> _writeAppLog;
    private readonly Action<PopupPresentationRequest> _showDeferredPopup;
    private readonly Action<AppDialogRequest> _showDeferredDialog;
    private readonly Action<IReadOnlyList<IFlowLogEntry>, string> _dispatchFlowLogs;

    public MainViewModelStateApplier(
        Action<ScanFolderStateUpdate> applyScanFolderStateUpdate,
        Action<string?> setSettingsStatusText,
        Action<string?> setScanStatusText,
        Action queuePendingStartupNotice,
        Action<string, string> setRemoteCatalogError,
        Action<RemoteRuntimeData?, ShellGameCatalog?, ModuleDownloadLinkContext?, OptiScalerVariantCatalog?, string?> applyRuntimeData,
        Action<IReadOnlyDictionary<string, ShellGameMatchResult>> replaceMatchByGameId,
        Action<IReadOnlyDictionary<string, string>> replaceTargetPathByGameId,
        Action<IReadOnlyList<GameCardViewModel>> replaceVisibleGames,
        Action<bool, bool, string?, string?> writeAppLog,
        Action<PopupPresentationRequest> showDeferredPopup,
        Action<AppDialogRequest> showDeferredDialog,
        Action<IReadOnlyList<IFlowLogEntry>, string> dispatchFlowLogs)
    {
        _applyScanFolderStateUpdate = applyScanFolderStateUpdate;
        _setSettingsStatusText = setSettingsStatusText;
        _setScanStatusText = setScanStatusText;
        _queuePendingStartupNotice = queuePendingStartupNotice;
        _setRemoteCatalogError = setRemoteCatalogError;
        _applyRuntimeData = applyRuntimeData;
        _replaceMatchByGameId = replaceMatchByGameId;
        _replaceTargetPathByGameId = replaceTargetPathByGameId;
        _replaceVisibleGames = replaceVisibleGames;
        _writeAppLog = writeAppLog;
        _showDeferredPopup = showDeferredPopup;
        _showDeferredDialog = showDeferredDialog;
        _dispatchFlowLogs = dispatchFlowLogs;
    }

    public void ApplyStateUpdate(MainViewModelStateUpdate update, string defaultFlowLogCategory = MainViewModelLogCategories.App)
    {
        ArgumentNullException.ThrowIfNull(update);
        DispatchStateUpdateFlowLogs(update, defaultFlowLogCategory);

        if (update.ScanFolderStateUpdate is { } scanFolderStateUpdate)
        {
            _applyScanFolderStateUpdate(scanFolderStateUpdate);
        }

        if (!string.IsNullOrWhiteSpace(update.SettingsStatusText))
        {
            _setSettingsStatusText(update.SettingsStatusText);
        }

        if (!string.IsNullOrWhiteSpace(update.ScanStatusText))
        {
            _setScanStatusText(update.ScanStatusText);
        }

        if (update.ShouldQueuePendingStartupNotice)
        {
            _queuePendingStartupNotice();
        }

        if (update.RemoteCatalogErrorCode is not null)
        {
            _setRemoteCatalogError(update.RemoteCatalogErrorCode, update.RemoteCatalogDetailErrorCode ?? "");
        }

        if (update.RuntimeData is not null
            || update.RemoteCatalog is not null
            || update.ModuleDownloadLinks is not null
            || update.OptiScalerVariantCatalog is not null
            || update.GpuBundleKey is not null)
        {
            _applyRuntimeData(
                update.RuntimeData,
                update.RemoteCatalog,
                update.ModuleDownloadLinks,
                update.OptiScalerVariantCatalog,
                update.GpuBundleKey);
        }

        if (update.MatchByGameId is { } matchByGameId)
        {
            _replaceMatchByGameId(matchByGameId);
        }

        if (update.TargetPathByGameId is { } targetPathByGameId)
        {
            _replaceTargetPathByGameId(targetPathByGameId);
        }

        if (update.VisibleGames is not null)
        {
            _replaceVisibleGames(update.VisibleGames);
        }

        if (string.IsNullOrWhiteSpace(update.SupportLogMessage))
        {
            return;
        }

        _writeAppLog(true, update.SupportLogAsWarning, update.SupportLogCategory, update.SupportLogMessage);
    }

    public void ApplyDeferredStateUpdate(MainViewModelStateUpdate update, string defaultFlowLogCategory = MainViewModelLogCategories.App)
    {
        ApplyStateUpdate(update, defaultFlowLogCategory);

        if (update.PopupRequest is { } popupRequest)
        {
            _showDeferredPopup(popupRequest);
        }

        if (update.DialogRequest is { } dialogRequest)
        {
            _showDeferredDialog(dialogRequest);
        }
    }

    public void DispatchStateUpdateFlowLogs(
        MainViewModelStateUpdate update,
        string defaultCategory)
    {
        if (update.FlowLogs.Count == 0)
        {
            return;
        }

        var flowLogCategory = string.IsNullOrWhiteSpace(update.FlowLogFallbackCategory)
            ? defaultCategory
            : update.FlowLogFallbackCategory;
        _dispatchFlowLogs(update.FlowLogs, flowLogCategory);
    }
}
