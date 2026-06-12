using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using OptiClick.Core.OptiScaler;
using OptiClick.Core.Runtime;
using OptiClick.Core.Scan;
using OptiClick.Wpf.Collections;
using OptiClick.Wpf.Install.Archives;
using OptiClick.Wpf.Localization;
using OptiClick.Wpf.Logging;
using OptiClick.Wpf.Services;
using OptiClick.Wpf.Shell.Dialogs;
using OptiClick.Wpf.Shell.Flow;
using OptiClick.Wpf.Shell.Navigation;
using OptiClick.Wpf.Shell.Scan;
using OptiClick.Wpf.Shell.Startup;
using OptiClick.Wpf.Shell.Wiki;
using OptiClick.Wpf.ViewModels;
using OptiClick.Wpf.ViewModels.Sections.OptiScaler;
using OptiClick.Wpf.ViewModels.Sections.Scan;

namespace OptiClick.Wpf.ViewModels.Sections;

public sealed class ShellSectionsCompositionFactory
{
    private static readonly Brush AutoDetectedFolderStatusBrush = new SolidColorBrush(Color.FromRgb(179, 227, 186));
    private static readonly Brush AddedFolderStatusBrush = new SolidColorBrush(Color.FromRgb(185, 226, 250));
    private static readonly Brush MissingFolderStatusBrush = new SolidColorBrush(Color.FromRgb(212, 180, 142));

    public ShellSections Create(ShellSectionsCompositionFactoryInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var home = input.Home;
        var scan = input.Scan;
        var supportedGames = input.SupportedGames;
        var optiScaler = input.OptiScaler;
        var settings = input.Settings;

        var games = input.SeedMockGameCards
            ? new BatchedObservableCollection<GameCardViewModel>(input.MockDataProvider.CreateGames())
            : new BatchedObservableCollection<GameCardViewModel>();
        var defaultFolders = input.SeedMockScanFolders
            ? new ObservableCollection<ScanFolderRowViewModel>(input.MockDataProvider.CreateDefaultFolders())
            : new ObservableCollection<ScanFolderRowViewModel>(CreateDefaultFolderRows(scan));
        var addedFolders = input.SeedMockScanFolders
            ? new ObservableCollection<ScanFolderRowViewModel>(input.MockDataProvider.CreateAddedFolders())
            : new ObservableCollection<ScanFolderRowViewModel>(LoadAddedScanFoldersFromManifest(input, defaultFolders));
        scan.ScanFolderListController.RelocalizeRows(
            defaultFolders,
            addedFolders,
            input.StringsAccessor(),
            AddedFolderStatusBrush,
            MissingFolderStatusBrush);

        var scanResultCoordinator = scan.ScanResultCoordinatorFactory.Create(
            new ScanResultCoordinatorFactoryInput
            {
                FlowLogDispatcher = scan.FlowLogDispatcher,
                FlowLogFallbackCategory = scan.ScanLogCategory,
                CreateScanStateUpdate = scan.CreateScanStateUpdate,
                DialogPresenter = scan.DialogPresenter,
                StringsAccessor = input.StringsAccessor,
                GameCountAccessor = () => games.Count,
                RemoteCatalogErrorCodeAccessor = scan.RemoteCatalogErrorCodeAccessor,
                ReadSuppressHomeNavigationForAutoSelection = scan.ReadSuppressHomeNavigationForAutoSelection,
                SetSuppressHomeNavigationForAutoSelection = scan.SetSuppressHomeNavigationForAutoSelection,
                ApplyStateUpdate = scan.ApplyStateUpdate,
                SetCurrentView = scan.SetCurrentView,
                RecomputeSelectionAfterScanAsync = scan.RecomputeSelectionAfterScanAsync
            });
        var scanOrchestrator = scan.ScanOrchestratorFactory.Create(
            new ScanOrchestratorFactoryInput
            {
                StringsAccessor = input.StringsAccessor,
                ScanFlowController = scan.ScanFlowController,
                ScanLock = scan.ScanLock,
                ScannedGameState = scan.ScannedGameState,
                DialogPresenter = scan.DialogPresenter,
                IsMultiGpuBlocked = scan.IsMultiGpuBlocked,
                BuildScanRequest = scan.BuildScanRequest,
                ScanResultCoordinator = scanResultCoordinator,
                ClearVisibleGameCards = scan.ClearVisibleGameCards,
                LogWarning = scan.LogScanWarning
            });

        return input.ShellSectionsFactory.Create(
            new ShellSectionsFactoryInput
            {
                Home = new HomeSectionFactoryInput
                {
                    StringsAccessor = input.StringsAccessor,
                    Games = games,
                    SelectGameAsync = home.SelectGameAsync,
                    ShowDetails = home.ShowDetails,
                    ShowInstallAsync = home.ShowInstallAsync,
                    CanSelectGame = home.CanSelectGame,
                    CanShowDetails = home.CanShowDetails,
                    CanShowInstall = home.CanShowInstall,
                    OnSelectGameException = home.OnSelectGameException,
                    OnShowInstallException = home.OnShowInstallException
                },
                Scan = new ScanSectionFactoryInput
                {
                    StringsAccessor = input.StringsAccessor,
                    DefaultFolders = defaultFolders,
                    AddedFolders = addedFolders,
                    ScanFolderListController = scan.ScanFolderListController,
                    ScanFolderActionController = scan.ScanFolderActionController,
                    ApplyScanFolderActionResult = scan.ApplyScanFolderActionResult,
                    ScanOrchestrator = scanOrchestrator,
                    ShowHome = scan.ShowHome,
                    AddedFolderStatusBrush = AddedFolderStatusBrush,
                    MissingFolderStatusBrush = MissingFolderStatusBrush,
                    OnScanCommandException = scan.OnScanCommandException
                },
                SupportedGames = new SupportedGamesSectionFactoryInput
                {
                    SupportedGamesWikiMarkdownLoader = supportedGames.SupportedGamesWikiMarkdownLoader,
                    StartupBackgroundTaskManager = supportedGames.StartupBackgroundTaskManager,
                    AppLogger = supportedGames.AppLogger,
                    StringsAccessor = input.StringsAccessor,
                    SelectedLanguageAccessor = supportedGames.SelectedLanguageAccessor,
                    CurrentViewKindAccessor = supportedGames.CurrentViewKindAccessor,
                    OpenGameSupportRequestCommandAccessor = supportedGames.OpenGameSupportRequestCommandAccessor,
                    UpdateStartupPreparationState = supportedGames.UpdateStartupPreparationState
                },
                OptiScaler = new OptiScalerSectionFactoryInput
                {
                    StringsAccessor = input.StringsAccessor,
                    OptiScalerVariantOptions = optiScaler.OptiScalerVariantOptions,
                    InitialOptiScalerVariantOption = optiScaler.InitialOptiScalerVariantOption,
                    InitialCommonIniSettings = optiScaler.InitialCommonIniSettings,
                    SaveHandler = optiScaler.SaveHandler
                },
                Settings = new SettingsSectionFactoryInput
                {
                    StringsAccessor = input.StringsAccessor,
                    DialogPresenter = settings.DialogPresenter,
                    LocalDataPathProvider = settings.LocalDataPathProvider,
                    AppLogger = settings.AppLogger,
                    IsKoreanUi = settings.IsKoreanUi,
                    SettingsLanguageOptions = settings.SettingsLanguageOptions,
                    InitialSettingsLanguageOption = settings.InitialSettingsLanguageOption,
                    ApplySettingsLanguageOption = settings.ApplySettingsLanguageOption,
                    IsInstallExecutionInProgress = settings.IsInstallExecutionInProgress,
                    OpenLogFolder = settings.OpenLogFolder,
                    OpenSupportRequest = settings.OpenSupportRequest,
                    OnRefreshInstallFilesException = settings.OnRefreshInstallFilesException
                }
            });
    }

    private static IReadOnlyList<ScanFolderRowViewModel> LoadAddedScanFoldersFromManifest(
        ShellSectionsCompositionFactoryInput input,
        IReadOnlyCollection<ScanFolderRowViewModel> defaultFolders)
    {
        var result = input.Scan.ScanFolderActionController.LoadAddedFoldersFromManifest(
            defaultFolders,
            input.StringsAccessor(),
            AddedFolderStatusBrush,
            MissingFolderStatusBrush);

        return input.Scan.ApplyInitialScanFolderLoadResult(result);
    }

    private static IReadOnlyList<ScanFolderRowViewModel> CreateDefaultFolderRows(ScanSectionCompositionInput scan)
    {
        var entries = scan.ScanFolderDiscoveryService?.DiscoverDefaultFolders() ?? [];
        return entries
            .Select(static entry => new ScanFolderRowViewModel(
                entry.Name,
                entry.Path,
                "",
                entry.IsChecked,
                true,
                false,
                AutoDetectedFolderStatusBrush))
            .ToArray();
    }
}

public sealed record ShellSectionsCompositionFactoryInput
{
    public required ShellSectionsFactory ShellSectionsFactory { get; init; }
    public required IShellMockDataProvider MockDataProvider { get; init; }
    public required bool SeedMockGameCards { get; init; }
    public required bool SeedMockScanFolders { get; init; }
    public required Func<AppStrings> StringsAccessor { get; init; }
    public required HomeSectionCompositionInput Home { get; init; }
    public required ScanSectionCompositionInput Scan { get; init; }
    public required SupportedGamesSectionCompositionInput SupportedGames { get; init; }
    public required OptiScalerSectionCompositionInput OptiScaler { get; init; }
    public required SettingsSectionCompositionInput Settings { get; init; }
}

public sealed record HomeSectionCompositionInput
{
    public required Func<GameCardViewModel, CancellationToken, Task> SelectGameAsync { get; init; }
    public required Action ShowDetails { get; init; }
    public required Func<CancellationToken, Task> ShowInstallAsync { get; init; }
    public required Func<bool> CanSelectGame { get; init; }
    public required Func<bool> CanShowDetails { get; init; }
    public required Func<bool> CanShowInstall { get; init; }
    public Action<Exception>? OnSelectGameException { get; init; }
    public Action<Exception>? OnShowInstallException { get; init; }
}

public sealed record ScanSectionCompositionInput
{
    public IScanFolderDiscoveryService? ScanFolderDiscoveryService { get; init; }
    public required ScanResultCoordinatorFactory ScanResultCoordinatorFactory { get; init; }
    public required ScanOrchestratorFactory ScanOrchestratorFactory { get; init; }
    public required ScanFlowController ScanFlowController { get; init; }
    public required ScanFolderListController ScanFolderListController { get; init; }
    public required ScanFolderActionController ScanFolderActionController { get; init; }
    public required SemaphoreSlim ScanLock { get; init; }
    public required ScannedGameState ScannedGameState { get; init; }
    public required DialogPresenter DialogPresenter { get; init; }
    public required FlowLogDispatcher FlowLogDispatcher { get; init; }
    public required Func<ScanFlowResult, MainViewModelStateUpdate> CreateScanStateUpdate { get; init; }
    public required Func<string> RemoteCatalogErrorCodeAccessor { get; init; }
    public required Func<bool> ReadSuppressHomeNavigationForAutoSelection { get; init; }
    public required Action<bool> SetSuppressHomeNavigationForAutoSelection { get; init; }
    public required Action<MainViewModelStateUpdate> ApplyStateUpdate { get; init; }
    public required Action<ShellViewKind> SetCurrentView { get; init; }
    public required Func<CancellationToken, bool, Task> RecomputeSelectionAfterScanAsync { get; init; }
    public required Func<bool> IsMultiGpuBlocked { get; init; }
    public required Func<IReadOnlyList<string>, ScanFlowRequest> BuildScanRequest { get; init; }
    public required Action ClearVisibleGameCards { get; init; }
    public required Action<string> LogScanWarning { get; init; }
    public required Func<ScanFolderActionResult, IReadOnlyList<ScanFolderRowViewModel>> ApplyInitialScanFolderLoadResult { get; init; }
    public required Action<ScanFolderActionResult> ApplyScanFolderActionResult { get; init; }
    public required Action ShowHome { get; init; }
    public Action<Exception>? OnScanCommandException { get; init; }
    public string ScanLogCategory { get; init; } = "scan";
}

public sealed record SupportedGamesSectionCompositionInput
{
    public required ISupportedGamesWikiMarkdownLoader SupportedGamesWikiMarkdownLoader { get; init; }
    public required StartupBackgroundTaskManager StartupBackgroundTaskManager { get; init; }
    public required IAppLogger AppLogger { get; init; }
    public required Func<AppLanguage> SelectedLanguageAccessor { get; init; }
    public required Func<ShellViewKind> CurrentViewKindAccessor { get; init; }
    public required Func<ICommand> OpenGameSupportRequestCommandAccessor { get; init; }
    public required Action<Func<StartupPreparationState, StartupPreparationState>> UpdateStartupPreparationState { get; init; }
}

public sealed record SettingsSectionCompositionInput
{
    public required DialogPresenter DialogPresenter { get; init; }
    public required IAppLocalDataPathProvider LocalDataPathProvider { get; init; }
    public required IAppLogger AppLogger { get; init; }
    public required ObservableCollection<string> SettingsLanguageOptions { get; init; }
    public required string InitialSettingsLanguageOption { get; init; }
    public required Func<bool> IsKoreanUi { get; init; }
    public required Action<string> ApplySettingsLanguageOption { get; init; }
    public required Func<bool> IsInstallExecutionInProgress { get; init; }
    public required Action OpenLogFolder { get; init; }
    public required Action OpenSupportRequest { get; init; }
    public Action<Exception>? OnRefreshInstallFilesException { get; init; }
}

public sealed record OptiScalerSectionCompositionInput
{
    public required ObservableCollection<OptiScalerVariantSelectionOption> OptiScalerVariantOptions { get; init; }
    public required string InitialOptiScalerVariantOption { get; init; }
    public required OptiScalerCommonIniSettingsDocument InitialCommonIniSettings { get; init; }
    public required IOptiScalerSectionSaveHandler SaveHandler { get; init; }
}
