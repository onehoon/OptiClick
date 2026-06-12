using System.Collections.ObjectModel;
using System.Windows.Input;
using OptiClick.Core.Scan;
using OptiClick.Core.Models;
using OptiClick.Core.Runtime;
using OptiClick.Wpf.Install.Archives;
using OptiClick.Wpf.Install.Flow;
using OptiClick.Wpf.Install.UiState;
using OptiClick.Wpf.Localization;
using OptiClick.Wpf.Logging;
using OptiClick.Wpf.Models;
using OptiClick.Wpf.Services;
using OptiClick.Wpf.Shell.Dialogs;
using OptiClick.Wpf.Shell.Flow;
using OptiClick.Wpf.Shell.Games;
using OptiClick.Wpf.Shell.Games.GpuBundle;
using OptiClick.Wpf.Shell.Gpu;
using OptiClick.Wpf.Shell.Localization;
using OptiClick.Wpf.Shell.Navigation;
using OptiClick.Wpf.Shell.Runtime;
using OptiClick.Wpf.Shell.RuntimeData;
using OptiClick.Wpf.Shell.Scan;
using OptiClick.Wpf.Shell.Selection;
using OptiClick.Wpf.Shell.Startup;
using OptiClick.Wpf.ViewModels.Shell;

namespace OptiClick.Wpf.ViewModels;

internal static class MainViewModelShellFacadeComposer
{
    public static MainStartupShellFacade ComposeStartup(MainStartupShellFacadeCompositionInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        var ports = input.Ports;

        return MainStartupShellFacade.Create(
            new MainStartupShellFacadeInput
            {
                ShouldBlockStartupForUnsupportedOperatingSystem =
                    ports.App.ShouldBlockStartupForUnsupportedOperatingSystem,
                LocalDataPathProvider = ports.App.LocalDataPathProvider,
                AppLogger = ports.App.AppLogger,
                ReadStrings = ports.App.ReadStrings,
                RuntimeShellState = ports.Runtime.RuntimeShellState,
                ReadStartupOverlay = ports.Ui.ReadStartupOverlay,
                DialogPresenter = ports.App.DialogPresenter,
                StartupNoticePresenter = input.StartupDependencies.StartupNoticePresenter,
                StartupFlowCoordinator = input.StartupDependencies.StartupFlowCoordinator,
                StartupPreparationCoordinator = input.StartupDependencies.StartupPreparationCoordinator,
                UpdateStartupPreparationState = ports.Startup.UpdateStartupPreparationState,
                ClearLastErrorCode = ports.Startup.ClearLastErrorCode,
                SetSettingsStatusText = ports.App.SetSettingsStatusText,
                ShowPendingStartupNoticesAsync = ports.Startup.ShowPendingStartupNoticesAsync,
                ReadAppVersion = ports.App.ReadAppVersion,
                RefreshRuntimeContextAsync = ports.Runtime.RefreshRuntimeContextAsync,
                RefreshRuntimeDataCatalogForStartupAsync =
                    ports.Runtime.RefreshRuntimeDataCatalogForStartupAsync,
                RunStartupAutoScanAsync = ports.Startup.RunStartupAutoScanAsync,
                RefreshDeviceIdentityRulesAsync = ports.Runtime.RefreshDeviceIdentityRulesAsync,
                ApplyDeviceIdentityRulesFromCacheAsync = ports.Runtime.ApplyDeviceIdentityRulesFromCacheAsync,
                StartDeviceIdentityRulesRefreshInBackground =
                    ports.Runtime.StartDeviceIdentityRulesRefreshInBackground,
                StartStartupDialogsInBackground = ports.Startup.StartStartupDialogsInBackground,
                StartSupportedGamesWikiRefreshInBackground =
                    ports.Ui.StartSupportedGamesWikiRefreshInBackground,
                StartGameMasterCoverPrefetchInBackground =
                    ports.Startup.StartGameMasterCoverPrefetchInBackground,
                RefreshArchiveReadinessWithoutCoordinatorAsync =
                    ports.Install.RefreshArchiveReadinessWithoutCoordinatorAsync,
                RecomputeSelectionAfterScanAsync = ports.Selection.RecomputeSelectionAfterScanAsync
            });
    }

    public static MainRuntimeShellFacade ComposeRuntime(MainRuntimeShellFacadeCompositionInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        var ports = input.Ports;

        return MainRuntimeShellFacade.Create(
            new MainRuntimeShellFacadeInput
            {
                RuntimeFlowDependencies = input.RuntimeFlowDependencies,
                OperationLocks = ports.App.OperationLocks,
                State = new MainRuntimeShellStatePort
                {
                    RuntimeShellState = ports.Runtime.RuntimeShellState,
                    ScannedGameState = ports.Runtime.ScannedGameState,
                    ReadSelectionState = ports.Selection.ReadSelectionState,
                    ApplySelectionState = ports.Selection.ApplySelectionStateFromRuntimeCatalog,
                    ReadVisibleGameCount = ports.Selection.ReadVisibleGameCount,
                    HasSupportedGamesEntries = ports.Ui.HasSupportedGamesEntries,
                    SetSettingsStatusText = ports.App.SetSettingsStatusText,
                    SetScanStatusText = ports.App.SetScanStatusText,
                    ApplyStateUpdate = ports.App.ApplyStateUpdate,
                    BuildRuntimeSummaryStateUpdate = ports.Runtime.BuildLatestRuntimeSummaryStateUpdate,
                    ApplyRuntimeSummaryStateUpdate = ports.Runtime.ApplyRuntimeSummaryStateUpdate
                },
                Interaction = new MainRuntimeShellInteractionPort
                {
                    FlowLogDispatcher = ports.App.FlowLogDispatcher,
                    ResultApplier = ports.App.ResultApplier,
                    RemoteCatalogDialogGate = ports.App.RemoteCatalogDialogGate,
                    DialogPresenter = ports.App.DialogPresenter,
                    AppLogger = ports.App.AppLogger,
                    ReadStrings = ports.App.ReadStrings,
                    IsKoreanUi = ports.App.IsKoreanUi,
                    ReadAppVersion = ports.App.ReadAppVersion,
                    ShowRemoteCatalogDialogOnceAsync = ports.App.ShowRemoteCatalogDialogOnceAsync
                },
                Catalog = new MainRuntimeCatalogServicesPort
                {
                    GpuBundleManifestClient = input.RuntimeDependencies.GpuBundleManifestClient,
                    GpuBundleManifestRuleResolver = input.RuntimeDependencies.GpuBundleManifestRuleResolver
                },
                CrossFeature = new MainRuntimeCrossFeaturePort
                {
                    RefreshVisibleGamesFromScanMatches = ports.Selection.RefreshVisibleGamesFromScanMatches,
                    RebuildSupportedGamesRows = ports.Ui.RebuildSupportedGamesRows,
                    StartStartupPreparationAsync = ports.Startup.StartStartupPreparationAsync,
                    RefreshArchiveReadinessAsync = ports.Install.RefreshArchiveReadinessAsync,
                    ReplaceGameCards = ports.Selection.ReplaceGameCards,
                    SetSelectedGame = ports.Selection.SetSelectedGame,
                    ResolveManifestSupportedGpuCandidatesAsync =
                        ports.Runtime.ResolveManifestSupportedGpuCandidatesAsync,
                    ApplyMultiGpuBlockedUiState = ports.Runtime.ApplyMultiGpuBlockedUiState,
                    RefreshRuntimeCatalogAsync = ports.Runtime.RefreshRuntimeCatalogAsync
                }
            });
    }

    public static MainSelectionShellFacade ComposeSelection(MainSelectionShellFacadeCompositionInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        var ports = input.Ports;

        return MainSelectionShellFacade.Create(
            new MainSelectionShellFacadeInput
            {
                SelectionDependencies = input.SelectionDependencies,
                SelectionScanDependencies = input.SelectionScanDependencies,
                FlowRequestFactory = ports.App.FlowRequestFactory,
                RuntimeShellState = ports.Runtime.RuntimeShellState,
                ScannedGameState = ports.Runtime.ScannedGameState,
                FlowLogDispatcher = ports.App.FlowLogDispatcher,
                AppLogger = ports.App.AppLogger,
                ReadStrings = ports.App.ReadStrings,
                ReadSelectedLanguage = ports.Localization.ReadSelectedLanguage,
                SetLanguage = ports.Localization.SetLanguage,
                RefreshLocalizedStrings = ports.Localization.RefreshLocalizedStrings,
                ApplySelectedGameLocalization = ports.Ui.ApplySelectedGameLocalization,
                RefreshSupportedGamesAfterLanguageChange =
                    ports.Ui.RefreshSupportedGamesAfterLanguageChange,
                BuildRefreshState = ports.Localization.BuildRefreshState,
                RefreshRuntimeContextAsync = ports.Runtime.RefreshRuntimeContextAsync,
                RecomputeSelectionAfterScanAsync = ports.Selection.RecomputeSelectionAfterScanAsync,
                RefreshVisibleGamesAfterLanguageChangeAsync =
                    ports.Selection.RefreshVisibleGamesAfterLanguageChangeAsync,
                LogLanguageChangeInfo = message =>
                    ports.App.AppLogger.Info(MainViewModelLogCategories.I18n, message),
                LogLanguageChangeWarning = (message, ex) =>
                    ports.App.AppLogger.Warning(MainViewModelLogCategories.I18n, message),
                ApplyLocalizationStateUpdate = ports.Localization.ApplyLocalizationStateUpdate,
                ReadVisibleCards = ports.Selection.ReadVisibleCards,
                ReadSelectedGame = ports.Selection.ReadSelectedGame,
                SetSelectedGame = ports.Selection.SetSelectedGame,
                IsInstallExecutionInProgress = ports.Selection.IsInstallExecutionInProgress,
                IsAppUpdateInProgress = ports.Selection.IsAppUpdateInProgress,
                ReadSuppressHomeNavigationForAutoSelection =
                    ports.Selection.ReadSuppressHomeNavigationForAutoSelection,
                IncrementSelectionVersion = ports.Selection.IncrementSelectionVersion,
                ReadSelectionState = ports.Selection.ReadSelectionState,
                ReadSelectionVersion = ports.Selection.ReadSelectionVersion,
                ApplySelectionState = ports.Selection.ApplySelectionState,
                ApplySelectionBridgeState = ports.Selection.ApplySelectionBridgeState,
                ApplyPrecheckRunningIntermediate = ports.Selection.ApplyPrecheckRunningIntermediate,
                SetCurrentView = ports.Ui.SetCurrentView,
                QueueHomeCoverPrefetchInBackground = ports.Startup.QueueHomeCoverPrefetchInBackground,
                SelectGameAsync = ports.Selection.SelectGameAsync
            });
    }

    public static MainScanShellFacade ComposeScan(MainScanShellFacadeCompositionInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        var ports = input.Ports;

        return MainScanShellFacade.Create(
            new MainScanShellFacadeInput
            {
                ScanDependencies = input.ScanDependencies,
                DialogPresenter = ports.App.DialogPresenter,
                FlowLogDispatcher = ports.App.FlowLogDispatcher,
                ResultApplier = ports.App.ResultApplier
            });
    }

    public static MainInstallShellFacade ComposeInstall(MainInstallShellFacadeCompositionInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        var ports = input.Ports;

        return MainInstallShellFacade.Create(
            new MainInstallShellFacadeInput
            {
                InstallDependencies = input.InstallDependencies,
                OperationLocks = ports.App.OperationLocks,
                RuntimeShellState = ports.Runtime.RuntimeShellState,
                ScannedGameState = ports.Runtime.ScannedGameState,
                FlowRequestFactory = ports.App.FlowRequestFactory,
                FlowLogDispatcher = ports.App.FlowLogDispatcher,
                ResultApplier = ports.App.ResultApplier,
                DialogPresenter = ports.App.DialogPresenter,
                StartupNoticePresenter = input.StartupDependencies.StartupNoticePresenter,
                InstallManagementDialogService = ports.App.InstallManagementDialogService,
                AppLogger = ports.App.AppLogger,
                ArchiveReadinessRefreshCoordinator =
                    input.StartupDependencies.ArchiveReadinessRefreshCoordinator,
                ReadStrings = ports.App.ReadStrings,
                ShouldBlockStartupForUnsupportedOperatingSystem =
                    ports.App.ShouldBlockStartupForUnsupportedOperatingSystem,
                IsAppUpdateInProgress = ports.Selection.IsAppUpdateInProgress,
                IsInstallExecutionInProgress = ports.Selection.IsInstallExecutionInProgress,
                ResolveSelectedGame = ports.Selection.ReadSelectedGame,
                ReadSelectionState = ports.Selection.ReadSelectionState,
                ReadInstallButtonText = ports.Install.ReadInstallButtonText,
                SetSettingsStatusText = ports.App.SetSettingsStatusText,
                ApplyInstallBusyState = ports.Install.ApplyInstallBusyState,
                TryRefreshVisibleCard = ports.Selection.TryRefreshVisibleCard,
                SelectGameAsync = ports.Selection.SelectGameAsync,
                RefreshSelectionForInstallAsync = ports.Selection.RefreshSelectionForInstallAsync,
                HandleUninstallAsync = ports.Install.HandleUninstallAsync,
                ExecuteCurrentInstallFlowAsync = ports.Install.ExecuteCurrentInstallFlowAsync,
                ApplyStateUpdate = ports.App.ApplyStateUpdate,
                ApplyDeferredStateUpdate = ports.App.ApplyDeferredStateUpdate,
                ClearSelectedGameContext = ports.Install.ClearSelectedGameContext,
                ReadPreferredOptiScalerVariant = ports.Install.ReadPreferredOptiScalerVariant,
                ApplyOptiScalerVariantOptions = ports.Install.ApplyOptiScalerVariantOptions,
                PersistEffectiveVariantPreference = ports.Install.PersistEffectiveVariantPreference,
                SaveUserSettings = ports.Install.SaveUserSettings,
                IsOperatingSystemSupported = ports.Install.IsOperatingSystemSupported,
                RefreshArchiveReadinessAsync = ports.Install.RefreshArchiveReadinessAsync,
                ResolveSelectedIndex = ports.Selection.ResolveSelectedIndex
            });
    }
}

internal sealed record MainShellFacadePorts
{
    public required MainShellFacadeAppPort App { get; init; }
    public required MainShellFacadeRuntimePort Runtime { get; init; }
    public required MainShellFacadeStartupPort Startup { get; init; }
    public required MainShellFacadeSelectionPort Selection { get; init; }
    public required MainShellFacadeInstallPort Install { get; init; }
    public required MainShellFacadeUiPort Ui { get; init; }
    public required MainShellFacadeLocalizationPort Localization { get; init; }
}

internal sealed record MainShellFacadeAppPort
{
    public required IAppLocalDataPathProvider LocalDataPathProvider { get; init; }
    public required IAppLogger AppLogger { get; init; }
    public required Func<AppStrings> ReadStrings { get; init; }
    public required DialogPresenter DialogPresenter { get; init; }
    public required FlowLogDispatcher FlowLogDispatcher { get; init; }
    public required MainViewModelResultApplier ResultApplier { get; init; }
    public required OnceDialogGate RemoteCatalogDialogGate { get; init; }
    public required MainViewModelFlowRequestFactory FlowRequestFactory { get; init; }
    public required MainShellOperationLocks OperationLocks { get; init; }
    public required IInstallManagementDialogService InstallManagementDialogService { get; init; }
    public required Func<string> ReadAppVersion { get; init; }
    public required Func<bool> IsKoreanUi { get; init; }
    public required Action<string> SetSettingsStatusText { get; init; }
    public required Action<string> SetScanStatusText { get; init; }
    public required Action<MainViewModelStateUpdate> ApplyStateUpdate { get; init; }
    public required Action<MainViewModelStateUpdate> ApplyDeferredStateUpdate { get; init; }
    public required Func<bool> ShouldBlockStartupForUnsupportedOperatingSystem { get; init; }
    public required Func<AppDialogRequest, CancellationToken, Task> ShowRemoteCatalogDialogOnceAsync { get; init; }
}

internal sealed record MainShellFacadeRuntimePort
{
    public required RuntimeShellState RuntimeShellState { get; init; }
    public required ScannedGameState ScannedGameState { get; init; }
    public required Func<CancellationToken, Task> RefreshRuntimeContextAsync { get; init; }
    public required Func<CancellationToken, Task> RefreshRuntimeDataCatalogForStartupAsync { get; init; }
    public required Func<CancellationToken, Task> RefreshDeviceIdentityRulesAsync { get; init; }
    public required Func<CancellationToken, Task> ApplyDeviceIdentityRulesFromCacheAsync { get; init; }
    public required Action StartDeviceIdentityRulesRefreshInBackground { get; init; }
    public required Func<RuntimeSummaryStateUpdate> BuildLatestRuntimeSummaryStateUpdate { get; init; }
    public required Action<RuntimeSummaryStateUpdate> ApplyRuntimeSummaryStateUpdate { get; init; }
    public required Func<bool> IsMultiGpuBlocked { get; init; }
    public required Func<RuntimeContext, IReadOnlyList<GpuInfo>, CancellationToken, Task<IReadOnlyList<GpuInfo>>>
        ResolveManifestSupportedGpuCandidatesAsync
    {
        get;
        init;
    }

    public required Action ApplyMultiGpuBlockedUiState { get; init; }
    public required Func<RuntimeCatalogRefreshMode, CancellationToken, Task> RefreshRuntimeCatalogAsync { get; init; }
}

internal sealed record MainShellFacadeStartupPort
{
    public required Action<Func<StartupPreparationState, StartupPreparationState>> UpdateStartupPreparationState { get; init; }
    public required Func<string, string, string> ClearLastErrorCode { get; init; }
    public required Func<CancellationToken, Task> ShowPendingStartupNoticesAsync { get; init; }
    public required Func<CancellationToken, Task> RunStartupAutoScanAsync { get; init; }
    public required Action StartStartupDialogsInBackground { get; init; }
    public required Action StartGameMasterCoverPrefetchInBackground { get; init; }
    public required Action<string> QueueHomeCoverPrefetchInBackground { get; init; }
    public required Func<CancellationToken, Task> StartStartupPreparationAsync { get; init; }
}

internal sealed record MainShellFacadeSelectionPort
{
    public required Func<ObservableCollection<GameCardViewModel>> ReadVisibleCards { get; init; }
    public required Func<int> ReadVisibleGameCount { get; init; }
    public required Func<GameCardViewModel?> ReadSelectedGame { get; init; }
    public required Action<GameCardViewModel?> SetSelectedGame { get; init; }
    public required Func<ShellInstallSelectionState> ReadSelectionState { get; init; }
    public required Action<ShellInstallSelectionState> ApplySelectionState { get; init; }
    public required Action<ShellInstallSelectionState> ApplySelectionStateFromRuntimeCatalog { get; init; }
    public required Action<ShellInstallSelectionState> ApplySelectionBridgeState { get; init; }
    public required Action ApplyPrecheckRunningIntermediate { get; init; }
    public required Func<bool> IsInstallExecutionInProgress { get; init; }
    public required Func<bool> IsAppUpdateInProgress { get; init; }
    public required Func<bool> ReadSuppressHomeNavigationForAutoSelection { get; init; }
    public required Action<bool> SetSuppressHomeNavigationForAutoSelection { get; init; }
    public required Func<long> IncrementSelectionVersion { get; init; }
    public required Func<long> ReadSelectionVersion { get; init; }
    public required Func<CancellationToken, bool, Task> RecomputeSelectionAfterScanAsync { get; init; }
    public required Func<CancellationToken, Task<bool>> RefreshVisibleGamesAfterLanguageChangeAsync { get; init; }
    public required Func<IReadOnlyList<string>, ScanFlowRequest> BuildScanRequest { get; init; }
    public required Action RefreshVisibleGamesFromScanMatches { get; init; }
    public required Action<IReadOnlyList<GameCardViewModel>, bool> ReplaceGameCards { get; init; }
    public required Func<string, GameCardViewModel?> TryRefreshVisibleCard { get; init; }
    public required Func<GameCardViewModel?, CancellationToken, bool, bool, Task> SelectGameAsync { get; init; }
    public required Func<GameCardViewModel, CancellationToken, bool, bool, Task> RefreshSelectionForInstallAsync { get; init; }
    public required Func<GameCardViewModel, int> ResolveSelectedIndex { get; init; }
}

internal sealed record MainShellFacadeInstallPort
{
    public required Func<string> ReadInstallButtonText { get; init; }
    public required Action<bool, string, ShellInstallSelectionState?> ApplyInstallBusyState { get; init; }
    public required Func<CancellationToken, Task> ShowInstallAsync { get; init; }
    public required Func<GameCardViewModel, CancellationToken, Task> HandleUninstallAsync { get; init; }
    public required Func<CancellationToken, Task> ExecuteCurrentInstallFlowAsync { get; init; }
    public required Action ClearSelectedGameContext { get; init; }
    public required Func<string> ReadPreferredOptiScalerVariant { get; init; }
    public required Action ApplyOptiScalerVariantOptions { get; init; }
    public required Action<string> PersistEffectiveVariantPreference { get; init; }
    public required Action<string> SetOptiScalerVariantPreference { get; init; }
    public required Action SaveUserSettings { get; init; }
    public required Func<bool> IsOperatingSystemSupported { get; init; }
    public required Func<CancellationToken, Task<ArchiveReadinessFlowResult>> RefreshArchiveReadinessAsync { get; init; }
    public required Func<CancellationToken, Task<ArchiveReadinessFlowResult>> RefreshArchiveReadinessWithoutCoordinatorAsync { get; init; }
}

internal sealed record MainShellFacadeUiPort
{
    public required Func<StartupOverlayViewModel> ReadStartupOverlay { get; init; }
    public required Func<ShellViewKind> ReadCurrentViewKind { get; init; }
    public required Action<ShellViewKind> SetCurrentView { get; init; }
    public required Func<ICommand> ReadOpenGameSupportRequestCommand { get; init; }
    public required Func<bool> HasSupportedGamesEntries { get; init; }
    public required Action RebuildSupportedGamesRows { get; init; }
    public required Action RefreshSupportedGamesAfterLanguageChange { get; init; }
    public required Action ApplySelectedGameLocalization { get; init; }
    public required Action StartSupportedGamesWikiRefreshInBackground { get; init; }
    public required Action ShowDetails { get; init; }
    public required Action OpenLogFolder { get; init; }
    public required Action OpenSupportRequest { get; init; }
}

internal sealed record MainShellFacadeLocalizationPort
{
    public required Func<AppLanguage> ReadSelectedLanguage { get; init; }
    public required Func<string> ReadLanguagePreference { get; init; }
    public required Action<AppLanguage> SetLanguage { get; init; }
    public required Action RefreshLocalizedStrings { get; init; }
    public required Func<AppLanguage, AppStrings, LocalizationStateUpdate> BuildRefreshState { get; init; }
    public required Action<LocalizationStateUpdate> ApplyLocalizationStateUpdate { get; init; }
    public required Action<string> ApplySettingsLanguageOption { get; init; }
}

internal sealed record MainStartupShellFacadeCompositionInput
{
    public required MainStartupResolvedDependencies StartupDependencies { get; init; }
    public required MainShellFacadePorts Ports { get; init; }
}

internal sealed record MainRuntimeShellFacadeCompositionInput
{
    public required MainRuntimeResolvedDependencies RuntimeDependencies { get; init; }
    public required MainRuntimeFlowResolvedDependencies RuntimeFlowDependencies { get; init; }
    public required MainShellFacadePorts Ports { get; init; }
}

internal sealed record MainSelectionShellFacadeCompositionInput
{
    public required MainSelectionResolvedDependencies SelectionDependencies { get; init; }
    public required MainSelectionScanResolvedDependencies SelectionScanDependencies { get; init; }
    public required MainShellFacadePorts Ports { get; init; }
}

internal sealed record MainScanShellFacadeCompositionInput
{
    public required MainScanResolvedDependencies ScanDependencies { get; init; }
    public required MainShellFacadePorts Ports { get; init; }
}

internal sealed record MainInstallShellFacadeCompositionInput
{
    public required MainInstallResolvedDependencies InstallDependencies { get; init; }
    public required MainStartupResolvedDependencies StartupDependencies { get; init; }
    public required MainShellFacadePorts Ports { get; init; }
}
