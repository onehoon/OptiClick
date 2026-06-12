using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using OptiClick.Core.Abstractions;
using OptiClick.Core.Models;
using OptiClick.Core.OptiScaler;
using OptiClick.Core.Runtime;
using OptiClick.Wpf.Install.Archives;
using OptiClick.Wpf.Install.Flow;
using OptiClick.Wpf.Install.Planning;
using OptiClick.Wpf.Install.Presentation;
using OptiClick.Wpf.Install.Uninstall;
using OptiClick.Wpf.Localization;
using OptiClick.Wpf.Logging;
using OptiClick.Wpf.Models;
using OptiClick.Wpf.Services;
using OptiClick.Wpf.Shell.Actions;
using OptiClick.Wpf.Shell.Dialogs;
using OptiClick.Wpf.Shell.Flow;
using OptiClick.Wpf.Shell.Games;
using OptiClick.Wpf.Shell.Gpu;
using OptiClick.Wpf.Shell.Localization;
using OptiClick.Wpf.Shell.Navigation;
using OptiClick.Wpf.Shell.Runtime;
using OptiClick.Wpf.Shell.RuntimeData;
using OptiClick.Wpf.Shell.Scan;
using OptiClick.Wpf.Shell.Selection;
using OptiClick.Wpf.Shell.Settings;
using OptiClick.Wpf.Shell.Startup;
using OptiClick.Wpf.Shell.Support;
using OptiClick.Wpf.Shell.Update;
using OptiClick.Wpf.Shell.Wiki;
using OptiClick.Wpf.ViewModels.Sections;
using OptiClick.Wpf.ViewModels.Sections.Home;
using OptiClick.Wpf.ViewModels.Sections.OptiScaler;
using OptiClick.Wpf.ViewModels.Sections.Scan;
using OptiClick.Wpf.ViewModels.Sections.Settings;
using OptiClick.Wpf.ViewModels.Sections.SupportedGames;
using OptiClick.Wpf.ViewModels.Shell;

namespace OptiClick.Wpf.ViewModels;

public sealed partial class MainViewModel : ViewModelBase, IDisposable
{
    private const string LanguagePreferenceAuto = AppLanguagePreference.Auto;
    private const string LanguagePreferenceKorean = AppLanguagePreference.Korean;
    private const string LanguagePreferenceEnglish = AppLanguagePreference.English;
    private const string LanguageOptionAuto = "Auto";
    private const string LanguageOptionKorean = "\uD55C\uAD6D\uC5B4";
    private const string LanguageOptionEnglish = "English";

    // Core services
    private readonly IWritableAppLanguageProvider _languageProvider;
    private readonly IShellMockDataProvider _mockDataProvider;

    // Install services
    private readonly MainShellFeatureFacades _features;
    private readonly MainViewModelStateApplier _mainStateApplier;

    // Shell services
    private readonly IAppStringsProvider _appStringsProvider;
    private readonly ShellNavigationState _navigationState;
    private readonly MainShellOperationLocks _operationLocks = new();
    // UI state
    private readonly AppLanguage _systemPreferredLanguage = AppLanguage.English;
    private AppLanguage _selectedLanguage = AppLanguage.English;
    private string _languagePreference = LanguagePreferenceAuto;
    private string _optiScalerVariantPreference = OptiScalerVariantCatalogBuilder.StableVariant;
    private StartupPreparationState _startupPreparationState = StartupPreparationState.Empty;

    // Runtime state
    private readonly RuntimeShellState _runtimeShellState = new();
    private readonly ScannedGameState _scannedGameState = new();

    // Install/update state
    private bool _isInstallExecutionInProgress;
    private bool _isAppUpdateInProgress;
    private bool _suppressHomeNavigationForAutoSelection;
    private ShellInstallSelectionState _selectionState = new();
    private long _selectionRequestVersion;
    private AppStrings _strings = null!;

    public MainViewModel(
        MainViewModelRequiredDependencies requiredDependencies,
        MainViewModelRuntimeDependencies? runtime = null,
        MainViewModelScanDependencies? scan = null,
        MainViewModelInstallDependencies? install = null,
        MainViewModelAppDependencies? app = null,
        bool allowDependencyFallbacks = true,
        bool seedMockGameCards = true,
        bool seedMockScanFolders = true)
        : this(
            MainViewModelFallbackDependencyResolver.Resolve(
                requiredDependencies,
                runtime,
                scan,
                install,
                app,
                allowFallbackResolution: allowDependencyFallbacks),
            seedMockGameCards,
            seedMockScanFolders)
    {
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
    public OptiScalerSectionViewModel OptiScaler { get; }
    public SettingsSectionViewModel Settings { get; }
    public StartupPreparationState StartupPreparationState
    {
        get
        {
            lock (_operationLocks.StartupPreparationStateGate)
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

    private Task ApplyLanguageChangeAsync(AppLanguage language, CancellationToken cancellationToken = default)
    {
        return _features.Selection.ApplyLanguageChangeAsync(language, cancellationToken);
    }

    private bool IsKoreanUi => SelectedLanguage == AppLanguage.Korean;

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
        return AppLanguagePreference.NormalizeOrDefault(preference);
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

    private static string NormalizeOptiScalerVariantPreference(string? preference)
    {
        var normalized = OptiScalerVariantCatalogBuilder.NormalizeVariant(preference);
        return string.IsNullOrWhiteSpace(normalized)
            ? OptiScalerVariantCatalogBuilder.StableVariant
            : normalized;
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
