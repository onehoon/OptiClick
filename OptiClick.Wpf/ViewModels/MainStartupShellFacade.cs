using OptiClick.Infrastructure.FileSystem;
using OptiClick.Wpf.Install.Archives;
using OptiClick.Wpf.Install.Flow;
using OptiClick.Wpf.Install.Planning;
using OptiClick.Wpf.Localization;
using OptiClick.Wpf.Logging;
using OptiClick.Wpf.Models;
using OptiClick.Wpf.Shell.Dialogs;
using OptiClick.Wpf.Shell.Runtime;
using OptiClick.Wpf.Shell.Startup;
using OptiClick.Wpf.ViewModels.Shell;

namespace OptiClick.Wpf.ViewModels;

internal sealed record MainStartupShellFacade
{
    public required MainStartupFlowContextFactory StartupFlowContextFactory { get; init; }
    public required StartupPreparationContextFactory StartupPreparationContextFactory { get; init; }

    public static MainStartupShellFacade Create(MainStartupShellFacadeInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var factories = MainStartupContextFactoryComposition.Compose(
            new MainStartupContextFactoryCompositionInput
            {
                StartupFlow = new MainStartupFlowContextFactoryInput
                {
                    ReadShouldBlockStartupForUnsupportedOperatingSystem =
                        input.ShouldBlockStartupForUnsupportedOperatingSystem,
                    ReadLocalDataRoot = () => input.LocalDataPathProvider.RootDirectory,
                    ReadArchiveCachePaths = () => ArchiveCachePaths.CreateDefault(input.LocalDataPathProvider),
                    UpdateStartupPreparationState = input.UpdateStartupPreparationState,
                    StartupInitializationErrorCode = "startup_initialization_failed",
                    ReadStartupInitializationWarningText = () => input.ReadStrings().RuntimeStartupInitWarning,
                    SetSettingsStatusText = input.SetSettingsStatusText,
                    ShowStartupBlockDialogAsync = ct =>
                        input.DialogPresenter.ShowSafelyAsync(
                            input.StartupNoticePresenter.BuildWindows10StartupBlockDialog(input.ReadStrings()),
                            ct),
                    RunInitialStartupAsync = input.StartupFlowCoordinator.RunInitialStartupAsync,
                    ShowPendingStartupNoticesAsync = input.ShowPendingStartupNoticesAsync,
                    ReadAppVersion = input.ReadAppVersion,
                    ReadLogDirectory = () => input.AppLogger.LogDirectory,
                    RefreshRuntimeContextAsync = input.RefreshRuntimeContextAsync,
                    RefreshRuntimeDataCatalogForStartupAsync =
                        input.RefreshRuntimeDataCatalogForStartupAsync,
                    WaitForStartupDialogsReadyAsync =
                        input.StartupPreparationCoordinator.WaitForStartupDialogsReadyAsync,
                    RunStartupAutoScanAsync = input.RunStartupAutoScanAsync,
                    RefreshDeviceIdentityRulesAsync = input.RefreshDeviceIdentityRulesAsync,
                    ApplyDeviceIdentityRulesFromCacheAsync = input.ApplyDeviceIdentityRulesFromCacheAsync,
                    StartDeviceIdentityRulesRefreshInBackground =
                        input.StartDeviceIdentityRulesRefreshInBackground,
                    StartStartupDialogsInBackground = input.StartStartupDialogsInBackground,
                    StartSupportedGamesWikiRefreshInBackground =
                        input.StartSupportedGamesWikiRefreshInBackground,
                    StartGameMasterCoverPrefetchInBackground =
                        input.StartGameMasterCoverPrefetchInBackground,
                    LogInfo = message => input.AppLogger.Info(MainViewModelLogCategories.App, message),
                    LogStartupInitializationError = ex =>
                        input.AppLogger.Error(
                            MainViewModelLogCategories.App,
                            "startup initialization failed",
                            ex),
                    ClearLastErrorCode = input.ClearLastErrorCode
                },
                StartupPreparation = new StartupPreparationContextFactoryInput
                {
                    ReadLatestArchiveReadiness = () => input.RuntimeShellState.LatestArchiveReadiness,
                    SetArchiveReadiness = readiness => input.RuntimeShellState.SetArchiveReadiness(readiness),
                    UpdateStartupPreparationState = input.UpdateStartupPreparationState,
                    ClearLastErrorCode = input.ClearLastErrorCode,
                    ApplyStartupPreparationOverlay =
                        visible => input.ReadStartupOverlay().ApplyStartupPreparationOverlay(visible),
                    ShowStartupPreparationFailureAsync =
                        (request, ct) => input.DialogPresenter.ShowSafelyAsync(request, ct),
                    ShouldBlockStartupForUnsupportedOperatingSystem =
                        input.ShouldBlockStartupForUnsupportedOperatingSystem,
                    ReadModuleDownloadLinks = () => input.RuntimeShellState.ModuleDownloadLinks,
                    ReadOptiScalerVariantCatalog = () => input.RuntimeShellState.LatestOptiScalerVariantCatalog,
                    ReadFsr4VariantCatalog = () => input.RuntimeShellState.LatestFsr4VariantCatalog,
                    RefreshArchiveReadinessWithoutCoordinatorAsync =
                        input.RefreshArchiveReadinessWithoutCoordinatorAsync,
                    RecomputeSelectionAfterScanAsync =
                        ct => input.RecomputeSelectionAfterScanAsync(ct, false),
                    LogAppInfo = message => input.AppLogger.Info(MainViewModelLogCategories.App, message),
                    LogAppWarning = message => input.AppLogger.Warning(MainViewModelLogCategories.App, message),
                    LogInstallInfo = message => input.AppLogger.Info(MainViewModelLogCategories.Install, message),
                    LogInstallWarning = message => input.AppLogger.Warning(MainViewModelLogCategories.Install, message),
                    ReadStartupPreparationFailedTitle = () => input.ReadStrings().StartupPreparationFailedTitle,
                    ReadStartupPreparationFailedSummary = () => input.ReadStrings().StartupPreparationFailedSummary,
                    ReadDialogButtonOkText = () => input.ReadStrings().DialogButtonOk
                }
            });

        return new MainStartupShellFacade
        {
            StartupFlowContextFactory = factories.StartupFlow,
            StartupPreparationContextFactory = factories.StartupPreparation
        };
    }
}

internal sealed record MainStartupShellFacadeInput
{
    public required Func<bool> ShouldBlockStartupForUnsupportedOperatingSystem { get; init; }
    public required IAppLocalDataPathProvider LocalDataPathProvider { get; init; }
    public required IAppLogger AppLogger { get; init; }
    public required Func<AppStrings> ReadStrings { get; init; }
    public required RuntimeShellState RuntimeShellState { get; init; }
    public required Func<StartupOverlayViewModel> ReadStartupOverlay { get; init; }
    public required DialogPresenter DialogPresenter { get; init; }
    public required StartupNoticePresenter StartupNoticePresenter { get; init; }
    public required StartupFlowCoordinator StartupFlowCoordinator { get; init; }
    public required StartupPreparationCoordinator StartupPreparationCoordinator { get; init; }
    public required Action<Func<StartupPreparationState, StartupPreparationState>> UpdateStartupPreparationState { get; init; }
    public required Func<string, string, string> ClearLastErrorCode { get; init; }
    public required Action<string> SetSettingsStatusText { get; init; }
    public required Func<CancellationToken, Task> ShowPendingStartupNoticesAsync { get; init; }
    public required Func<string> ReadAppVersion { get; init; }
    public required Func<CancellationToken, Task> RefreshRuntimeContextAsync { get; init; }
    public required Func<CancellationToken, Task> RefreshRuntimeDataCatalogForStartupAsync { get; init; }
    public required Func<CancellationToken, Task> RunStartupAutoScanAsync { get; init; }
    public required Func<CancellationToken, Task> RefreshDeviceIdentityRulesAsync { get; init; }
    public required Func<CancellationToken, Task> ApplyDeviceIdentityRulesFromCacheAsync { get; init; }
    public required Action StartDeviceIdentityRulesRefreshInBackground { get; init; }
    public required Action StartStartupDialogsInBackground { get; init; }
    public required Action StartSupportedGamesWikiRefreshInBackground { get; init; }
    public required Action StartGameMasterCoverPrefetchInBackground { get; init; }
    public required Func<CancellationToken, Task<ArchiveReadinessFlowResult>> RefreshArchiveReadinessWithoutCoordinatorAsync { get; init; }
    public required Func<CancellationToken, bool, Task> RecomputeSelectionAfterScanAsync { get; init; }
}
