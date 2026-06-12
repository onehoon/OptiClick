using OptiClick.Core.Models;
using OptiClick.Core.Runtime;
using OptiClick.Wpf.Localization;
using OptiClick.Wpf.Logging;
using OptiClick.Wpf.Models;
using OptiClick.Wpf.Services;
using OptiClick.Wpf.Shell.Games;
using OptiClick.Wpf.Shell.Localization;
using OptiClick.Wpf.Shell.Navigation;
using OptiClick.Wpf.Shell.Selection;
using OptiClick.Wpf.ViewModels.Features.Shell.ShellCommand;
using OptiClick.Wpf.ViewModels.Features.Shell.StartupAnnouncement;
using OptiClick.Wpf.ViewModels.Features.Shell.UserSettings;

namespace OptiClick.Wpf.ViewModels.Features.Shell;

internal sealed class MainShellInteractionFeatureFacade
{
    private readonly IAppLogger _appLogger;
    private readonly IAppVersionProvider _appVersionProvider;
    private readonly LocalizationStateController _localizationStateController;
    private readonly MainViewModelBusyStateApplier _busyStateApplier;
    private readonly OptiScalerDirtyNavigationGuard _optiScalerDirtyNavigationGuard;
    private readonly MainAppUpdateInteractionController _appUpdateInteractionController;
    private readonly GameDetailsDialogPresenter _gameDetailsDialogPresenter;
    private readonly MainShellInteractionContextFactory _contextFactory;
    private readonly MainShellCommandInteractionFeature _shellCommand;
    private readonly MainUserSettingsInteractionFeature _userSettings;
    private readonly MainStartupAnnouncementInteractionFeature _startupAnnouncement;

    public MainShellInteractionFeatureFacade(
        IAppLogger appLogger,
        IAppVersionProvider appVersionProvider,
        LocalizationStateController localizationStateController,
        MainViewModelBusyStateApplier busyStateApplier,
        OptiScalerDirtyNavigationGuard optiScalerDirtyNavigationGuard,
        MainAppUpdateInteractionController appUpdateInteractionController,
        GameDetailsDialogPresenter gameDetailsDialogPresenter,
        MainShellInteractionContextFactory contextFactory,
        MainShellCommandInteractionFeature shellCommand,
        MainUserSettingsInteractionFeature userSettings,
        MainStartupAnnouncementInteractionFeature startupAnnouncement)
    {
        _appLogger = appLogger ?? throw new ArgumentNullException(nameof(appLogger));
        _appVersionProvider = appVersionProvider ?? throw new ArgumentNullException(nameof(appVersionProvider));
        _localizationStateController =
            localizationStateController ?? throw new ArgumentNullException(nameof(localizationStateController));
        _busyStateApplier = busyStateApplier ?? throw new ArgumentNullException(nameof(busyStateApplier));
        _optiScalerDirtyNavigationGuard =
            optiScalerDirtyNavigationGuard ?? throw new ArgumentNullException(nameof(optiScalerDirtyNavigationGuard));
        _appUpdateInteractionController =
            appUpdateInteractionController ?? throw new ArgumentNullException(nameof(appUpdateInteractionController));
        _gameDetailsDialogPresenter =
            gameDetailsDialogPresenter ?? throw new ArgumentNullException(nameof(gameDetailsDialogPresenter));
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        _shellCommand = shellCommand ?? throw new ArgumentNullException(nameof(shellCommand));
        _userSettings = userSettings ?? throw new ArgumentNullException(nameof(userSettings));
        _startupAnnouncement = startupAnnouncement ?? throw new ArgumentNullException(nameof(startupAnnouncement));
    }

    public string GetCurrentAppVersion()
    {
        var value = _appVersionProvider.GetCurrentVersion();
        var normalized = (value ?? "").Trim();
        return string.IsNullOrWhiteSpace(normalized) ? "0.0.0" : normalized;
    }

    public void LogInfo(string category, string message)
    {
        _appLogger.Info(category, message);
    }

    public void LogWarning(string category, string message)
    {
        _appLogger.Warning(category, message);
    }

    public void LogError(string category, string message, Exception? exception = null)
    {
        if (exception is null)
        {
            _appLogger.Error(category, message);
            return;
        }

        _appLogger.Error(category, message, exception);
    }

    public void ApplyAppLog(
        bool shouldWrite,
        bool asWarning,
        string? category,
        string? message)
    {
        if (!shouldWrite || string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        var normalizedCategory = NormalizeStatusCode(category, MainViewModelLogCategories.App);
        var normalizedMessage = message.Trim();
        if (asWarning)
        {
            LogWarning(normalizedCategory, normalizedMessage);
            return;
        }

        LogInfo(normalizedCategory, normalizedMessage);
    }

    public void OpenSupportRequest()
    {
        _shellCommand.OpenSupportRequest();
    }

    public void OpenGameSupportRequest()
    {
        _shellCommand.OpenGameSupportRequest();
    }

    public void NotifyAdministratorRelaunchCancelled()
    {
        _shellCommand.NotifyAdministratorRelaunchCancelled();
    }

    public void QueuePendingStartupNotice()
    {
        _shellCommand.QueuePendingStartupNotice();
    }

    public Task ShowPendingStartupNoticesAsync(CancellationToken cancellationToken = default)
    {
        return _shellCommand.ShowPendingStartupNoticesAsync(cancellationToken);
    }

    public Task ShowStartupAnnouncementIfNeededAsync(CancellationToken cancellationToken = default)
    {
        return _startupAnnouncement.ShowStartupAnnouncementIfNeededAsync(cancellationToken);
    }

    public void OpenLogFolder()
    {
        _shellCommand.OpenLogFolder();
    }

    public AppUserSettings LoadUserSettings()
    {
        return _userSettings.LoadUserSettings();
    }

    public void SavePreferencesNonBlocking(string languagePreference, string optiScalerVariantPreference)
    {
        _userSettings.SavePreferencesNonBlocking(languagePreference, optiScalerVariantPreference);
    }

    public void DisposeUserSettings()
    {
        _userSettings.DisposeUserSettings();
    }

    public void ApplyUserSettings(AppUserSettings settings)
    {
        _userSettings.ApplyUserSettings(settings);
    }

    public LocalizationStateUpdate BuildInitialLocalizationState(
        AppLanguage language,
        AppStrings strings,
        string settingsStatusText,
        string scanStatusText,
        string deviceText,
        string gpuText)
    {
        return _localizationStateController.BuildInitialState(
            language,
            strings,
            settingsStatusText,
            scanStatusText,
            deviceText,
            gpuText);
    }

    public LocalizationStateUpdate BuildRefreshLocalizationState(AppLanguage language, AppStrings strings)
    {
        return _localizationStateController.BuildRefreshState(language, strings);
    }

    public MainViewModelBusyStateUpdate CreateInstallBusyState(
        bool inProgress,
        bool isAppUpdateInProgress,
        ShellInstallSelectionState currentSelectionState,
        ShellInstallSelectionState? restoreSelectionState,
        string operationOverlayMessage)
    {
        return _busyStateApplier.CreateInstallBusyState(
            inProgress,
            isAppUpdateInProgress,
            currentSelectionState,
            restoreSelectionState,
            operationOverlayMessage);
    }

    public Task<bool> ConfirmOptiScalerDirtyNavigationAsync(
        OptiScalerDirtyNavigationGuardRequest request,
        CancellationToken cancellationToken)
    {
        return _optiScalerDirtyNavigationGuard.ConfirmAsync(request, cancellationToken);
    }

    public Task<bool> ConfirmOptiScalerDirtyNavigationAsync(
        ShellViewKind currentView,
        ShellViewKind targetView,
        bool hasUnsavedChanges,
        OptiScalerDirtyNavigationGuardText text,
        Action saveChanges,
        Action discardChanges,
        CancellationToken cancellationToken)
    {
        var context = _contextFactory.CreateShellCommandContext();
        return ConfirmOptiScalerDirtyNavigationAsync(
            new OptiScalerDirtyNavigationGuardRequest
            {
                CurrentView = currentView,
                TargetView = targetView,
                HasUnsavedChanges = hasUnsavedChanges,
                Text = text,
                ShowDialogAsync = context.ShowDialogAsync,
                SaveChanges = saveChanges,
                DiscardChanges = discardChanges
            },
            cancellationToken);
    }

    public Task ShowStartupUpdateCheckDialogAsync(CancellationToken cancellationToken)
    {
        return _appUpdateInteractionController.ShowStartupUpdateCheckDialogAsync(
            _contextFactory.CreateAppUpdateInteractionContext(),
            cancellationToken);
    }

    public void ShowDetailsDialog()
    {
        var context = _contextFactory.CreateDetailsDialogContext();
        if (context.SelectedGame is null)
        {
            return;
        }

        context.ShowDeferredDialog(
            _gameDetailsDialogPresenter.BuildDetailsDialog(
                context.SelectedGame,
                context.Strings));
    }

    private static string NormalizeStatusCode(string? value, string fallback)
    {
        var normalized = (value ?? "").Trim();
        return string.IsNullOrWhiteSpace(normalized) ? fallback : normalized;
    }
}
