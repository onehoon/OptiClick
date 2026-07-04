using OptiClick.Core.Runtime;
using OptiClick.Wpf.Install.UiState;
using OptiClick.Wpf.Localization;
using OptiClick.Wpf.Models;
using OptiClick.Wpf.Shell.Games.GpuBundle;
using OptiClick.Wpf.Shell.Runtime;
using OptiClick.Wpf.Shell.Selection;

namespace OptiClick.Wpf.ViewModels;

internal sealed class MainRuntimeCatalogUiFlowContextFactory
{
    private readonly MainRuntimeCatalogUiFlowContextFactoryInput _input;

    public MainRuntimeCatalogUiFlowContextFactory(MainRuntimeCatalogUiFlowContextFactoryInput input)
    {
        _input = input ?? throw new ArgumentNullException(nameof(input));
    }

    public MainRuntimeCatalogUiFlowContext Create()
    {
        return new MainRuntimeCatalogUiFlowContext
        {
            State = new MainRuntimeCatalogUiFlowState
            {
                ReadStrings = _input.ReadStrings,
                ReadSelectionState = _input.ReadSelectionState,
                ApplySelectionState = _input.ApplySelectionState,
                ReadLatestRemoteCatalogErrorCode = _input.ReadLatestRemoteCatalogErrorCode,
                SetRemoteCatalogError = _input.SetRemoteCatalogError,
                SetGpuManifestRestartRequired = _input.SetGpuManifestRestartRequired,
                ReadGpuManifestRestartDialogShown = _input.ReadGpuManifestRestartDialogShown,
                SetGpuManifestRestartDialogShown = _input.SetGpuManifestRestartDialogShown,
                ReadRemoteCatalogGameCount = _input.ReadRemoteCatalogGameCount,
                ReadVisibleGameCount = _input.ReadVisibleGameCount,
                HasSupportedGamesEntries = _input.HasSupportedGamesEntries,
                SetSettingsStatusText = _input.SetSettingsStatusText,
                SetScanStatusText = _input.SetScanStatusText
            },
            Services = new MainRuntimeCatalogUiFlowServices
            {
                DispatchFlowLogs = _input.DispatchFlowLogs,
                CreateRuntimeCatalogStateUpdate = _input.CreateRuntimeCatalogStateUpdate,
                ApplyStateUpdate = _input.ApplyStateUpdate,
                ResetRemoteCatalogDialogGate = _input.ResetRemoteCatalogDialogGate,
                RefreshVisibleGamesFromScanMatches = _input.RefreshVisibleGamesFromScanMatches,
                RebuildSupportedGamesRows = _input.RebuildSupportedGamesRows,
                StartStartupPreparationAsync = _input.StartStartupPreparationAsync,
                RefreshArchiveReadinessAsync = _input.RefreshArchiveReadinessAsync,
                RefreshRuntimeCatalogWithSelectedGpuAsync = _input.RefreshRuntimeCatalogWithSelectedGpuAsync,
                ShowRemoteCatalogDialogOnceAsync = _input.ShowRemoteCatalogDialogOnceAsync,
                ShowDialogAsync = _input.ShowDialogAsync,
                ClearScannedGameState = _input.ClearScannedGameState,
                ReplaceGameCards = _input.ReplaceGameCards,
                SetSelectedGame = _input.SetSelectedGame,
                GpuBundleManifestClient = _input.GpuBundleManifestClient,
                GpuBundleManifestRuleResolver = _input.GpuBundleManifestRuleResolver,
                ReadAppVersion = _input.ReadAppVersion
            },
            Callbacks = new MainRuntimeCatalogUiFlowCallbacks
            {
                CaptureStartupRemoteCatalogSnapshot = _input.CaptureStartupRemoteCatalogSnapshot,
                LogRemoteInfo = _input.LogRemoteInfo,
                LogRuntimeInfo = _input.LogRuntimeInfo,
                LogRuntimeWarning = _input.LogRuntimeWarning
            }
        };
    }
}

internal sealed record MainRuntimeCatalogUiFlowContextFactoryInput
{
    public required Func<AppStrings> ReadStrings { get; init; }
    public required Func<ShellInstallSelectionState> ReadSelectionState { get; init; }
    public required Action<ShellInstallSelectionState> ApplySelectionState { get; init; }
    public required Func<string> ReadLatestRemoteCatalogErrorCode { get; init; }
    public required Action<string, string> SetRemoteCatalogError { get; init; }
    public required Action<bool> SetGpuManifestRestartRequired { get; init; }
    public required Func<bool> ReadGpuManifestRestartDialogShown { get; init; }
    public required Action<bool> SetGpuManifestRestartDialogShown { get; init; }
    public required Func<int> ReadRemoteCatalogGameCount { get; init; }
    public required Func<int> ReadVisibleGameCount { get; init; }
    public required Func<bool> HasSupportedGamesEntries { get; init; }
    public required Action<string> SetSettingsStatusText { get; init; }
    public required Action<string> SetScanStatusText { get; init; }
    public required Action<IReadOnlyList<RuntimeFlowLogEntry>, string> DispatchFlowLogs { get; init; }
    public required Func<RuntimeCatalogFlowResult, string, MainViewModelStateUpdate> CreateRuntimeCatalogStateUpdate { get; init; }
    public required Action<MainViewModelStateUpdate> ApplyStateUpdate { get; init; }
    public required Action ResetRemoteCatalogDialogGate { get; init; }
    public required Action RefreshVisibleGamesFromScanMatches { get; init; }
    public required Action RebuildSupportedGamesRows { get; init; }
    public required Func<CancellationToken, Task> StartStartupPreparationAsync { get; init; }
    public required Func<CancellationToken, Task> RefreshArchiveReadinessAsync { get; init; }
    public required Func<GpuInfo, RuntimeCatalogRefreshMode, CancellationToken, Task> RefreshRuntimeCatalogWithSelectedGpuAsync { get; init; }
    public required Func<AppDialogRequest, CancellationToken, Task> ShowRemoteCatalogDialogOnceAsync { get; init; }
    public required Func<AppDialogRequest, CancellationToken, Task<AppDialogResult>> ShowDialogAsync { get; init; }
    public required Action ClearScannedGameState { get; init; }
    public required Action<IReadOnlyList<GameCardViewModel>, bool> ReplaceGameCards { get; init; }
    public required Action<GameCardViewModel?> SetSelectedGame { get; init; }
    public required IRemoteGpuBundleManifestClient GpuBundleManifestClient { get; init; }
    public required IGpuBundleManifestRuleResolver GpuBundleManifestRuleResolver { get; init; }
    public required Func<string> ReadAppVersion { get; init; }
    public required Action<RuntimeCatalogFlowResult, string> CaptureStartupRemoteCatalogSnapshot { get; init; }
    public required Action<string> LogRemoteInfo { get; init; }
    public required Action<string> LogRuntimeInfo { get; init; }
    public required Action<string> LogRuntimeWarning { get; init; }
}
