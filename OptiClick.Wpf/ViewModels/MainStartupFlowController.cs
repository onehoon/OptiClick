using System;
using System.Threading;
using System.Threading.Tasks;
using OptiClick.Wpf.Install.Archives;
using OptiClick.Wpf.Shell.Startup;

namespace OptiClick.Wpf.ViewModels;

internal sealed class MainStartupFlowController
{
    private readonly MainStartupRuntimeFacade _startupRuntimeFacade;

    public MainStartupFlowController(MainStartupRuntimeFacade startupRuntimeFacade)
    {
        _startupRuntimeFacade = startupRuntimeFacade;
    }

    public Task<bool> ShowStartupOperatingSystemBlockIfNeededAsync(
        MainStartupFlowContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        return _startupRuntimeFacade.ShowStartupOperatingSystemBlockIfNeededAsync(
            CreateStartupOrchestratorContext(context),
            cancellationToken);
    }

    public Task InitializeAsync(
        MainStartupFlowContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        return _startupRuntimeFacade.InitializeAsync(
            CreateStartupOrchestratorContext(context),
            cancellationToken);
    }

    private MainStartupOrchestratorContext CreateStartupOrchestratorContext(MainStartupFlowContext context)
    {
        return new MainStartupOrchestratorContext
        {
            ShouldBlockStartupForUnsupportedOperatingSystem =
                context.State.ReadShouldBlockStartupForUnsupportedOperatingSystem,
            ShowStartupBlockDialogAsync = context.Services.ShowStartupBlockDialogAsync,
            BuildStartupFlowRequest = () => BuildStartupFlowRequest(context),
            RunInitialStartupAsync = context.Services.RunInitialStartupAsync,
            UpdateStartupPreparationState = context.State.UpdateStartupPreparationState,
            LogStartupInitializationError = ex => context.Callbacks.LogStartupInitializationError(ex),
            SetSettingsStatusText = context.State.SetSettingsStatusText,
            ClearLastErrorCode = context.Callbacks.ClearLastErrorCode,
            ShowPendingStartupNoticesAsync = context.Services.ShowPendingStartupNoticesAsync,
            StartupInitializationErrorCode = context.State.StartupInitializationErrorCode,
            StartupInitializationWarningText = context.State.StartupInitializationWarningText
        };
    }

    private StartupFlowRequest BuildStartupFlowRequest(MainStartupFlowContext context)
    {
        var archiveCachePaths = context.State.ReadArchiveCachePaths();

        return new StartupFlowRequest
        {
            AppVersion = context.Services.ReadAppVersion(),
            LocalDataRoot = context.State.ReadLocalDataRoot(),
            LogDirectory = context.Services.ReadLogDirectory(),
            CacheArchivesDirectory = archiveCachePaths.Root,
            CacheManifestDirectory = archiveCachePaths.ManifestRoot,
            CachePayloadDirectory = archiveCachePaths.OptiScalerPayloadCacheRoot,
            RefreshRuntimeContextAsync = async ct =>
            {
                await context.Services.RefreshRuntimeContextAsync(ct);
                context.State.UpdateStartupPreparationState(state => state with { RuntimeContextCompleted = true });
            },
            RefreshRuntimeDataCatalogForStartupAsync = async ct =>
            {
                await context.Services.RefreshRuntimeDataCatalogForStartupAsync(ct);
                context.State.UpdateStartupPreparationState(state => state with { RuntimeCatalogCompleted = true });
            },
            WaitForStartupDialogsReadyAsync = context.Services.WaitForStartupDialogsReadyAsync,
            RunStartupAutoScanAsync = async ct =>
            {
                await context.Services.RunStartupAutoScanAsync(ct);
                context.State.UpdateStartupPreparationState(state => state with { StartupScanCompleted = true });
            },
            RefreshDeviceIdentityRulesAsync = async ct =>
            {
                // Apply only cached identity rules during startup; remote data can update later in background.
                await context.Services.ApplyDeviceIdentityRulesFromCacheAsync(ct);
                context.State.UpdateStartupPreparationState(state => state with { DeviceIdentityRulesCompleted = true });
            },
            StartDeviceIdentityRulesRefreshInBackground = context.Services.StartDeviceIdentityRulesRefreshInBackground,
            StartStartupDialogsInBackground = () => StartStartupDialogs(context),
            StartStartupUpdateCheckInBackground = () => StartStartupUpdateCheck(context),
            StartStartupAnnouncementInBackground = () => StartStartupAnnouncement(context),
            StartSupportedGamesWikiRefreshInBackground = context.Services.StartSupportedGamesWikiRefreshInBackground,
            StartGameMasterCoverPrefetchInBackground = context.Services.StartGameMasterCoverPrefetchInBackground,
            LogInfo = context.Services.LogInfo
        };
    }

    private void StartStartupDialogs(MainStartupFlowContext context)
    {
        context.State.ApplyStartupDialogsStartedState();
        context.Services.StartStartupDialogsInBackground();
    }

    private void StartStartupUpdateCheck(MainStartupFlowContext context)
    {
        context.State.ApplyStartupDialogsStartedState();
        context.Services.StartStartupUpdateCheckInBackground();
    }

    private void StartStartupAnnouncement(MainStartupFlowContext context)
    {
        context.State.ApplyStartupDialogsStartedState();
        context.Services.StartStartupAnnouncementInBackground();
    }
}

internal sealed class MainStartupFlowContext
{
    public required MainStartupFlowState State { get; init; }
    public required MainStartupFlowServices Services { get; init; }
    public required MainStartupFlowCallbacks Callbacks { get; init; }
}

internal sealed class MainStartupFlowState
{
    public required Func<bool> ReadShouldBlockStartupForUnsupportedOperatingSystem { get; init; }
    public required Func<string> ReadLocalDataRoot { get; init; }
    public required Func<ArchiveCachePaths> ReadArchiveCachePaths { get; init; }
    public required Action<Func<StartupPreparationState, StartupPreparationState>> UpdateStartupPreparationState
    { get; init; }
    public required Action ApplyStartupDialogsStartedState { get; init; }
    public required string StartupInitializationErrorCode { get; init; }
    public required string StartupInitializationWarningText { get; init; }
    public required Action<string> SetSettingsStatusText { get; init; }
}

internal sealed class MainStartupFlowServices
{
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
}

internal sealed class MainStartupFlowCallbacks
{
    public required Action<Exception> LogStartupInitializationError { get; init; }
    public required Func<string, string, string> ClearLastErrorCode { get; init; }
}
