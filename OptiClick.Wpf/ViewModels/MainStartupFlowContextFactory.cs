using OptiClick.Wpf.Install.Archives;
using OptiClick.Wpf.Shell.Startup;

namespace OptiClick.Wpf.ViewModels;

internal sealed class MainStartupFlowContextFactory
{
    private readonly MainStartupFlowContextFactoryInput _input;

    public MainStartupFlowContextFactory(MainStartupFlowContextFactoryInput input)
    {
        _input = input ?? throw new ArgumentNullException(nameof(input));
    }

    public MainStartupFlowContext Create()
    {
        return new MainStartupFlowContext
        {
            State = new MainStartupFlowState
            {
                ReadShouldBlockStartupForUnsupportedOperatingSystem =
                    _input.ReadShouldBlockStartupForUnsupportedOperatingSystem,
                ReadLocalDataRoot = _input.ReadLocalDataRoot,
                ReadArchiveCachePaths = _input.ReadArchiveCachePaths,
                UpdateStartupPreparationState = _input.UpdateStartupPreparationState,
                ApplyStartupDialogsStartedState = ApplyStartupDialogsStartedState,
                StartupInitializationErrorCode = _input.StartupInitializationErrorCode,
                StartupInitializationWarningText = _input.ReadStartupInitializationWarningText(),
                SetSettingsStatusText = _input.SetSettingsStatusText
            },
            Services = new MainStartupFlowServices
            {
                ShowStartupBlockDialogAsync = _input.ShowStartupBlockDialogAsync,
                RunInitialStartupAsync = _input.RunInitialStartupAsync,
                ShowPendingStartupNoticesAsync = _input.ShowPendingStartupNoticesAsync,
                ReadAppVersion = _input.ReadAppVersion,
                ReadLogDirectory = _input.ReadLogDirectory,
                RefreshRuntimeContextAsync = _input.RefreshRuntimeContextAsync,
                RefreshRuntimeDataCatalogForStartupAsync =
                    _input.RefreshRuntimeDataCatalogForStartupAsync,
                WaitForStartupDialogsReadyAsync = _input.WaitForStartupDialogsReadyAsync,
                RunStartupAutoScanAsync = _input.RunStartupAutoScanAsync,
                RefreshDeviceIdentityRulesAsync = _input.RefreshDeviceIdentityRulesAsync,
                ApplyDeviceIdentityRulesFromCacheAsync =
                    _input.ApplyDeviceIdentityRulesFromCacheAsync,
                StartDeviceIdentityRulesRefreshInBackground =
                    _input.StartDeviceIdentityRulesRefreshInBackground,
                StartStartupDialogsInBackground = _input.StartStartupDialogsInBackground,
                StartStartupUpdateCheckInBackground =
                    _input.StartStartupUpdateCheckInBackground,
                StartStartupAnnouncementInBackground =
                    _input.StartStartupAnnouncementInBackground,
                StartSupportedGamesWikiRefreshInBackground =
                    _input.StartSupportedGamesWikiRefreshInBackground,
                StartGameMasterCoverPrefetchInBackground =
                    _input.StartGameMasterCoverPrefetchInBackground,
                LogInfo = _input.LogInfo
            },
            Callbacks = new MainStartupFlowCallbacks
            {
                LogStartupInitializationError = _input.LogStartupInitializationError,
                ClearLastErrorCode = _input.ClearLastErrorCode
            }
        };
    }

    private void ApplyStartupDialogsStartedState()
    {
        _input.UpdateStartupPreparationState(state => state with
        {
            StartupDialogsStarted = true,
            StartupDialogsRunning = true,
            StartupDialogsCompleted = false,
            StartupDialogsCanceled = false,
            StartupDialogsFailed = false
        });
    }
}

internal sealed record MainStartupFlowContextFactoryInput
{
    public required Func<bool> ReadShouldBlockStartupForUnsupportedOperatingSystem { get; init; }
    public required Func<string> ReadLocalDataRoot { get; init; }
    public required Func<ArchiveCachePaths> ReadArchiveCachePaths { get; init; }
    public required Action<Func<StartupPreparationState, StartupPreparationState>> UpdateStartupPreparationState
    {
        get;
        init;
    }

    public required string StartupInitializationErrorCode { get; init; }
    public required Func<string> ReadStartupInitializationWarningText { get; init; }
    public required Action<string> SetSettingsStatusText { get; init; }
    public required Func<CancellationToken, Task> ShowStartupBlockDialogAsync { get; init; }
    public required Func<StartupFlowRequest, CancellationToken, Task> RunInitialStartupAsync { get; init; }
    public required Func<CancellationToken, Task> ShowPendingStartupNoticesAsync { get; init; }
    public required Func<string> ReadAppVersion { get; init; }
    public required Func<string> ReadLogDirectory { get; init; }
    public required Func<CancellationToken, Task> RefreshRuntimeContextAsync { get; init; }
    public required Func<CancellationToken, Task> RefreshRuntimeDataCatalogForStartupAsync { get; init; }
    public required Func<CancellationToken, Task> WaitForStartupDialogsReadyAsync { get; init; }
    public required Func<CancellationToken, Task> RunStartupAutoScanAsync { get; init; }
    public required Func<CancellationToken, Task> RefreshDeviceIdentityRulesAsync { get; init; }
    public required Func<CancellationToken, Task> ApplyDeviceIdentityRulesFromCacheAsync { get; init; }
    public required Action StartDeviceIdentityRulesRefreshInBackground { get; init; }
    public required Action StartStartupDialogsInBackground { get; init; }
    public required Action StartStartupUpdateCheckInBackground { get; init; }
    public required Action StartStartupAnnouncementInBackground { get; init; }
    public required Action StartSupportedGamesWikiRefreshInBackground { get; init; }
    public required Action StartGameMasterCoverPrefetchInBackground { get; init; }
    public required Action<string> LogInfo { get; init; }
    public required Action<Exception> LogStartupInitializationError { get; init; }
    public required Func<string, string, string> ClearLastErrorCode { get; init; }
}
