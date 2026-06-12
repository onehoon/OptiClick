using System.Collections.ObjectModel;
using System.Windows.Input;

using OptiClick.Core.Runtime;
using OptiClick.Core.Scan;
using OptiClick.Wpf.Install.Archives;
using OptiClick.Wpf.Localization;
using OptiClick.Wpf.Logging;
using OptiClick.Wpf.Models;
using OptiClick.Wpf.Shell.Dialogs;
using OptiClick.Wpf.Shell.Flow;
using OptiClick.Wpf.Shell.Navigation;
using OptiClick.Wpf.Shell.Scan;
using OptiClick.Wpf.Shell.RuntimeData;
using OptiClick.Wpf.Shell.Startup;
using OptiClick.Wpf.ViewModels.Sections;
using OptiClick.Wpf.ViewModels.Sections.OptiScaler;
using OptiClick.Wpf.ViewModels.Sections.Scan;

namespace OptiClick.Wpf.ViewModels;

internal sealed record MainShellSectionsCompositionInput
{
    public required MainShellSectionsResolvedDependencies Dependencies { get; init; }
    public required bool SeedMockGameCards { get; init; }
    public required bool SeedMockScanFolders { get; init; }
    public required ObservableCollection<string> SettingsLanguageOptions { get; init; }
    public required ObservableCollection<OptiScalerVariantSelectionOption> OptiScalerVariantOptions { get; init; }
    public required Func<AppStrings> ReadStrings { get; init; }
    public required Func<GameCardViewModel?> ResolveSelectedGame { get; init; }
    public required Func<bool> IsInstallExecutionInProgress { get; init; }
    public required Func<bool> IsAppUpdateInProgress { get; init; }
    public required Func<bool> ShouldBlockStartupForUnsupportedOperatingSystem { get; init; }
    public required Func<GameCardViewModel, CancellationToken, Task> SelectGameAsync { get; init; }
    public required Action ShowDetails { get; init; }
    public required Func<CancellationToken, Task> ShowInstallAsync { get; init; }
    public required Action<Exception> LogSelectGameException { get; init; }
    public required Action<Exception> LogInstallCommandException { get; init; }
    public required ScanSectionCompositionInput Scan { get; init; }
    public required Func<AppLanguage> ReadSelectedLanguage { get; init; }
    public required Func<ShellViewKind> ReadCurrentViewKind { get; init; }
    public required Func<ICommand> ReadOpenGameSupportRequestCommand { get; init; }
    public required Action<Func<StartupPreparationState, StartupPreparationState>> UpdateStartupPreparationState { get; init; }
    public required MainOptiScalerSettingsController OptiScalerSettingsController { get; init; }
    public required Func<string> ReadLanguagePreference { get; init; }
    public required Action<string> SetOptiScalerVariantPreference { get; init; }
    public required string InitialSettingsLanguageOption { get; init; }
    public required Func<bool> IsKoreanUi { get; init; }
    public required Action<string> ApplySettingsLanguageOption { get; init; }
    public required Action OpenLogFolder { get; init; }
    public required Action OpenSupportRequest { get; init; }
    public required Action<Exception> LogRefreshInstallFilesException { get; init; }
}

internal static class MainShellSectionsComposition
{
    public static ShellSections Compose(MainShellSectionsCompositionInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var dependencies = input.Dependencies;
        return dependencies.ShellSectionsCompositionFactory.Create(
            new ShellSectionsCompositionFactoryInput
            {
                ShellSectionsFactory = dependencies.ShellSectionsFactory,
                MockDataProvider = dependencies.MockDataProvider,
                SeedMockGameCards = input.SeedMockGameCards,
                SeedMockScanFolders = input.SeedMockScanFolders,
                StringsAccessor = input.ReadStrings,
                Home = CreateHomeSectionCompositionInput(input),
                Scan = input.Scan,
                SupportedGames = CreateSupportedGamesSectionCompositionInput(input),
                OptiScaler = CreateOptiScalerSectionCompositionInput(input),
                Settings = CreateSettingsSectionCompositionInput(input)
            });
    }

    private static HomeSectionCompositionInput CreateHomeSectionCompositionInput(
        MainShellSectionsCompositionInput input)
    {
        return new HomeSectionCompositionInput
        {
            SelectGameAsync = input.SelectGameAsync,
            ShowDetails = input.ShowDetails,
            ShowInstallAsync = input.ShowInstallAsync,
            CanSelectGame = () => !input.IsInstallExecutionInProgress()
                                  && !input.IsAppUpdateInProgress(),
            CanShowDetails = () => input.ResolveSelectedGame() is not null,
            CanShowInstall = () => input.ResolveSelectedGame() is not null
                                  && !input.IsInstallExecutionInProgress()
                                  && !input.IsAppUpdateInProgress()
                                  && !input.ShouldBlockStartupForUnsupportedOperatingSystem(),
            OnSelectGameException = input.LogSelectGameException,
            OnShowInstallException = input.LogInstallCommandException
        };
    }

    private static SupportedGamesSectionCompositionInput CreateSupportedGamesSectionCompositionInput(
        MainShellSectionsCompositionInput input)
    {
        return new SupportedGamesSectionCompositionInput
        {
            SupportedGamesWikiMarkdownLoader = input.Dependencies.SupportedGamesWikiMarkdownLoader,
            StartupBackgroundTaskManager = input.Dependencies.StartupBackgroundTaskManager,
            AppLogger = input.Dependencies.AppLogger,
            SelectedLanguageAccessor = input.ReadSelectedLanguage,
            CurrentViewKindAccessor = input.ReadCurrentViewKind,
            OpenGameSupportRequestCommandAccessor = input.ReadOpenGameSupportRequestCommand,
            UpdateStartupPreparationState = input.UpdateStartupPreparationState
        };
    }

    private static OptiScalerSectionCompositionInput CreateOptiScalerSectionCompositionInput(
        MainShellSectionsCompositionInput input)
    {
        return new OptiScalerSectionCompositionInput
        {
            OptiScalerVariantOptions = input.OptiScalerVariantOptions,
            InitialOptiScalerVariantOption = OptiScalerVariantCatalogBuilder.StableVariant,
            InitialCommonIniSettings = input.OptiScalerSettingsController.LoadCommonIniSettings(),
            SaveHandler = new MainOptiScalerSectionSaveHandler(
                input.OptiScalerSettingsController,
                input.ReadLanguagePreference,
                input.SetOptiScalerVariantPreference)
        };
    }

    private static SettingsSectionCompositionInput CreateSettingsSectionCompositionInput(
        MainShellSectionsCompositionInput input)
    {
        return new SettingsSectionCompositionInput
        {
            DialogPresenter = input.Dependencies.DialogPresenter,
            LocalDataPathProvider = input.Dependencies.LocalDataPathProvider,
            AppLogger = input.Dependencies.AppLogger,
            SettingsLanguageOptions = input.SettingsLanguageOptions,
            InitialSettingsLanguageOption = input.InitialSettingsLanguageOption,
            IsKoreanUi = input.IsKoreanUi,
            ApplySettingsLanguageOption = input.ApplySettingsLanguageOption,
            IsInstallExecutionInProgress = input.IsInstallExecutionInProgress,
            OpenLogFolder = input.OpenLogFolder,
            OpenSupportRequest = input.OpenSupportRequest,
            OnRefreshInstallFilesException = input.LogRefreshInstallFilesException
        };
    }
}
