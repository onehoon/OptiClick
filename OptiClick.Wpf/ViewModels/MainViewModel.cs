using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using OptiClick.Core.Abstractions;
using OptiClick.Core.Models;
using OptiClick.Core.Runtime;
using OptiClick.Wpf.Collections;
using OptiClick.Wpf.Install.Archives;
using OptiClick.Wpf.Install.Flow;
using OptiClick.Wpf.Install.Planning;
using OptiClick.Wpf.Install.Presentation;
using OptiClick.Wpf.Install.Uninstall;
using OptiClick.Wpf.Localization;
using OptiClick.Wpf.Logging;
using OptiClick.Wpf.Models;
using OptiClick.Wpf.Shell.Runtime.DeviceIdentity;
using OptiClick.Wpf.Shell.RuntimeData;
using OptiClick.Wpf.Services;
using OptiClick.Wpf.Shell.Actions;
using OptiClick.Wpf.Shell.Dialogs;
using OptiClick.Wpf.Shell.Flow;
using OptiClick.Wpf.Shell.Games;
using OptiClick.Wpf.Shell.Gpu;
using OptiClick.Wpf.Shell.Games.GpuBundle;
using OptiClick.Wpf.Shell.Localization;
using OptiClick.Wpf.Shell.Navigation;
using OptiClick.Wpf.Shell.Runtime;
using OptiClick.Wpf.Shell.Scan;
using OptiClick.Wpf.Shell.Selection;
using OptiClick.Wpf.Shell.Settings;
using OptiClick.Wpf.Shell.Startup;
using OptiClick.Wpf.Shell.Support;
using OptiClick.Wpf.Shell.Update;
using OptiClick.Wpf.Shell.Wiki;
using OptiClick.Wpf.ViewModels.Sections;
using OptiClick.Wpf.ViewModels.Sections.Home;
using OptiClick.Wpf.ViewModels.Sections.Scan;
using OptiClick.Wpf.ViewModels.Sections.Settings;
using OptiClick.Wpf.ViewModels.Sections.SupportedGames;
using OptiClick.Wpf.ViewModels.Shell;
using OptiClick.Infrastructure.FileSystem;
using OperatingSystemSupportState = OptiClick.Infrastructure.Windows.OperatingSystemSupportState;

namespace OptiClick.Wpf.ViewModels;

public sealed partial class MainViewModel : ViewModelBase
{
    private const int MaxSupportedGpuCount = 2;
    private const string LanguagePreferenceAuto = "auto";
    private const string LanguagePreferenceKorean = "ko";
    private const string LanguagePreferenceEnglish = "en";
    private const string LanguageOptionAuto = "Auto";
    private const string LanguageOptionKorean = "\uD55C\uAD6D\uC5B4";
    private const string LanguageOptionEnglish = "English";
    private const string GpuManifestRestartRequiredErrorCode = "gpu_bundle_manifest_restart_required";

    private static readonly Brush AddedFolderStatusBrush = new SolidColorBrush(Color.FromRgb(185, 226, 250));
    private static readonly Brush MissingFolderStatusBrush = new SolidColorBrush(Color.FromRgb(212, 180, 142));
    // Core services
    private readonly IWritableAppLanguageProvider _languageProvider;
    private readonly IShellMockDataProvider _mockDataProvider;

    // Runtime/catalog services
    private readonly IOperatingSystemSupportPolicy _operatingSystemSupportPolicy;
    private readonly IShellGameCardViewModelFactory? _shellGameCardViewModelFactory;
    private readonly DeviceIdentityRulesFlowController _deviceIdentityRulesFlowController;
    private readonly RuntimeContextCoordinator _runtimeContextCoordinator;
    private readonly RuntimeCatalogCoordinator _runtimeCatalogCoordinator;
    private readonly IRemoteGpuBundleManifestClient _gpuBundleManifestClient;
    private readonly IGpuBundleManifestRuleResolver _gpuBundleManifestRuleResolver;

    // Install services
    private readonly GameSelectionFlowController _gameSelectionFlowController;
    private readonly ArchiveReadinessFlowController _archiveReadinessFlowController;
    private readonly InstallExecutionCoordinator _installExecutionCoordinator;
    private readonly UninstallFlowCoordinator _uninstallFlowCoordinator;

    // App/update/support services
    private readonly IAppVersionProvider _appVersionProvider;
    private readonly AppUpdateFlowController _appUpdateFlowController;
    private readonly GameDetailsDialogPresenter _gameDetailsDialogPresenter;
    private readonly IAppLogger _appLogger;
    private readonly IAppLocalDataPathProvider _localDataPathProvider;
    private readonly IAppStringsProvider _appStringsProvider;
    private readonly IFirstRunStateStore _firstRunStateStore;
    private readonly ShellNavigationState _navigationState;
    private readonly DialogPresenter _dialogPresenter;
    private readonly IInstallManagementDialogService _installManagementDialogService;
    private readonly OnceDialogGate _remoteCatalogDialogGate;
    private readonly UserSettingsController _userSettingsController;
    private readonly ScanVisibleGameResolver _scanVisibleGameResolver;
    private readonly StartupNoticePresenter _startupNoticePresenter;
    private readonly StartupAnnouncementFlowController _startupAnnouncementFlowController;
    private readonly ShellCommandActionController _shellCommandActionController;
    private readonly LocalizationStateController _localizationStateController;
    private readonly RuntimeSummaryStateController _runtimeSummaryStateController;
    private readonly MainViewModelBusyStateApplier _busyStateApplier;
    private readonly InstallPopupPresenter _installPopupPresenter;
    private readonly FlowLogDispatcher _flowLogDispatcher;
    private readonly MainViewModelFlowRequestFactory _flowRequestFactory;
    private readonly AppUpdateCoordinator _appUpdateCoordinator;
    private readonly MainViewModelResultApplier _resultApplier;
    private readonly GameCardSelectionStateController _gameCardSelectionStateController;
    private readonly IGameMasterCoverPrefetchService _gameMasterCoverPrefetchService;
    private readonly ICoverCacheBootstrapService _coverCacheBootstrapService;
    private readonly StartupBackgroundTaskManager _startupBackgroundTaskManager;
    private readonly ArchiveReadinessRefreshCoordinator _archiveReadinessRefreshCoordinator;
    private readonly ArchiveReadinessWarmupController _archiveReadinessWarmupController;
    private readonly StartupFlowCoordinator _startupFlowCoordinator;
    private readonly SelectionPopupCoordinator _selectionPopupCoordinator;
    private readonly GpuSelectionCoordinator _gpuSelectionCoordinator;
    private readonly object _startupPreparationStateGate = new();
    private readonly object _startupDialogsReadyGate = new();
    private Task _startupDialogsReadyTask = Task.CompletedTask;

    // Locks
    private readonly SemaphoreSlim _deviceRulesRefreshLock = new(1, 1);
    private readonly SemaphoreSlim _scanLock = new(1, 1);
    private readonly SemaphoreSlim _installExecutionLock = new(1, 1);
    // UI state
    private readonly AppLanguage _systemPreferredLanguage = AppLanguage.English;
    private AppLanguage _selectedLanguage = AppLanguage.English;
    private string _languagePreference = LanguagePreferenceAuto;
    private StartupPreparationState _startupPreparationState = StartupPreparationState.Empty;

    // Runtime state
    private readonly RuntimeShellState _runtimeShellState = new();
    private readonly ScannedGameState _scannedGameState = new();
    private bool _isGameMasterCoverPrefetchStarted;
    private int _homeCoverPrefetchRunning;

    // Install/update state
    private bool _isInstallExecutionInProgress;
    private bool _isAppUpdateInProgress;
    private bool _gpuManifestRestartRequired;
    private bool _gpuManifestRestartDialogShown;
    private bool _suppressHomeNavigationForAutoSelection;
    private bool _pendingAdministratorRelaunchCancelledNotice;
    private ShellInstallSelectionState _selectionState = new();
    private long _selectionRequestVersion;
    private AppStrings _strings = new AppStringsProvider().Get(AppLanguage.English);

    public MainViewModel(
        MainViewModelRequiredDependencies requiredDependencies,
        MainViewModelRuntimeDependencies? runtime = null,
        MainViewModelScanDependencies? scan = null,
        MainViewModelInstallDependencies? install = null,
        MainViewModelAppDependencies? app = null,
        bool allowDependencyFallbacks = true,
        bool seedMockGameCards = true,
        bool seedMockScanFolders = true)
    {
        var resolved = MainViewModelDependencyResolver.Resolve(
            requiredDependencies,
            runtime,
            scan,
            install,
            app,
            allowFallbackResolution: allowDependencyFallbacks);

        _languageProvider = resolved.LanguageProvider;
        _systemPreferredLanguage = _languageProvider.CurrentLanguage;
        _mockDataProvider = resolved.MockDataProvider;
        _operatingSystemSupportPolicy = resolved.OperatingSystemSupportPolicy;
        _shellGameCardViewModelFactory = resolved.ShellGameCardViewModelFactory;
        var runtimeContextFlowController = resolved.RuntimeContextFlowController;
        _deviceIdentityRulesFlowController = resolved.DeviceIdentityRulesFlowController;
        var runtimeCatalogFlowController = resolved.RuntimeCatalogFlowController;
        var runtimeEndpointStatusPresenter = resolved.RuntimeEndpointStatusPresenter;
        _gpuBundleManifestClient = resolved.GpuBundleManifestClient;
        _gpuBundleManifestRuleResolver = resolved.GpuBundleManifestRuleResolver;
        _gameSelectionFlowController = resolved.GameSelectionFlowController;
        var scanFlowController = resolved.ScanFlowController;
        _appVersionProvider = resolved.AppVersionProvider;
        var scanFolderDiscoveryService = resolved.ScanFolderDiscoveryService;
        _appLogger = resolved.AppLogger;
        _localDataPathProvider = resolved.LocalDataPathProvider;
        _appStringsProvider = resolved.AppStringsProvider;
        _firstRunStateStore = resolved.FirstRunStateStore;
        _flowLogDispatcher = resolved.FlowLogDispatcher;
        _flowRequestFactory = resolved.FlowRequestFactory;
        _navigationState = resolved.NavigationState;
        _dialogPresenter = resolved.DialogPresenter;
        _installManagementDialogService = resolved.InstallManagementDialogService;
        _remoteCatalogDialogGate = resolved.RemoteCatalogDialogGate;
        _userSettingsController = resolved.UserSettingsController;
        var scanFolderListController = resolved.ScanFolderListController;
        var scanFolderActionController = resolved.ScanFolderActionController;
        var scanOrchestratorFactory = resolved.ScanOrchestratorFactory;
        _scanVisibleGameResolver = resolved.ScanVisibleGameResolver;
        _installPopupPresenter = resolved.InstallPopupPresenter;
        _archiveReadinessFlowController = resolved.ArchiveReadinessFlowController;
        _resultApplier = resolved.ResultApplier;
        _installExecutionCoordinator = new InstallExecutionCoordinator(resolved.InstallFlowController);
        _uninstallFlowCoordinator = new UninstallFlowCoordinator(
            resolved.OptiClickUninstallPlanBuilder,
            resolved.OptiClickUninstallExecutor,
            _dialogPresenter,
            _appLogger);
        _startupNoticePresenter = resolved.StartupNoticePresenter;
        _startupAnnouncementFlowController = resolved.StartupAnnouncementFlowController;
        _shellCommandActionController = resolved.ShellCommandActionController;
        _localizationStateController = resolved.LocalizationStateController;
        _runtimeSummaryStateController = resolved.RuntimeSummaryStateController;
        var supportedGamesWikiMarkdownLoader = resolved.SupportedGamesWikiMarkdownLoader;
        _busyStateApplier = resolved.BusyStateApplier;
        _appUpdateFlowController = resolved.AppUpdateFlowController;
        _appUpdateCoordinator = new AppUpdateCoordinator(_appUpdateFlowController);
        _gameDetailsDialogPresenter = resolved.GameDetailsDialogPresenter;
        _gameCardSelectionStateController = resolved.GameCardSelectionStateController;
        _gameMasterCoverPrefetchService = resolved.GameMasterCoverPrefetchService;
        _coverCacheBootstrapService = resolved.CoverCacheBootstrapService;
        _startupBackgroundTaskManager = resolved.StartupBackgroundTaskManager;
        _archiveReadinessRefreshCoordinator = resolved.ArchiveReadinessRefreshCoordinator;
        _archiveReadinessWarmupController = resolved.ArchiveReadinessWarmupController;
        _startupFlowCoordinator = resolved.StartupFlowCoordinator;
        _selectionPopupCoordinator = new SelectionPopupCoordinator(
            _gameSelectionFlowController,
            _dialogPresenter,
            _flowLogDispatcher,
            _appLogger);
        _gpuSelectionCoordinator = new GpuSelectionCoordinator(MaxSupportedGpuCount);
        _runtimeContextCoordinator = new RuntimeContextCoordinator(
            runtimeContextFlowController,
            _runtimeSummaryStateController,
            _flowLogDispatcher,
            _gpuSelectionCoordinator);
        _runtimeCatalogCoordinator = new RuntimeCatalogCoordinator(
            runtimeCatalogFlowController,
            runtimeEndpointStatusPresenter);
        DialogHost = resolved.DialogHost;
        InstallManagementDialogHost = resolved.InstallManagementDialogHost;
        var games = seedMockGameCards
            ? new BatchedObservableCollection<GameCardViewModel>(_mockDataProvider.CreateGames())
            : new BatchedObservableCollection<GameCardViewModel>();
        var defaultFolders = seedMockScanFolders
            ? new ObservableCollection<ScanFolderRowViewModel>(_mockDataProvider.CreateDefaultFolders())
            : new ObservableCollection<ScanFolderRowViewModel>(
                scanFolderDiscoveryService?.DiscoverDefaultFolders() ?? []);
        var addedFolders = seedMockScanFolders
            ? new ObservableCollection<ScanFolderRowViewModel>(_mockDataProvider.CreateAddedFolders())
            : new ObservableCollection<ScanFolderRowViewModel>(LoadAddedScanFoldersFromManifest(defaultFolders, scanFolderActionController));
        var settingsLanguageOptions = new ObservableCollection<string> { LanguageOptionAuto, LanguageOptionKorean, LanguageOptionEnglish };
        Navigation = resolved.ShellChrome.Navigation;
        RuntimeHeader = resolved.ShellChrome.RuntimeHeader;
        StartupOverlay = resolved.ShellChrome.StartupOverlay;
        ShellBusyState = resolved.ShellChrome.ShellBusyState;
        InitializeCommandSet();
        var scanResultCoordinator = new ScanResultCoordinator(
            new ScanResultCoordinatorOptions
            {
                FlowLogDispatcher = _flowLogDispatcher,
                FlowLogFallbackCategory = MainViewModelLogCategories.Scan,
                ResultApplier = _resultApplier,
                DialogPresenter = _dialogPresenter,
                StringsAccessor = () => Strings,
                GameCountAccessor = () => Games.Count,
                RemoteCatalogErrorCodeAccessor = () => _runtimeShellState.LatestRemoteCatalogErrorCode,
                ReadSuppressHomeNavigationForAutoSelection = () => _suppressHomeNavigationForAutoSelection,
                SetSuppressHomeNavigationForAutoSelection = value => _suppressHomeNavigationForAutoSelection = value,
                ApplyStateUpdate = ApplyStateUpdate,
                SetCurrentView = SetCurrentView,
                RecomputeSelectionAfterScanAsync = RecomputeSelectionAfterScanAsync
            });
        var scanOrchestrator = scanOrchestratorFactory.Create(
            new ScanOrchestratorFactoryInput
            {
                StringsAccessor = () => Strings,
                ScanFlowController = scanFlowController,
                ScanLock = _scanLock,
                ScannedGameState = _scannedGameState,
                DialogPresenter = _dialogPresenter,
                IsMultiGpuBlocked = () => _gpuSelectionCoordinator.MultiGpuBlocked,
                BuildScanRequest = BuildScanRequest,
                ScanResultCoordinator = scanResultCoordinator,
                ClearVisibleGameCards = () => ReplaceGameCards([]),
                LogWarning = message => LogWarning(MainViewModelLogCategories.Scan, message)
            });
        var sections = resolved.ShellSectionsFactory.Create(
            new ShellSectionsFactoryInput
            {
                Home = new HomeSectionFactoryInput
                {
                    StringsAccessor = () => Strings,
                    Games = games,
                    SelectGameAsync = (game, cancellationToken) => SelectGameCardAsync(game, cancellationToken),
                    ShowDetails = ShowDetailsDialog,
                    ShowInstallAsync = ShowInstallDialogAsync,
                    CanSelectGame = () => !_isInstallExecutionInProgress && !_isAppUpdateInProgress,
                    CanShowDetails = () => SelectedGame is not null,
                    CanShowInstall = () => SelectedGame is not null
                                          && !_isInstallExecutionInProgress
                                          && !_isAppUpdateInProgress
                                          && !ShouldBlockStartupForUnsupportedOperatingSystem(),
                    OnSelectGameException = ex => LogError(MainViewModelLogCategories.Command, "select game command failed", ex),
                    OnShowInstallException = ex => LogError(MainViewModelLogCategories.Command, "install command failed", ex)
                },
                Scan = new ScanSectionFactoryInput
                {
                    StringsAccessor = () => Strings,
                    DefaultFolders = defaultFolders,
                    AddedFolders = addedFolders,
                    ScanFolderListController = scanFolderListController,
                    ScanFolderActionController = scanFolderActionController,
                    ApplyScanFolderActionResult = result => ApplyDeferredStateUpdate(
                        _resultApplier.CreateScanFolderActionStateUpdate(result)),
                    ScanOrchestrator = scanOrchestrator,
                    ShowHome = () => SetCurrentView(ShellViewKind.Home),
                    AddedFolderStatusBrush = AddedFolderStatusBrush,
                    MissingFolderStatusBrush = MissingFolderStatusBrush,
                    OnScanCommandException = ex => LogError(MainViewModelLogCategories.Command, "save and scan command failed", ex)
                },
                SupportedGames = new SupportedGamesSectionFactoryInput
                {
                    SupportedGamesWikiMarkdownLoader = supportedGamesWikiMarkdownLoader,
                    StartupBackgroundTaskManager = _startupBackgroundTaskManager,
                    AppLogger = _appLogger,
                    StringsAccessor = () => Strings,
                    SelectedLanguageAccessor = () => SelectedLanguage,
                    CurrentViewKindAccessor = () => Navigation.CurrentViewKind,
                    OpenGameSupportRequestCommandAccessor = () => _openGameSupportRequestCommand,
                    UpdateStartupPreparationState = UpdateStartupPreparationState
                },
                Settings = new SettingsSectionFactoryInput
                {
                    StringsAccessor = () => Strings,
                    DialogPresenter = _dialogPresenter,
                    LocalDataPathProvider = _localDataPathProvider,
                    AppLogger = _appLogger,
                    IsKoreanUi = () => IsKoreanUi,
                    SettingsLanguageOptions = settingsLanguageOptions,
                    InitialSettingsLanguageOption = LanguageOptionAuto,
                    ApplySettingsLanguageOption = ApplySettingsLanguageOption,
                    IsInstallExecutionInProgress = () => _isInstallExecutionInProgress,
                    OpenLogFolder = OpenLogFolder,
                    OpenSupportRequest = OpenSupportRequest,
                    OnRefreshInstallFilesException = ex => LogError(MainViewModelLogCategories.Command, "reset app cache command failed", ex)
                }
            });

        Home = sections.Home;
        SupportedGames = sections.SupportedGames;
        Scan = sections.Scan;
        Settings = sections.Settings;

        _selectedLanguage = _languageProvider.CurrentLanguage;
        RefreshLocalizedStrings();
        SelectedGameAction.ApplyLocalization(Strings);
        ApplyLocalizationStateUpdate(_localizationStateController.BuildInitialState(
            SelectedLanguage,
            Strings,
            SettingsStatusText,
            ScanStatusText,
            RuntimeHeader.DeviceText,
            RuntimeHeader.GpuText));
        ApplyUserSettings(_userSettingsController.Load());
    }

    private ObservableCollection<GameCardViewModel> Games => Home.Games;
    public DialogHostViewModel DialogHost { get; }
    public InstallManagementDialogHostViewModel InstallManagementDialogHost { get; }
    private SelectedGameActionViewModel SelectedGameAction => Home.SelectedGameAction;
    public ShellNavigationViewModel Navigation { get; }
    public RuntimeHeaderViewModel RuntimeHeader { get; }
    public StartupOverlayViewModel StartupOverlay { get; }
    public ShellBusyStateViewModel ShellBusyState { get; }
    public ShellCommandsViewModel Commands { get; private set; } = null!;
    public HomeSectionViewModel Home { get; }
    public ScanSectionViewModel Scan { get; }
    public SupportedGamesSectionViewModel SupportedGames { get; }
    public SettingsSectionViewModel Settings { get; }
    public StartupPreparationState StartupPreparationState
    {
        get
        {
            lock (_startupPreparationStateGate)
            {
                return _startupPreparationState;
            }
        }
    }

    public AppStrings Strings
    {
        get => _strings;
        private set => SetProperty(ref _strings, value);
    }
    public string WindowTitleWithVersion => $"{Strings.WindowTitle} v{GetCurrentAppVersion()}";

    private string SettingsStatusText
    {
        get => Settings.SettingsStatusText;
        set => Settings.SettingsStatusText = value;
    }

    public AppLanguage SelectedLanguage
    {
        get => _selectedLanguage;
        set
        {
            if (!SetProperty(ref _selectedLanguage, value))
            {
                return;
            }

            _ = ApplyLanguageChangeAsync(value);
        }
    }

    private async Task ApplyLanguageChangeAsync(AppLanguage language, CancellationToken cancellationToken = default)
    {
        try
        {
            _languageProvider.SetLanguage(language);
            LogInfo(MainViewModelLogCategories.I18n, $"ui_language_changed source=settings value={ToLanguageCode(language)}");
            RefreshLocalizedStrings();
            SelectedGameAction.ApplyLocalization(Strings);
            SupportedGames.RefreshAfterLanguageChange();
            ApplyLocalizationStateUpdate(_localizationStateController.BuildRefreshState(language, Strings));
            await RefreshRuntimeContextAsync(cancellationToken);
            await RefreshRuntimeDataCatalogAsync(cancellationToken);
            await RecomputeSelectionAfterScanAsync(cancellationToken, navigateHome: false);
        }
        catch (Exception ex)
        {
            LogWarning(MainViewModelLogCategories.I18n, $"language change refresh failed type={ex.GetType().Name}");
        }
    }

    private bool IsKoreanUi => SelectedLanguage == AppLanguage.Korean;

    private static string ToLanguageCode(AppLanguage language)
    {
        return language == AppLanguage.Korean ? "ko" : "en";
    }

    private static string NormalizeLanguageOption(string? option)
    {
        return string.Equals(option, LanguageOptionKorean, StringComparison.Ordinal)
            ? LanguageOptionKorean
            : string.Equals(option, LanguageOptionEnglish, StringComparison.Ordinal)
                ? LanguageOptionEnglish
                : LanguageOptionAuto;
    }

    private static string NormalizeLanguagePreference(string? preference)
    {
        return string.Equals(preference, LanguagePreferenceKorean, StringComparison.OrdinalIgnoreCase)
            ? LanguagePreferenceKorean
            : string.Equals(preference, LanguagePreferenceEnglish, StringComparison.OrdinalIgnoreCase)
                ? LanguagePreferenceEnglish
                : LanguagePreferenceAuto;
    }

    private static string ResolveLanguageOptionFromState(string preference)
    {
        var normalizedPreference = NormalizeLanguagePreference(preference);
        if (normalizedPreference == LanguagePreferenceKorean)
        {
            return LanguageOptionKorean;
        }

        if (normalizedPreference == LanguagePreferenceEnglish)
        {
            return LanguageOptionEnglish;
        }

        return LanguageOptionAuto;
    }

    private void ApplySettingsLanguageOption(string option)
    {
        var normalizedOption = NormalizeLanguageOption(option);
        var preference = normalizedOption switch
        {
            LanguageOptionKorean => LanguagePreferenceKorean,
            LanguageOptionEnglish => LanguagePreferenceEnglish,
            _ => LanguagePreferenceAuto
        };

        _languagePreference = preference;

        var nextLanguage = preference switch
        {
            LanguagePreferenceKorean => AppLanguage.Korean,
            LanguagePreferenceEnglish => AppLanguage.English,
            _ => ResolveAutoLanguage()
        };

        if (SelectedLanguage != nextLanguage)
        {
            SelectedLanguage = nextLanguage;
        }

        SaveUserSettings();
    }

    private AppLanguage ResolveAutoLanguage()
    {
        return _systemPreferredLanguage;
    }

    private string ScanStatusText
    {
        get => Scan.ScanStatusText;
        set => Scan.ScanStatusText = value;
    }

    private GameCardViewModel? SelectedGame
    {
        get => Home.SelectedGame;
        set => Home.SelectedGame = value;
    }

}
