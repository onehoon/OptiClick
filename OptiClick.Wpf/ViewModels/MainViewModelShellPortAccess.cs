using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using OptiClick.Core.OptiScaler;
using OptiClick.Core.Runtime;
using OptiClick.Wpf.Localization;
using OptiClick.Wpf.Shell.Localization;
using OptiClick.Wpf.Shell.Navigation;
using OptiClick.Wpf.Shell.Runtime;
using OptiClick.Wpf.Shell.RuntimeData;
using OptiClick.Wpf.Shell.Scan;
using OptiClick.Wpf.Shell.Selection;
using OptiClick.Wpf.Shell.Startup;
using OptiClick.Wpf.ViewModels.Ports.App;
using OptiClick.Wpf.ViewModels.Ports.Install;
using OptiClick.Wpf.ViewModels.Ports.Localization;
using OptiClick.Wpf.ViewModels.Ports.Runtime;
using OptiClick.Wpf.ViewModels.Ports.Selection;
using OptiClick.Wpf.ViewModels.Ports.ShellInteractionContext.AppUpdate;
using OptiClick.Wpf.ViewModels.Ports.ShellInteractionContext.Language;
using OptiClick.Wpf.ViewModels.Ports.ShellInteractionContext.OptiScaler;
using OptiClick.Wpf.ViewModels.Ports.ShellInteractionContext.ShellCommand;
using OptiClick.Wpf.ViewModels.Ports.ShellInteractionContext.StartupAnnouncement;
using OptiClick.Wpf.ViewModels.Ports.ShellInteractionContext.UserSettings;
using OptiClick.Wpf.ViewModels.Ports.Startup;
using OptiClick.Wpf.ViewModels.Ports.Ui;
using OptiClick.Wpf.ViewModels.Shell;

namespace OptiClick.Wpf.ViewModels;

public sealed partial class MainViewModel
{
    private sealed class ShellPortAccess :
        IMainShellAppPortAccess,
        IMainShellRuntimePortAccess,
        IMainShellStartupPortAccess,
        IMainShellSelectionPortAccess,
        IMainShellInstallPortAccess,
        IMainShellUiPortAccess,
        IMainShellLocalizationPortAccess,
        IMainShellCommandInteractionAccess,
        IMainStartupAnnouncementInteractionAccess,
        IMainAppUpdateInteractionAccess,
        IMainUserSettingsInteractionAccess,
        IMainLanguagePreferenceInteractionAccess,
        IMainOptiScalerSettingsInteractionAccess
    {
        private readonly MainViewModel _owner;

        public ShellPortAccess(MainViewModel owner)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        }

        public AppStrings Strings => _owner.Strings;
        public AppLanguage SelectedLanguage => _owner.SelectedLanguage;
        public RuntimeContext LatestRuntimeContext => _owner._runtimeShellState.LatestRuntimeContext;
        public RemoteRuntimeData LatestRuntimeData => _owner._runtimeShellState.LatestRuntimeData;
        public bool IsAppUpdateInProgress => _owner._isAppUpdateInProgress;
        public bool IsInstallExecutionInProgress => _owner._isInstallExecutionInProgress;
        public GameCardViewModel? SelectedGame => _owner.SelectedGame;
        public RuntimeShellState RuntimeShellState => _owner._runtimeShellState;
        public ScannedGameState ScannedGameState => _owner._scannedGameState;
        public MainShellOperationLocks OperationLocks => _owner._operationLocks;
        public StartupOverlayViewModel StartupOverlay => _owner.StartupOverlay;
        public ObservableCollection<GameCardViewModel> VisibleCards => _owner.Games;
        public int VisibleGameCount => _owner.Games.Count;
        public string InstallButtonText => _owner.SelectedGameAction.InstallButtonText;
        public ShellViewKind CurrentViewKind => _owner.Navigation.CurrentViewKind;
        public ICommand OpenGameSupportRequestCommand => _owner._openGameSupportRequestCommand;
        public bool SupportedGamesHasEntries => _owner.SupportedGames.HasEntries;

        public ShellInstallSelectionState SelectionState
        {
            get => _owner._selectionState;
            set => _owner._selectionState = value;
        }

        public bool SuppressHomeNavigationForAutoSelection
        {
            get => _owner._suppressHomeNavigationForAutoSelection;
            set => _owner._suppressHomeNavigationForAutoSelection = value;
        }

        public string PreferredOptiScalerVariant
        {
            get => _owner._optiScalerVariantPreference;
            set => _owner._optiScalerVariantPreference = value;
        }

        public string LanguagePreference
        {
            get => _owner._languagePreference;
            set => _owner._languagePreference = value;
        }

        public int ResolveSelectedIndex(GameCardViewModel game) => _owner.Games.IndexOf(game);
        public void SetSettingsStatusText(string message) => _owner.SettingsStatusText = message;
        public void SetScanStatusText(string message) => _owner.ScanStatusText = message;
        public void ApplyStateUpdate(MainViewModelStateUpdate update)
        {
            _owner.ApplyStateUpdate(update);
            if (update.GpuBundleKey is not null)
            {
                _owner.OptiScaler.ApplyGpuBundleKey(update.GpuBundleKey, persistModeChanges: true);
            }
        }
        public void ApplyDeferredStateUpdate(MainViewModelStateUpdate update) =>
            _owner.ApplyDeferredStateUpdate(update);
        public void ApplyBusyStateUpdate(MainViewModelBusyStateUpdate update) => _owner.ApplyBusyStateUpdate(update);
        public void ShutdownApplication() => Application.Current?.Shutdown();
        public string NormalizeLanguagePreference(string? preference) =>
            MainViewModel.NormalizeLanguagePreference(preference);
        public string NormalizeOptiScalerVariantPreference(string? preference) =>
            MainViewModel.NormalizeOptiScalerVariantPreference(preference);
        public AppLanguage ResolvePreferredLanguage(string languagePreference) =>
            _owner.ResolvePreferredLanguage(languagePreference);
        public string ResolveLanguageOptionFromState(string preference) =>
            MainViewModel.ResolveLanguageOptionFromState(preference);
        public void ApplyChangedLanguage(AppLanguage preferredLanguage) => _owner.ApplyLoadedLanguage(preferredLanguage);
        public void ApplyLoadedSettings(string languageOption) => _owner.ApplyLoadedSettingsOption(languageOption);
        public void ApplySavedOptiScalerSettings(
            string optiScalerVariant,
            OptiScalerCommonIniSettingsDocument? settings) =>
            _owner.ApplySavedOptiScalerSettings(optiScalerVariant, settings);
        public void UpdateStartupPreparationState(Func<StartupPreparationState, StartupPreparationState> update) =>
            _owner.UpdateStartupPreparationState(update);
        public string ClearLastErrorCode(string lastErrorCode, string errorCode) =>
            MainViewModel.ClearLastErrorCode(lastErrorCode, errorCode);
        public Task RunStartupAutoScanAsync(CancellationToken cancellationToken) =>
            _owner.RunStartupAutoScanAsync(cancellationToken);
        public void StartDeviceIdentityRulesRefreshInBackground() =>
            _owner.StartDeviceIdentityRulesRefreshInBackground();
        public void ApplyRuntimeSummaryStateUpdate(RuntimeSummaryStateUpdate update) =>
            _owner.ApplyRuntimeSummaryStateUpdate(update);
        public Task RefreshRuntimeDataCatalogByModeAsync(
            RuntimeCatalogRefreshMode refreshMode,
            CancellationToken cancellationToken) =>
            _owner.RefreshRuntimeDataCatalogByModeAsync(refreshMode, cancellationToken);
        public void SetSelectedGame(GameCardViewModel? selectedGame) => _owner.SetSelectedGame(selectedGame);
        public void ApplyRuntimeCatalogSelectionState(ShellInstallSelectionState selectionState) =>
            _owner.ApplyRuntimeCatalogSelectionState(selectionState);
        public void ApplySelectionBridgeState(ShellInstallSelectionState selectionState) =>
            _owner.SelectedGameAction.ApplySelectionBridgeState(selectionState);
        public void ApplyPrecheckRunningIntermediate() => _owner.SelectedGameAction.ApplyPrecheckRunningIntermediate();
        public long IncrementSelectionVersion() => Interlocked.Increment(ref _owner._selectionRequestVersion);
        public long ReadSelectionVersion() => Volatile.Read(ref _owner._selectionRequestVersion);
        public void ClearSelectedGameContext() => _owner.ClearSelectedGameContext();
        public void ApplyOptiScalerVariantOptions() => _owner.OptiScaler.ApplyOptiScalerVariantOptions(
            _owner._runtimeShellState.LatestOptiScalerVariantSelectionOptions,
            _owner._runtimeShellState.EffectiveOptiScalerVariant);
        public void SetCurrentView(ShellViewKind view) => _owner.SetCurrentView(view);
        public void RebuildSupportedGamesRows() => _owner.SupportedGames.RebuildRows();
        public void RefreshSupportedGamesAfterLanguageChange() => _owner.SupportedGames.RefreshAfterLanguageChange();
        public void ApplySelectedGameLocalization() => _owner.SelectedGameAction.ApplyLocalization(_owner.Strings);
        public void StartSupportedGamesWikiRefreshInBackground() => _owner.SupportedGames.StartRefreshInBackground();
        public void RefreshLocalizedStrings() => _owner.RefreshLocalizedStrings();
        public void ApplyLocalizationStateUpdate(LocalizationStateUpdate update) =>
            _owner.ApplyLocalizationStateUpdate(update);
        public void ApplySettingsLanguageOption(string option) => _owner.ApplySettingsLanguageOption(option);
    }
}
