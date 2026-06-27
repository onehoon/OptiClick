using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OptiClick.Core.OptiScaler;
using OptiClick.Wpf.Install.Archives;
using OptiClick.Core.Install;
using OptiClick.Wpf.Install.Flow;
using OptiClick.Wpf.Install.Planning;
using OptiClick.Wpf.Install.UiState;
using OptiClick.Wpf.Models;
using OptiClick.Wpf.Shell.Runtime;
using OptiClick.Wpf.Shell.RuntimeData;
using OptiClick.Wpf.Shell.Games;
using OptiClick.Wpf.Shell.Localization;
using OptiClick.Wpf.Shell.Scan;
using OptiClick.Wpf.Shell.Selection;

namespace OptiClick.Wpf.ViewModels;

public sealed partial class MainViewModel : ViewModelBase
{
    private void ApplyStateUpdate(MainViewModelStateUpdate update)
    {
        _mainStateApplier.ApplyStateUpdate(update, MainViewModelLogCategories.App);
    }

    private void ApplyDeferredStateUpdate(MainViewModelStateUpdate update)
    {
        _mainStateApplier.ApplyDeferredStateUpdate(update, MainViewModelLogCategories.App);
    }

    private void DispatchStateUpdateFlowLogs(
        MainViewModelStateUpdate update,
        string defaultCategory = MainViewModelLogCategories.App)
    {
        _mainStateApplier.DispatchStateUpdateFlowLogs(update, defaultCategory);
    }

    private void ApplyAppLog(
        bool shouldWrite,
        bool asWarning,
        string? category,
        string? message)
    {
        _features.ShellInteraction.ApplyAppLog(shouldWrite, asWarning, category, message);
    }

    private void ApplyInstallBusyState(
        bool inProgress,
        ShellInstallSelectionState? restoreSelectionState = null,
        string operationOverlayMessage = "")
    {
        ApplyBusyStateUpdate(
            _features.ShellInteraction.CreateInstallBusyState(
                inProgress,
                _isAppUpdateInProgress,
                _selectionState,
                restoreSelectionState,
                operationOverlayMessage));
    }

    private void RefreshLocalizedStrings()
    {
        Strings = _appStringsProvider.Get(SelectedLanguage);
        Scan.RefreshLocalization();
        Settings.RefreshLocalization();
        OptiScaler.RefreshLocalization();
        Home.RefreshLocalization();
        OnPropertyChanged(nameof(WindowTitleWithVersion));
    }

    private void ApplyLocalizationStateUpdate(LocalizationStateUpdate update)
    {
        ArgumentNullException.ThrowIfNull(update);
        if (!string.IsNullOrWhiteSpace(update.ScanStatusText)) ScanStatusText = update.ScanStatusText;
        if (!string.IsNullOrWhiteSpace(update.SettingsStatusText)) SettingsStatusText = update.SettingsStatusText;
        RuntimeHeader.ApplyTextUpdate(update.DeviceText, update.GpuText);
        if (update.ShouldRelocalizeScanFolders) RelocalizeScanFolderRows();
        if (update.ShouldRefreshRuntimeSummary) ApplyRuntimeSummaryStateUpdate(_features.Runtime.BuildLatestRuntimeSummaryStateUpdate(RuntimeSummaryStateText.FromAppStrings(Strings)));
    }

    private void LogInfo(string category, string message) => _features.ShellInteraction.LogInfo(category, message);

    private void LogWarning(string category, string message) => _features.ShellInteraction.LogWarning(category, message);

    private void LogError(string category, string message, Exception? exception = null)
    {
        _features.ShellInteraction.LogError(category, message, exception);
    }

    private static string NormalizeStatusCode(string? value, string fallback)
    {
        var normalized = (value ?? "").Trim();
        return string.IsNullOrWhiteSpace(normalized) ? fallback : normalized;
    }

    private bool IsOperatingSystemPolicySupported()
    {
        return _features.Runtime.IsOperatingSystemPolicySupported();
    }

    private bool IsUnsupportedOperatingSystem()
    {
        return _features.Runtime.IsUnsupportedOperatingSystem();
    }

    private string GetCurrentAppVersion()
    {
        return _features.ShellInteraction.GetCurrentAppVersion();
    }

}
