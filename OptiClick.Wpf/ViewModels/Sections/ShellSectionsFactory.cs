using OptiClick.Wpf.Shell.Navigation;
using OptiClick.Wpf.Shell.Settings;
using OptiClick.Wpf.ViewModels.Sections.Home;
using OptiClick.Wpf.ViewModels.Sections.OptiScaler;
using OptiClick.Wpf.ViewModels.Sections.Scan;
using OptiClick.Wpf.ViewModels.Sections.Settings;
using OptiClick.Wpf.ViewModels.Sections.SupportedGames;

namespace OptiClick.Wpf.ViewModels.Sections;

public sealed class ShellSectionsFactory
{
    public ShellSections Create(ShellSectionsFactoryInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(input.Home);
        ArgumentNullException.ThrowIfNull(input.Scan);
        ArgumentNullException.ThrowIfNull(input.SupportedGames);
        ArgumentNullException.ThrowIfNull(input.OptiScaler);
        ArgumentNullException.ThrowIfNull(input.Settings);

        return new ShellSections(
            CreateHome(input.Home),
            CreateScan(input.Scan),
            CreateSupportedGames(input.SupportedGames),
            CreateOptiScaler(input.OptiScaler),
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
                ScanOrchestrator = input.ScanOrchestrator,
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

    private static OptiScalerSectionViewModel CreateOptiScaler(OptiScalerSectionFactoryInput input)
    {
        return new OptiScalerSectionViewModel(
            new OptiScalerSectionViewModelOptions
            {
                StringsAccessor = input.StringsAccessor,
                OptiScalerVariantOptions = input.OptiScalerVariantOptions,
                InitialOptiScalerVariantOption = input.InitialOptiScalerVariantOption,
                InitialCommonIniSettings = input.InitialCommonIniSettings,
                SaveSettings = input.SaveSettings
            });
    }
}
