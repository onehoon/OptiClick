using OptiClick.Wpf.ViewModels.Ports;
using OptiClick.Wpf.ViewModels.Ports.ShellInteractionContext;

namespace OptiClick.Wpf.ViewModels;

public sealed partial class MainViewModel
{
    internal MainViewModel(
        MainViewModelCompositionDependencies resolved,
        bool seedMockGameCards,
        bool seedMockScanFolders)
    {
        var appDependencies = resolved.App;
        var shellDependencies = resolved.Shell;
        var installDependencies = resolved.Features.Install;
        var mainOptiScalerSettingsController = installDependencies.MainOptiScalerSettingsController;

        _languageProvider = appDependencies.LanguageProvider;
        _systemPreferredLanguage = _languageProvider.CurrentLanguage;
        _mockDataProvider = appDependencies.MockDataProvider;
        _appStringsProvider = appDependencies.AppStringsProvider;
        _strings = _appStringsProvider.Get(_systemPreferredLanguage);
        _navigationState = shellDependencies.NavigationState;
        var featureRegistry = new MainViewModelFeatureFacadeRegistry();
        var shellPortAccess = new ShellPortAccess(this);
        var shellPortGraph = MainViewModelShellPortComposer.Compose(
            new MainViewModelShellPortCompositionInput
            {
                Dependencies = resolved,
                FeatureRegistry = featureRegistry,
                Access = new MainShellPortAccessGraph
                {
                    App = shellPortAccess,
                    Runtime = shellPortAccess,
                    Startup = shellPortAccess,
                    Selection = shellPortAccess,
                    Install = shellPortAccess,
                    Ui = shellPortAccess,
                    Localization = shellPortAccess,
                    ShellInteractionContext = new MainShellInteractionContextAccesses
                    {
                        ShellCommand = shellPortAccess,
                        StartupAnnouncement = shellPortAccess,
                        AppUpdate = shellPortAccess,
                        UserSettings = shellPortAccess,
                        Language = shellPortAccess,
                        OptiScaler = shellPortAccess
                    }
                },
                OptiScalerSettingsController = mainOptiScalerSettingsController
            });
        var featureGraph = MainViewModelFeatureGraphComposer.Compose(
            new MainViewModelFeatureGraphCompositionInput
            {
                Dependencies = resolved,
                Ports = shellPortGraph.FacadePorts,
                Registry = featureRegistry,
                ShellInteractionContext = shellPortGraph.ShellInteractionContext,
                State = new MainFeatureGraphStateCallbacks
                {
                    ApplyScanFolderStateUpdate = ApplyScanFolderStateUpdate,
                    SetSettingsStatusText = message => SettingsStatusText = message,
                    SetScanStatusText = message => ScanStatusText = message,
                    ApplyAppLog = ApplyAppLog,
                    RefreshHomeCoversOnDispatcherAsync = RefreshHomeCoversOnDispatcherAsync
                },
                RuntimeShellState = _runtimeShellState,
                ScannedGameState = _scannedGameState,
                OperationLocks = _operationLocks,
                OptiScalerSettingsController = mainOptiScalerSettingsController,
                SeedMockGameCards = seedMockGameCards,
                SeedMockScanFolders = seedMockScanFolders,
                SettingsLanguageOptions =
                [
                    LanguageOptionAuto,
                    LanguageOptionKorean,
                    LanguageOptionEnglish
                ],
                InitialSettingsLanguageOption = LanguageOptionAuto
            });
        _features = featureGraph.Features;
        _mainStateApplier = featureGraph.StateApplier;
        var sections = featureGraph.Sections;

        DialogHost = shellDependencies.DialogHost;
        InstallManagementDialogHost = shellDependencies.InstallManagementDialogHost;
        Navigation = shellDependencies.ShellChrome.Navigation;
        RuntimeHeader = shellDependencies.ShellChrome.RuntimeHeader;
        StartupOverlay = shellDependencies.ShellChrome.StartupOverlay;
        ShellBusyState = shellDependencies.ShellChrome.ShellBusyState;
        InitializeCommandSet();
        Home = sections.Home;
        SupportedGames = sections.SupportedGames;
        Scan = sections.Scan;
        OptiScaler = sections.OptiScaler;
        Settings = sections.Settings;

        _selectedLanguage = _languageProvider.CurrentLanguage;
        RefreshLocalizedStrings();
        SelectedGameAction.ApplyLocalization(Strings);
        ApplyLocalizationStateUpdate(_features.ShellInteraction.BuildInitialLocalizationState(
            SelectedLanguage,
            Strings,
            SettingsStatusText,
            ScanStatusText,
            RuntimeHeader.DeviceText,
            RuntimeHeader.GpuText));
        ApplyUserSettings(_features.ShellInteraction.LoadUserSettings());
    }
}
