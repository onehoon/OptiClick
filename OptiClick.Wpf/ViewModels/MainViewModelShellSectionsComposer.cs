using System.Collections.ObjectModel;

using OptiClick.Wpf.Install.Archives;
using OptiClick.Wpf.ViewModels.Sections;
using OptiClick.Wpf.ViewModels.Sections.OptiScaler;
using OptiClick.Wpf.ViewModels.Sections.Scan;

namespace OptiClick.Wpf.ViewModels;

internal sealed record MainViewModelShellSectionsCompositionInput
{
    public required MainShellSectionsResolvedDependencies Dependencies { get; init; }
    public required MainShellFacadePorts Ports { get; init; }
    public required MainScanShellFacade ScanShellFacade { get; init; }
    public required MainOptiScalerSettingsController OptiScalerSettingsController { get; init; }
    public required bool SeedMockGameCards { get; init; }
    public required bool SeedMockScanFolders { get; init; }
    public required IReadOnlyList<string> SettingsLanguageOptions { get; init; }
    public required string InitialSettingsLanguageOption { get; init; }
}

internal static class MainViewModelShellSectionsComposer
{
    public static ShellSections Compose(MainViewModelShellSectionsCompositionInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var ports = input.Ports;
        var scanSectionComposition = input.ScanShellFacade.CreateSectionCompositionInput(
            new MainScanSectionCompositionContext
            {
                ScanLock = ports.App.OperationLocks.ScanLock,
                ScannedGameState = ports.Runtime.ScannedGameState,
                ReadRemoteCatalogErrorCode = () => ports.Runtime.RuntimeShellState.LatestRemoteCatalogErrorCode,
                ReadSuppressHomeNavigationForAutoSelection =
                    ports.Selection.ReadSuppressHomeNavigationForAutoSelection,
                SetSuppressHomeNavigationForAutoSelection =
                    value => ports.Selection.SetSuppressHomeNavigationForAutoSelection(value),
                ApplyStateUpdate = ports.App.ApplyStateUpdate,
                ApplyDeferredStateUpdate = ports.App.ApplyDeferredStateUpdate,
                SetCurrentView = ports.Ui.SetCurrentView,
                RecomputeSelectionAfterScanAsync = ports.Selection.RecomputeSelectionAfterScanAsync,
                IsMultiGpuBlocked = ports.Runtime.IsMultiGpuBlocked,
                BuildScanRequest = ports.Selection.BuildScanRequest,
                ClearVisibleGameCards = () => ports.Selection.ReplaceGameCards([], true),
                LogScanWarning = message => ports.App.AppLogger.Warning(MainViewModelLogCategories.Scan, message),
                LogScanCommandException = ex =>
                    ports.App.AppLogger.Error(MainViewModelLogCategories.Command, "save and scan command failed", ex)
            });

        return MainShellSectionsComposition.Compose(
            new MainShellSectionsCompositionInput
            {
                Dependencies = input.Dependencies,
                SeedMockGameCards = input.SeedMockGameCards,
                SeedMockScanFolders = input.SeedMockScanFolders,
                SettingsLanguageOptions = CreateSettingsLanguageOptions(input.SettingsLanguageOptions),
                OptiScalerVariantOptions = new ObservableCollection<OptiScalerVariantSelectionOption>(),
                ReadStrings = ports.App.ReadStrings,
                ResolveSelectedGame = ports.Selection.ReadSelectedGame,
                IsInstallExecutionInProgress = ports.Selection.IsInstallExecutionInProgress,
                IsAppUpdateInProgress = ports.Selection.IsAppUpdateInProgress,
                ShouldBlockStartupForUnsupportedOperatingSystem =
                    ports.App.ShouldBlockStartupForUnsupportedOperatingSystem,
                SelectGameAsync = (game, cancellationToken) =>
                    ports.Selection.SelectGameAsync(game, cancellationToken, true, true),
                ShowInstallAsync = ports.Install.ShowInstallAsync,
                LogSelectGameException = ex =>
                    ports.App.AppLogger.Error(MainViewModelLogCategories.Command, "select game command failed", ex),
                LogInstallCommandException = ex =>
                    ports.App.AppLogger.Error(MainViewModelLogCategories.Command, "install command failed", ex),
                Scan = scanSectionComposition,
                ReadSelectedLanguage = ports.Localization.ReadSelectedLanguage,
                ReadCurrentViewKind = ports.Ui.ReadCurrentViewKind,
                ReadOpenGameSupportRequestCommand = ports.Ui.ReadOpenGameSupportRequestCommand,
                UpdateStartupPreparationState = ports.Startup.UpdateStartupPreparationState,
                OptiScalerSettingsController = input.OptiScalerSettingsController,
                ReadLanguagePreference = ports.Localization.ReadLanguagePreference,
                SetOptiScalerVariantPreference = ports.Install.SetOptiScalerVariantPreference,
                RefreshVisibleGamesAfterOptiScalerPreferenceChange =
                    ports.Selection.RefreshVisibleGamesFromScanMatchesWithoutAutoSelection,
                ReadGpuBundleKey = () => ports.Runtime.RuntimeShellState.LatestGpuBundleKey,
                InitialSettingsLanguageOption = input.InitialSettingsLanguageOption,
                IsKoreanUi = ports.App.IsKoreanUi,
                ApplySettingsLanguageOption = ports.Localization.ApplySettingsLanguageOption,
                OpenLogFolder = ports.Ui.OpenLogFolder,
                OpenSupportRequest = ports.Ui.OpenSupportRequest,
                LogRefreshInstallFilesException = ex =>
                    ports.App.AppLogger.Error(MainViewModelLogCategories.Command, "reset app cache command failed", ex)
            });
    }

    private static ObservableCollection<string> CreateSettingsLanguageOptions(IReadOnlyList<string> options)
    {
        return new ObservableCollection<string>(options);
    }
}
