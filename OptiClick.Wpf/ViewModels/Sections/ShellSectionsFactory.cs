using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using OptiClick.Core.Runtime;
using OptiClick.Wpf.Localization;
using OptiClick.Wpf.Logging;
using OptiClick.Wpf.Models;
using OptiClick.Wpf.Services;
using OptiClick.Wpf.Shell.Dialogs;
using OptiClick.Wpf.Shell.Navigation;
using OptiClick.Wpf.Shell.RuntimeData;
using OptiClick.Wpf.Shell.Scan;
using OptiClick.Wpf.Shell.Settings;
using OptiClick.Wpf.Shell.Startup;
using OptiClick.Wpf.Shell.Wiki;
using OptiClick.Wpf.ViewModels.Sections.Home;
using OptiClick.Wpf.ViewModels.Sections.Scan;
using OptiClick.Wpf.ViewModels.Sections.Settings;
using OptiClick.Wpf.ViewModels.Sections.SupportedGames;

namespace OptiClick.Wpf.ViewModels.Sections;

public sealed class ShellSectionsFactory
{
    public ShellSections Create(ShellSectionsFactoryInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new ShellSections(
            CreateHome(input.Home),
            CreateScan(input.Scan),
            CreateSupportedGames(input.SupportedGames),
            CreateSettings(input.Settings));
    }

    private static HomeSectionViewModel CreateHome(HomeSectionFactoryInput input)
    {
        return new HomeSectionViewModel(
            new HomeSectionViewModelOptions
            {
                StringsAccessor = input.StringsAccessor,
                Games = input.Games,
                SelectedGameAction = new SelectedGameActionViewModel(),
                SelectGameAsync = input.SelectGameAsync,
                ShowDetails = input.ShowDetails,
                ShowInstallAsync = input.ShowInstallAsync,
                CanSelectGame = input.CanSelectGame,
                CanShowDetails = input.CanShowDetails,
                CanShowInstall = input.CanShowInstall,
                OnSelectGameException = input.OnSelectGameException,
                OnShowInstallException = input.OnShowInstallException
            });
    }

    private static ScanSectionViewModel CreateScan(ScanSectionFactoryInput input)
    {
        return new ScanSectionViewModel(
            new ScanSectionViewModelOptions
            {
                StringsAccessor = input.StringsAccessor,
                DefaultFolders = input.DefaultFolders,
                AddedFolders = input.AddedFolders,
                ScanFolderListController = input.ScanFolderListController,
                ScanFolderActionController = input.ScanFolderActionController,
                ApplyScanFolderActionResult = input.ApplyScanFolderActionResult,
                ScanFlowController = input.ScanFlowController,
                ScanLock = input.ScanLock,
                ScannedGameState = input.ScannedGameState,
                DialogPresenter = input.DialogPresenter,
                IsMultiGpuBlocked = input.IsMultiGpuBlocked,
                BuildScanRequest = input.BuildScanRequest,
                ApplyScanFlowResultAsync = input.ApplyScanFlowResultAsync,
                RunWithStartupAutoSelectionSuppressedAsync = input.RunWithStartupAutoSelectionSuppressedAsync,
                ApplyStartupNoGamesNavigation = input.ApplyStartupNoGamesNavigation,
                ShowStartupNoSupportedGamesGuidanceAsync = input.ShowStartupNoSupportedGamesGuidanceAsync,
                ClearVisibleGameCards = input.ClearVisibleGameCards,
                LogWarning = input.LogScanWarning,
                ShowHome = input.ShowHome,
                AddedFolderStatusBrush = input.AddedFolderStatusBrush,
                MissingFolderStatusBrush = input.MissingFolderStatusBrush,
                OnCommandException = input.OnScanCommandException
            });
    }

    private static SupportedGamesSectionViewModel CreateSupportedGames(SupportedGamesSectionFactoryInput input)
    {
        return new SupportedGamesSectionViewModel(
            input.SupportedGamesWikiMarkdownLoader,
            input.StartupBackgroundTaskManager,
            input.AppLogger,
            input.SelectedLanguageAccessor,
            input.StringsAccessor,
            () => input.CurrentViewKindAccessor() == ShellViewKind.SupportedGamesWiki,
            input.OpenGameSupportRequestCommandAccessor,
            input.UpdateStartupPreparationState);
    }

    private static SettingsSectionViewModel CreateSettings(SettingsSectionFactoryInput input)
    {
        var settingsActionCoordinator = new SettingsActionCoordinator(
            input.DialogPresenter,
            input.LocalDataPathProvider,
            input.AppLogger);

        return new SettingsSectionViewModel(
            new SettingsSectionViewModelOptions
            {
                StringsAccessor = input.StringsAccessor,
                IsKoreanUi = input.IsKoreanUi,
                SettingsLanguageOptions = input.SettingsLanguageOptions,
                InitialSettingsLanguageOption = input.InitialSettingsLanguageOption,
                ApplySettingsLanguageOption = input.ApplySettingsLanguageOption,
                SettingsActionCoordinator = settingsActionCoordinator,
                IsInstallExecutionInProgress = input.IsInstallExecutionInProgress,
                OpenLogFolderCommand = new RelayCommand(_ => input.OpenLogFolder()),
                OpenSupportRequestCommand = new RelayCommand(_ => input.OpenSupportRequest()),
                OnRefreshInstallFilesException = input.OnRefreshInstallFilesException
            });
    }
}

public sealed record ShellSections(
    HomeSectionViewModel Home,
    ScanSectionViewModel Scan,
    SupportedGamesSectionViewModel SupportedGames,
    SettingsSectionViewModel Settings);

public sealed record ShellSectionsFactoryInput
{
    public required HomeSectionFactoryInput Home { get; init; }
    public required ScanSectionFactoryInput Scan { get; init; }
    public required SupportedGamesSectionFactoryInput SupportedGames { get; init; }
    public required SettingsSectionFactoryInput Settings { get; init; }
}

public sealed record HomeSectionFactoryInput
{
    public required Func<AppStrings> StringsAccessor { get; init; }
    public required ObservableCollection<GameCardViewModel> Games { get; init; }
    public required Func<GameCardViewModel, CancellationToken, Task> SelectGameAsync { get; init; }
    public required Action ShowDetails { get; init; }
    public required Func<CancellationToken, Task> ShowInstallAsync { get; init; }
    public required Func<bool> CanSelectGame { get; init; }
    public required Func<bool> CanShowDetails { get; init; }
    public required Func<bool> CanShowInstall { get; init; }
    public Action<Exception>? OnSelectGameException { get; init; }
    public Action<Exception>? OnShowInstallException { get; init; }
}

public sealed record ScanSectionFactoryInput
{
    public required Func<AppStrings> StringsAccessor { get; init; }
    public required ObservableCollection<ScanFolderRowViewModel> DefaultFolders { get; init; }
    public required ObservableCollection<ScanFolderRowViewModel> AddedFolders { get; init; }
    public required ScanFolderListController ScanFolderListController { get; init; }
    public required ScanFolderActionController ScanFolderActionController { get; init; }
    public required Action<ScanFolderActionResult> ApplyScanFolderActionResult { get; init; }
    public required ScanFlowController ScanFlowController { get; init; }
    public required SemaphoreSlim ScanLock { get; init; }
    public required ScannedGameState ScannedGameState { get; init; }
    public required DialogPresenter DialogPresenter { get; init; }
    public required Func<bool> IsMultiGpuBlocked { get; init; }
    public required Func<IReadOnlyList<string>, ScanFlowRequest> BuildScanRequest { get; init; }
    public required Func<ScanFlowResult, CancellationToken, bool, Task> ApplyScanFlowResultAsync { get; init; }
    public required Func<Func<CancellationToken, Task>, CancellationToken, Task> RunWithStartupAutoSelectionSuppressedAsync { get; init; }
    public required Action<ScanFlowResult> ApplyStartupNoGamesNavigation { get; init; }
    public required Func<ScanFlowResult, CancellationToken, Task> ShowStartupNoSupportedGamesGuidanceAsync { get; init; }
    public required Action ClearVisibleGameCards { get; init; }
    public required Action<string> LogScanWarning { get; init; }
    public required Action ShowHome { get; init; }
    public required Brush AddedFolderStatusBrush { get; init; }
    public required Brush MissingFolderStatusBrush { get; init; }
    public Action<Exception>? OnScanCommandException { get; init; }
}

public sealed record SupportedGamesSectionFactoryInput
{
    public required ISupportedGamesWikiMarkdownLoader SupportedGamesWikiMarkdownLoader { get; init; }
    public required StartupBackgroundTaskManager StartupBackgroundTaskManager { get; init; }
    public required IAppLogger AppLogger { get; init; }
    public required Func<AppStrings> StringsAccessor { get; init; }
    public required Func<AppLanguage> SelectedLanguageAccessor { get; init; }
    public required Func<ShellViewKind> CurrentViewKindAccessor { get; init; }
    public required Func<ICommand> OpenGameSupportRequestCommandAccessor { get; init; }
    public required Action<Func<StartupPreparationState, StartupPreparationState>> UpdateStartupPreparationState { get; init; }
}

public sealed record SettingsSectionFactoryInput
{
    public required Func<AppStrings> StringsAccessor { get; init; }
    public required DialogPresenter DialogPresenter { get; init; }
    public required IAppLocalDataPathProvider LocalDataPathProvider { get; init; }
    public required IAppLogger AppLogger { get; init; }
    public required Func<bool> IsKoreanUi { get; init; }
    public required ObservableCollection<string> SettingsLanguageOptions { get; init; }
    public required string InitialSettingsLanguageOption { get; init; }
    public required Action<string> ApplySettingsLanguageOption { get; init; }
    public required Func<bool> IsInstallExecutionInProgress { get; init; }
    public required Action OpenLogFolder { get; init; }
    public required Action OpenSupportRequest { get; init; }
    public Action<Exception>? OnRefreshInstallFilesException { get; init; }
}
