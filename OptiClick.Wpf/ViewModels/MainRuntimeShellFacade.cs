using OptiClick.Core.Runtime;
using OptiClick.Wpf.Install.Archives;
using OptiClick.Wpf.Install.UiState;
using OptiClick.Wpf.Localization;
using OptiClick.Wpf.Logging;
using OptiClick.Wpf.Models;
using OptiClick.Wpf.Shell.Dialogs;
using OptiClick.Wpf.Shell.Flow;
using OptiClick.Wpf.Shell.Games;
using OptiClick.Wpf.Shell.Games.GpuBundle;
using OptiClick.Wpf.Shell.Gpu;
using OptiClick.Wpf.Shell.Runtime;
using OptiClick.Wpf.Shell.RuntimeData;
using OptiClick.Wpf.Shell.Scan;
using OptiClick.Wpf.Shell.Selection;

namespace OptiClick.Wpf.ViewModels;

internal sealed record MainRuntimeShellFacade
{
    public required MainRuntimeCatalogUiFlowContextFactory CatalogUiFlowContextFactory { get; init; }
    public required MainRuntimeFlowContextFactory RuntimeFlowContextFactory { get; init; }

    public static MainRuntimeShellFacade Create(MainRuntimeShellFacadeInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var factories = MainRuntimeContextFactoryComposition.Compose(
            new MainRuntimeContextFactoryCompositionInput
            {
                CatalogUi = new MainRuntimeCatalogUiFlowContextFactoryInput
                {
                    ReadStrings = input.Interaction.ReadStrings,
                    ReadSelectionState = input.State.ReadSelectionState,
                    ApplySelectionState = input.State.ApplySelectionState,
                    ReadLatestRemoteCatalogErrorCode = () => input.State.RuntimeShellState.LatestRemoteCatalogErrorCode,
                    SetRemoteCatalogError = input.State.RuntimeShellState.SetRemoteCatalogError,
                    SetGpuManifestRestartRequired = input.State.RuntimeShellState.SetGpuManifestRestartRequired,
                    ReadGpuManifestRestartDialogShown =
                        () => input.State.RuntimeShellState.HasShownGpuManifestRestartDialog,
                    SetGpuManifestRestartDialogShown = input.State.RuntimeShellState.SetGpuManifestRestartDialogShown,
                    ReadRemoteCatalogGameCount = () => input.State.RuntimeShellState.LatestRemoteCatalog.Games.Count,
                    ReadVisibleGameCount = input.State.ReadVisibleGameCount,
                    HasSupportedGamesEntries = input.State.HasSupportedGamesEntries,
                    SetSettingsStatusText = input.State.SetSettingsStatusText,
                    SetScanStatusText = input.State.SetScanStatusText,
                    DispatchFlowLogs = input.Interaction.FlowLogDispatcher.Dispatch,
                    CreateRuntimeCatalogStateUpdate = input.Interaction.ResultApplier.CreateRuntimeCatalogStateUpdate,
                    ApplyStateUpdate = input.State.ApplyStateUpdate,
                    ResetRemoteCatalogDialogGate = input.Interaction.RemoteCatalogDialogGate.Reset,
                    RefreshVisibleGamesFromScanMatches = input.CrossFeature.RefreshVisibleGamesFromScanMatches,
                    RebuildSupportedGamesRows = input.CrossFeature.RebuildSupportedGamesRows,
                    StartStartupPreparationAsync = input.CrossFeature.StartStartupPreparationAsync,
                    RefreshArchiveReadinessAsync = async ct =>
                    {
                        _ = await input.CrossFeature.RefreshArchiveReadinessAsync(ct);
                    },
                    ShowRemoteCatalogDialogOnceAsync = input.Interaction.ShowRemoteCatalogDialogOnceAsync,
                    ShowDialogAsync = input.Interaction.DialogPresenter.ShowSafelyAsync,
                    ClearScannedGameState = input.State.ScannedGameState.Clear,
                    ReplaceGameCards = input.CrossFeature.ReplaceGameCards,
                    SetSelectedGame = input.CrossFeature.SetSelectedGame,
                    GpuBundleManifestClient = input.Catalog.GpuBundleManifestClient,
                    GpuBundleManifestRuleResolver = input.Catalog.GpuBundleManifestRuleResolver,
                    ReadAppVersion = input.Interaction.ReadAppVersion,
                    CaptureStartupRemoteCatalogSnapshot = (result, errorCode) =>
                        input.State.RuntimeShellState.TryCaptureStartupRemoteCatalogSnapshot(result, errorCode),
                    LogRemoteInfo = message => input.Interaction.AppLogger.Info(MainViewModelLogCategories.Remote, message),
                    LogRuntimeInfo = message => input.Interaction.AppLogger.Info(MainViewModelLogCategories.Runtime, message),
                    LogRuntimeWarning = message =>
                        input.Interaction.AppLogger.Warning(MainViewModelLogCategories.Runtime, message)
                },
                RuntimeFlow = new MainRuntimeFlowContextFactoryInput
                {
                    Dependencies = input.RuntimeFlowDependencies,
                    DeviceRulesRefreshLock = input.OperationLocks.DeviceRulesRefreshLock,
                    ReadStrings = input.Interaction.ReadStrings,
                    IsKoreanUi = input.Interaction.IsKoreanUi,
                    ReadSelectionState = input.State.ReadSelectionState,
                    ApplySelectionState = input.State.ApplySelectionState,
                    BuildRuntimeSummaryStateUpdate = input.State.BuildRuntimeSummaryStateUpdate,
                    ApplyRuntimeSummaryStateUpdate = input.State.ApplyRuntimeSummaryStateUpdate,
                    ResolveManifestSupportedGpuCandidatesAsync =
                        input.CrossFeature.ResolveManifestSupportedGpuCandidatesAsync,
                    ApplyMultiGpuBlockedUiState = input.CrossFeature.ApplyMultiGpuBlockedUiState,
                    ReadManifestRestartRequired = () => input.State.RuntimeShellState.IsGpuManifestRestartRequired,
                    DispatchRuntimeLogs = input.Interaction.FlowLogDispatcher.Dispatch,
                    ShowDialogAsync = input.Interaction.DialogPresenter.ShowSafelyAsync,
                    LogRuntimeInfo = message => input.Interaction.AppLogger.Info(MainViewModelLogCategories.Runtime, message),
                    LogRuntimeWarning = message =>
                        input.Interaction.AppLogger.Warning(MainViewModelLogCategories.Runtime, message),
                    RefreshRuntimeCatalogAsync = input.CrossFeature.RefreshRuntimeCatalogAsync
                }
            });

        return new MainRuntimeShellFacade
        {
            CatalogUiFlowContextFactory = factories.CatalogUi,
            RuntimeFlowContextFactory = factories.RuntimeFlow
        };
    }
}

internal sealed record MainRuntimeShellFacadeInput
{
    public required MainRuntimeFlowResolvedDependencies RuntimeFlowDependencies { get; init; }
    public required MainShellOperationLocks OperationLocks { get; init; }
    public required MainRuntimeShellStatePort State { get; init; }
    public required MainRuntimeShellInteractionPort Interaction { get; init; }
    public required MainRuntimeCatalogServicesPort Catalog { get; init; }
    public required MainRuntimeCrossFeaturePort CrossFeature { get; init; }
}

internal sealed record MainRuntimeShellStatePort
{
    public required RuntimeShellState RuntimeShellState { get; init; }
    public required ScannedGameState ScannedGameState { get; init; }
    public required Func<ShellInstallSelectionState> ReadSelectionState { get; init; }
    public required Action<ShellInstallSelectionState> ApplySelectionState { get; init; }
    public required Func<int> ReadVisibleGameCount { get; init; }
    public required Func<bool> HasSupportedGamesEntries { get; init; }
    public required Action<string> SetSettingsStatusText { get; init; }
    public required Action<string> SetScanStatusText { get; init; }
    public required Action<MainViewModelStateUpdate> ApplyStateUpdate { get; init; }
    public required Func<RuntimeSummaryStateUpdate> BuildRuntimeSummaryStateUpdate { get; init; }
    public required Action<RuntimeSummaryStateUpdate> ApplyRuntimeSummaryStateUpdate { get; init; }
}

internal sealed record MainRuntimeShellInteractionPort
{
    public required FlowLogDispatcher FlowLogDispatcher { get; init; }
    public required MainViewModelResultApplier ResultApplier { get; init; }
    public required OnceDialogGate RemoteCatalogDialogGate { get; init; }
    public required DialogPresenter DialogPresenter { get; init; }
    public required IAppLogger AppLogger { get; init; }
    public required Func<AppStrings> ReadStrings { get; init; }
    public required Func<bool> IsKoreanUi { get; init; }
    public required Func<string> ReadAppVersion { get; init; }
    public required Func<AppDialogRequest, CancellationToken, Task> ShowRemoteCatalogDialogOnceAsync { get; init; }
}

internal sealed record MainRuntimeCatalogServicesPort
{
    public required IRemoteGpuBundleManifestClient GpuBundleManifestClient { get; init; }
    public required IGpuBundleManifestRuleResolver GpuBundleManifestRuleResolver { get; init; }
}

internal sealed record MainRuntimeCrossFeaturePort
{
    public required Action RefreshVisibleGamesFromScanMatches { get; init; }
    public required Action RebuildSupportedGamesRows { get; init; }
    public required Func<CancellationToken, Task> StartStartupPreparationAsync { get; init; }
    public required Func<CancellationToken, Task<ArchiveReadinessFlowResult>> RefreshArchiveReadinessAsync { get; init; }
    public required Action<IReadOnlyList<GameCardViewModel>, bool> ReplaceGameCards { get; init; }
    public required Action<GameCardViewModel?> SetSelectedGame { get; init; }
    public required Func<RuntimeContext, IReadOnlyList<GpuInfo>, CancellationToken, Task<IReadOnlyList<GpuInfo>>>
        ResolveManifestSupportedGpuCandidatesAsync
    {
        get;
        init;
    }

    public required Action ApplyMultiGpuBlockedUiState { get; init; }
    public required Func<RuntimeCatalogRefreshMode, CancellationToken, Task> RefreshRuntimeCatalogAsync { get; init; }
}
