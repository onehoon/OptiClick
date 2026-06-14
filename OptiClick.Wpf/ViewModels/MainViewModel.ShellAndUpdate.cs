using System;
using System.Threading;
using System.Threading.Tasks;
using OptiClick.Core.OptiScaler;
using OptiClick.Core.Runtime;
using OptiClick.Core.Models;
using OptiClick.Wpf.Models;
using OptiClick.Wpf.Services;

namespace OptiClick.Wpf.ViewModels;

public sealed partial class MainViewModel : ViewModelBase
{
    private void OpenSupportRequest()
    {
        _features.ShellInteraction.OpenSupportRequest();
    }

    private void OpenGameSupportRequest()
    {
        _features.ShellInteraction.OpenGameSupportRequest();
    }

    public void NotifyAdministratorRelaunchCancelled()
    {
        _features.ShellInteraction.NotifyAdministratorRelaunchCancelled();
    }

    private Task ShowPendingStartupNoticesAsync(CancellationToken cancellationToken = default)
    {
        return _features.ShellInteraction.ShowPendingStartupNoticesAsync(cancellationToken);
    }

    private Task ShowStartupAnnouncementIfNeededAsync(CancellationToken cancellationToken = default)
    {
        return _features.ShellInteraction.ShowStartupAnnouncementIfNeededAsync(cancellationToken);
    }

    private void ApplyUserSettings(AppUserSettings settings)
    {
        _features.ShellInteraction.ApplyUserSettings(settings);
    }

    private AppLanguage ResolvePreferredLanguage(string languagePreference)
    {
        return languagePreference switch
        {
            LanguagePreferenceKorean => AppLanguage.Korean,
            LanguagePreferenceEnglish => AppLanguage.English,
            _ => ResolveAutoLanguage()
        };
    }

    private void ApplyLoadedLanguage(AppLanguage preferredLanguage)
    {
        _selectedLanguage = preferredLanguage;
        _languageProvider.SetLanguage(preferredLanguage);
        OnPropertyChanged(nameof(SelectedLanguage));
        RefreshLocalizedStrings();
        SelectedGameAction.ApplyLocalization(Strings);
        SupportedGames.RefreshAfterLanguageChange();
        ApplyLocalizationStateUpdate(_features.ShellInteraction.BuildRefreshLocalizationState(preferredLanguage, Strings));
    }

    private void ApplyLoadedSettingsOption(string languageOption)
    {
        Settings.ApplyLoadedSettings(languageOption);
    }

    private void ApplySavedOptiScalerSettings(
        string optiScalerVariantPreference,
        OptiScalerCommonIniSettingsDocument? commonIniSettings)
    {
        OptiScaler.ApplySavedSettings(
            optiScalerVariantPreference,
            commonIniSettings);
    }

    private void SaveUserSettings() => _features.ShellInteraction.SavePreferencesNonBlocking(
        _languagePreference,
        _optiScalerVariantPreference);

    public void Dispose()
    {
        CancelBackgroundWork();
        _features.ShellInteraction.DisposeUserSettings();
    }

    private void OpenLogFolder()
    {
        _features.ShellInteraction.OpenLogFolder();
    }

    private Task ShowStartupUpdateCheckDialogAsync(CancellationToken cancellationToken = default)
    {
        return _features.ShellInteraction.ShowStartupUpdateCheckDialogAsync(cancellationToken);
    }

    private void ApplyBusyStateUpdate(MainViewModelBusyStateUpdate update)
    {
        ArgumentNullException.ThrowIfNull(update);
        _isAppUpdateInProgress = update.IsAppUpdateInProgress;
        _isInstallExecutionInProgress = update.IsInstallExecutionInProgress;
        ShellBusyState.Apply(update.IsOperationOverlayVisible, update.OperationOverlayMessage);
        if (!string.IsNullOrWhiteSpace(update.SettingsStatusText)) SettingsStatusText = update.SettingsStatusText;
        if (update.ShouldRefreshInstallCommand)
        {
            Home.RefreshCommandStates();
        }
        _selectionState = update.SelectionState;
        if (update.ShouldApplySelectedGameActionRunningState) SelectedGameAction.ApplySelectionBridgeState(_selectionState);
    }

}
