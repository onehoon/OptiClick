using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using OptiClick.Core.Runtime;
using OptiClick.Wpf.Collections;
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
using OptiClick.Wpf.ViewModels.Sections.Scan;

namespace OptiClick.Wpf.ViewModels.Sections;

public sealed class ShellSectionsCompositionFactory
{
    private static readonly Brush AddedFolderStatusBrush = new SolidColorBrush(Color.FromRgb(185, 226, 250));
    private static readonly Brush MissingFolderStatusBrush = new SolidColorBrush(Color.FromRgb(212, 180, 142));

    public ShellSections Create(ShellSectionsCompositionFactoryInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var games = input.SeedMockGameCards
            ? new BatchedObservableCollection<GameCardViewModel>(input.MockDataProvider.CreateGames())
            : new BatchedObservableCollection<GameCardViewModel>();
        var defaultFolders = input.SeedMockScanFolders
            ? new ObservableCollection<ScanFolderRowViewModel>(input.MockDataProvider.CreateDefaultFolders())
            : new ObservableCollection<ScanFolderRowViewModel>(
                input.ScanFolderDiscoveryService?.DiscoverDefaultFolders() ?? []);
        var addedFolders = input.SeedMockScanFolders
            ? new ObservableCollection<ScanFolderRowViewModel>(input.MockDataProvider.CreateAddedFolders())
            : new ObservableCollection<ScanFolderRowViewModel>(LoadAddedScanFoldersFromManifest(input, defaultFolders));

        var scanResultCoordinator = input.ScanResultCoordinatorFactory.Create(
            new ScanResultCoordinatorFactoryInput
            {
                FlowLogDispatcher = input.FlowLogDispatcher,
                FlowLogFallbackCategory = input.ScanLogCategory,
                ResultApplier = input.ResultApplier,
                DialogPresenter = input.DialogPresenter,
                StringsAccessor = input.StringsAccessor,
                GameCountAccessor = () => games.Count,
                RemoteCatalogErrorCodeAccessor = input.RemoteCatalogErrorCodeAccessor,
                ReadSuppressHomeNavigationForAutoSelection = input.ReadSuppressHomeNavigationForAutoSelection,
                SetSuppressHomeNavigationForAutoSelection = input.SetSuppressHomeNavigationForAutoSelection,
                ApplyStateUpdate = input.ApplyStateUpdate,
                SetCurrentView = input.SetCurrentView,
                RecomputeSelectionAfterScanAsync = input.RecomputeSelectionAfterScanAsync
            });
        var scanOrchestrator = input.ScanOrchestratorFactory.Create(
            new ScanOrchestratorFactoryInput
            {
                StringsAccessor = input.StringsAccessor,
                ScanFlowController = input.ScanFlowController,
                ScanLock = input.ScanLock,
                ScannedGameState = input.ScannedGameState,
                DialogPresenter = input.DialogPresenter,
                IsMultiGpuBlocked = input.IsMultiGpuBlocked,
                BuildScanRequest = input.BuildScanRequest,
                ScanResultCoordinator = scanResultCoordinator,
                ClearVisibleGameCards = input.ClearVisibleGameCards,
                LogWarning = input.LogScanWarning
            });

        return input.ShellSectionsFactory.Create(
            new ShellSectionsFactoryInput
            {
                Home = new HomeSectionFactoryInput
                {
                    StringsAccessor = input.StringsAccessor,
                    Games = games,
                    SelectGameAsync = input.SelectGameAsync,
                    ShowDetails = input.ShowDetails,
                    ShowInstallAsync = input.ShowInstallAsync,
                    CanSelectGame = input.CanSelectGame,
                    CanShowDetails = input.CanShowDetails,
                    CanShowInstall = input.CanShowInstall,
                    OnSelectGameException = input.OnSelectGameException,
                    OnShowInstallException = input.OnShowInstallException
                },
                Scan = new ScanSectionFactoryInput
                {
                    StringsAccessor = input.StringsAccessor,
                    DefaultFolders = defaultFolders,
                    AddedFolders = addedFolders,
                    ScanFolderListController = input.ScanFolderListController,
                    ScanFolderActionController = input.ScanFolderActionController,
                    ApplyScanFolderActionResult = input.ApplyScanFolderActionResult,
                    ScanOrchestrator = scanOrchestrator,
                    ShowHome = input.ShowHome,
                    AddedFolderStatusBrush = AddedFolderStatusBrush,
                    MissingFolderStatusBrush = MissingFolderStatusBrush,
                    OnScanCommandException = input.OnScanCommandException
                },
                SupportedGames = new SupportedGamesSectionFactoryInput
                {
                    SupportedGamesWikiMarkdownLoader = input.SupportedGamesWikiMarkdownLoader,
                    StartupBackgroundTaskManager = input.StartupBackgroundTaskManager,
                    AppLogger = input.AppLogger,
                    StringsAccessor = input.StringsAccessor,
                    SelectedLanguageAccessor = input.SelectedLanguageAccessor,
                    CurrentViewKindAccessor = input.CurrentViewKindAccessor,
                    OpenGameSupportRequestCommandAccessor = input.OpenGameSupportRequestCommandAccessor,
                    UpdateStartupPreparationState = input.UpdateStartupPreparationState
                },
                Settings = new SettingsSectionFactoryInput
                {
                    StringsAccessor = input.StringsAccessor,
                    DialogPresenter = input.DialogPresenter,
                    LocalDataPathProvider = input.LocalDataPathProvider,
                    AppLogger = input.AppLogger,
                    IsKoreanUi = input.IsKoreanUi,
                    SettingsLanguageOptions = input.SettingsLanguageOptions,
                    InitialSettingsLanguageOption = input.InitialSettingsLanguageOption,
                    ApplySettingsLanguageOption = input.ApplySettingsLanguageOption,
                    IsInstallExecutionInProgress = input.IsInstallExecutionInProgress,
                    OpenLogFolder = input.OpenLogFolder,
                    OpenSupportRequest = input.OpenSupportRequest,
                    OnRefreshInstallFilesException = input.OnRefreshInstallFilesException
                }
            });
    }

    private static IReadOnlyList<ScanFolderRowViewModel> LoadAddedScanFoldersFromManifest(
        ShellSectionsCompositionFactoryInput input,
        IReadOnlyCollection<ScanFolderRowViewModel> defaultFolders)
    {
        var result = input.ScanFolderActionController.LoadAddedFoldersFromManifest(
            defaultFolders,
            input.StringsAccessor(),
            AddedFolderStatusBrush,
            MissingFolderStatusBrush);

        return input.ApplyInitialScanFolderLoadResult(result);
    }
}

public sealed record ShellSectionsCompositionFactoryInput
{
    public required ShellSectionsFactory ShellSectionsFactory { get; init; }
    public required ScanResultCoordinatorFactory ScanResultCoordinatorFactory { get; init; }
    public required ScanOrchestratorFactory ScanOrchestratorFactory { get; init; }
    public required IShellMockDataProvider MockDataProvider { get; init; }
    public IScanFolderDiscoveryService? ScanFolderDiscoveryService { get; init; }
    public required ScanFlowController ScanFlowController { get; init; }
    public required ScanFolderListController ScanFolderListController { get; init; }
    public required ScanFolderActionController ScanFolderActionController { get; init; }
    public required SemaphoreSlim ScanLock { get; init; }
    public required ScannedGameState ScannedGameState { get; init; }
    public required DialogPresenter DialogPresenter { get; init; }
    public required FlowLogDispatcher FlowLogDispatcher { get; init; }
    public required MainViewModelResultApplier ResultApplier { get; init; }
    public required ISupportedGamesWikiMarkdownLoader SupportedGamesWikiMarkdownLoader { get; init; }
    public required StartupBackgroundTaskManager StartupBackgroundTaskManager { get; init; }
    public required IAppLogger AppLogger { get; init; }
    public required IAppLocalDataPathProvider LocalDataPathProvider { get; init; }
    public required ObservableCollection<string> SettingsLanguageOptions { get; init; }
    public required string InitialSettingsLanguageOption { get; init; }
    public required bool SeedMockGameCards { get; init; }
    public required bool SeedMockScanFolders { get; init; }
    public required Func<AppStrings> StringsAccessor { get; init; }
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
    public required Func<GameCardViewModel, CancellationToken, Task> SelectGameAsync { get; init; }
    public required Action ShowDetails { get; init; }
    public required Func<CancellationToken, Task> ShowInstallAsync { get; init; }
    public required Func<bool> CanSelectGame { get; init; }
    public required Func<bool> CanShowDetails { get; init; }
    public required Func<bool> CanShowInstall { get; init; }
    public Action<Exception>? OnSelectGameException { get; init; }
    public Action<Exception>? OnShowInstallException { get; init; }
    public required Action ShowHome { get; init; }
    public Action<Exception>? OnScanCommandException { get; init; }
    public required Func<AppLanguage> SelectedLanguageAccessor { get; init; }
    public required Func<ShellViewKind> CurrentViewKindAccessor { get; init; }
    public required Func<ICommand> OpenGameSupportRequestCommandAccessor { get; init; }
    public required Action<Func<StartupPreparationState, StartupPreparationState>> UpdateStartupPreparationState { get; init; }
    public required Func<bool> IsKoreanUi { get; init; }
    public required Action<string> ApplySettingsLanguageOption { get; init; }
    public required Func<bool> IsInstallExecutionInProgress { get; init; }
    public required Action OpenLogFolder { get; init; }
    public required Action OpenSupportRequest { get; init; }
    public Action<Exception>? OnRefreshInstallFilesException { get; init; }
    public string ScanLogCategory { get; init; } = "scan";
}
