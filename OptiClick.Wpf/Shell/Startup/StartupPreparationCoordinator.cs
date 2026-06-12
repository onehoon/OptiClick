using OptiClick.Core.Abstractions;
using OptiClick.Core.Models;
using OptiClick.Wpf.Install.Archives;
using OptiClick.Wpf.Install.Planning;
using OptiClick.Wpf.Models;
using OptiClick.Wpf.Shell.RuntimeData;

namespace OptiClick.Wpf.Shell.Startup;

public sealed class StartupPreparationCoordinator
{
    private const string ArchiveWarmupFailedCode = "archive_readiness_warmup_failed";
    private readonly object _startupDialogsReadyGate = new();
    private readonly StartupBackgroundTaskManager _startupBackgroundTaskManager;
    private readonly ArchiveReadinessRefreshCoordinator _archiveReadinessRefreshCoordinator;
    private readonly ArchiveReadinessWarmupController _archiveReadinessWarmupController;
    private readonly FirstRunPreparationDecisionService _firstRunPreparationDecisionService;
    private readonly FirstRunStateMarkerService _firstRunStateMarkerService;
    private readonly CoverCacheBootstrapCoordinator _coverCacheBootstrapCoordinator;
    private Task _startupDialogsReadyTask = Task.CompletedTask;

    public StartupPreparationCoordinator(
        StartupBackgroundTaskManager startupBackgroundTaskManager,
        ArchiveReadinessRefreshCoordinator archiveReadinessRefreshCoordinator,
        ArchiveReadinessWarmupController archiveReadinessWarmupController,
        IFirstRunStateStore firstRunStateStore,
        ICoverCacheBootstrapService coverCacheBootstrapService,
        IAppLocalDataPathProvider localDataPathProvider)
        : this(
            startupBackgroundTaskManager,
            archiveReadinessRefreshCoordinator,
            archiveReadinessWarmupController,
            new FirstRunPreparationDecisionService(firstRunStateStore, localDataPathProvider),
            new FirstRunStateMarkerService(firstRunStateStore),
            new CoverCacheBootstrapCoordinator(coverCacheBootstrapService))
    {
    }

    internal StartupPreparationCoordinator(
        StartupBackgroundTaskManager startupBackgroundTaskManager,
        ArchiveReadinessRefreshCoordinator archiveReadinessRefreshCoordinator,
        ArchiveReadinessWarmupController archiveReadinessWarmupController,
        FirstRunPreparationDecisionService firstRunPreparationDecisionService,
        FirstRunStateMarkerService firstRunStateMarkerService,
        CoverCacheBootstrapCoordinator coverCacheBootstrapCoordinator)
    {
        _startupBackgroundTaskManager = startupBackgroundTaskManager ?? throw new ArgumentNullException(nameof(startupBackgroundTaskManager));
        _archiveReadinessRefreshCoordinator = archiveReadinessRefreshCoordinator ?? throw new ArgumentNullException(nameof(archiveReadinessRefreshCoordinator));
        _archiveReadinessWarmupController = archiveReadinessWarmupController ?? throw new ArgumentNullException(nameof(archiveReadinessWarmupController));
        _firstRunPreparationDecisionService = firstRunPreparationDecisionService ?? throw new ArgumentNullException(nameof(firstRunPreparationDecisionService));
        _firstRunStateMarkerService = firstRunStateMarkerService ?? throw new ArgumentNullException(nameof(firstRunStateMarkerService));
        _coverCacheBootstrapCoordinator = coverCacheBootstrapCoordinator ?? throw new ArgumentNullException(nameof(coverCacheBootstrapCoordinator));
    }

    public Task StartStartupPreparationAsync(
        StartupPreparationCoordinatorRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        request.LogPort.LogAppInfo("milestone archive_warmup_background_started");
        var cancellationTokenSource = _startupBackgroundTaskManager.CreateSource();
        var startupDialogsReadySource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        lock (_startupDialogsReadyGate)
        {
            _startupDialogsReadyTask = startupDialogsReadySource.Task;
        }

        _ = RunStartupPreparationInBackgroundAsync(
            request,
            cancellationTokenSource,
            startupDialogsReadySource);

        return Task.CompletedTask;
    }

    public async Task WaitForStartupDialogsReadyAsync(CancellationToken cancellationToken = default)
    {
        Task readyTask;
        lock (_startupDialogsReadyGate)
        {
            readyTask = _startupDialogsReadyTask;
        }

        await readyTask.WaitAsync(cancellationToken);
    }

    private async Task RunStartupPreparationInBackgroundAsync(
        StartupPreparationCoordinatorRequest request,
        CancellationTokenSource cancellationTokenSource,
        TaskCompletionSource startupDialogsReadySource)
    {
        var cancellationToken = cancellationTokenSource.Token;
        var showFirstRunPreparationOverlay = false;
        ArchiveReadinessFlowResult? archiveReadinessResult = null;
        CancellationTokenSource? coverCacheBootstrapCancellation = null;
        Task<CoverCacheBootstrapResult>? coverCacheBootstrapTask = null;
        try
        {
            showFirstRunPreparationOverlay = await ShouldShowFirstRunPreparationOverlayAsync(
                request,
                cancellationToken);
            if (showFirstRunPreparationOverlay)
            {
                request.UiPort.ApplyFirstRunPreparationOverlay(true);
                request.LogPort.LogAppInfo("milestone startup_overlay_shown");
                coverCacheBootstrapCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                coverCacheBootstrapTask = _coverCacheBootstrapCoordinator.StartForColdStartAsync(
                    request,
                    coverCacheBootstrapCancellation.Token);
            }
            else
            {
                request.StatePort.UpdateStartupPreparationState(state => state with
                {
                    CoverCacheBootstrapState = CoverCacheBootstrapState.NotRequired
                });
                startupDialogsReadySource.TrySetResult();
            }

            request.StatePort.UpdateStartupPreparationState(state => state with
            {
                ArchiveWarmupState = ArchiveReadinessWarmupState.Running
            });
            await _archiveReadinessWarmupController.StartAsync(
                async ct =>
                {
                    archiveReadinessResult = await _archiveReadinessRefreshCoordinator.RunBackgroundRefreshAsync(
                        request.RuntimePort.RefreshArchiveReadinessWithoutCoordinatorAsync,
                        ct);
                    ct.ThrowIfCancellationRequested();
                    if (!archiveReadinessResult.IsSuccess
                        || !AreRequiredStartupArchivesReady(archiveReadinessResult.Readiness))
                    {
                        throw new InvalidOperationException("Archive readiness refresh failed.");
                    }

                    await request.RuntimePort.RecomputeSelectionAfterScanAsync(ct);
                },
                request.LogPort.LogInstallInfo,
                request.LogPort.LogInstallWarning,
                cancellationToken);
        }
        finally
        {
            var archiveWarmupState = _archiveReadinessWarmupController.State;
            if (showFirstRunPreparationOverlay)
            {
                var coverCacheBootstrapResult = await _coverCacheBootstrapCoordinator.ResolveCompletionAsync(
                    request,
                    coverCacheBootstrapTask,
                    coverCacheBootstrapCancellation,
                    archiveWarmupState,
                    archiveReadinessResult);
                request.UiPort.ApplyFirstRunPreparationOverlay(false);
                request.LogPort.LogAppInfo("milestone first_run_overlay_hidden");
                await CompleteFirstRunPreparationOverlayAsync(
                    request,
                    archiveWarmupState,
                    archiveReadinessResult,
                    coverCacheBootstrapResult,
                    cancellationToken);
            }

            startupDialogsReadySource.TrySetResult();
            request.StatePort.UpdateStartupPreparationState(state => state with
            {
                ArchiveWarmupState = archiveWarmupState,
                LastErrorCode = archiveWarmupState == ArchiveReadinessWarmupState.Failed
                    ? ArchiveWarmupFailedCode
                    : archiveWarmupState == ArchiveReadinessWarmupState.Completed
                        ? request.StatePort.ClearLastErrorCode(state.LastErrorCode, ArchiveWarmupFailedCode)
                        : state.LastErrorCode
            });
            _startupBackgroundTaskManager.Remove(cancellationTokenSource);
            coverCacheBootstrapCancellation?.Dispose();
        }
    }

    private async Task<bool> ShouldShowFirstRunPreparationOverlayAsync(
        StartupPreparationCoordinatorRequest request,
        CancellationToken cancellationToken)
    {
        var decision = await _firstRunPreparationDecisionService.DecideAsync(
            new FirstRunPreparationDecisionRequest
            {
                ShouldBlockStartupForUnsupportedOperatingSystem = request.RuntimePort.ShouldBlockStartupForUnsupportedOperatingSystem(),
                LatestArchiveReadiness = request.StatePort.ReadLatestArchiveReadiness(),
                ModuleDownloadLinks = request.RuntimePort.ReadModuleDownloadLinks(),
                Fsr4VariantCatalog = request.RuntimePort.ReadFsr4VariantCatalog()
            },
            cancellationToken);

        if (decision.LocalReadiness is not null)
        {
            request.StatePort.SetArchiveReadiness(decision.LocalReadiness);
        }

        if (decision.ShouldSavePreparedMarker)
        {
            await _firstRunStateMarkerService.SaveCompletedMarkerAsync(CancellationToken.None);
        }

        return decision.ShouldShowOverlay;
    }

    private async Task CompleteFirstRunPreparationOverlayAsync(
        StartupPreparationCoordinatorRequest request,
        ArchiveReadinessWarmupState archiveWarmupState,
        ArchiveReadinessFlowResult? archiveReadinessResult,
        CoverCacheBootstrapResult coverCacheBootstrapResult,
        CancellationToken cancellationToken)
    {
        if (archiveWarmupState == ArchiveReadinessWarmupState.Canceled
            || cancellationToken.IsCancellationRequested)
        {
            return;
        }

        var readiness = archiveReadinessResult?.Readiness ?? request.StatePort.ReadLatestArchiveReadiness();
        if (archiveWarmupState == ArchiveReadinessWarmupState.Completed
            && AreRequiredStartupArchivesReady(readiness)
            && CoverCacheBootstrapCoordinator.IsReady(coverCacheBootstrapResult.State))
        {
            await _firstRunStateMarkerService.SaveCompletedMarkerAsync(CancellationToken.None, coverCacheBootstrapResult);
            return;
        }

        await request.UiPort.ShowFirstRunPreparationFailureAsync(
            CreateFirstRunPreparationFailureRequest(request.FirstRunPreparationFailureText),
            CancellationToken.None);
    }

    private static AppDialogRequest CreateFirstRunPreparationFailureRequest(StartupPreparationFailureText text)
    {
        return new AppDialogRequest
        {
            Kind = AppDialogKind.Blocking,
            Severity = DialogSeverity.Blocking,
            Title = text.Title,
            Summary = text.Summary,
            IsBlocking = true,
            CanClose = false,
            CloseOnOverlayClick = false,
            PrimaryButtonText = text.PrimaryButtonText
        };
    }

    private static bool AreRequiredStartupArchivesReady(ArchiveReadinessSnapshot readiness)
    {
        // All install archives are startup-critical. Missing "optional" caches can still make a later
        // game-specific install fail after the user reaches the final button, so first-run warmup must
        // verify the complete archive set instead of only the currently selected game's plan.
        return readiness.AreAllStartupArchivesReady();
    }

}

public sealed record StartupPreparationCoordinatorRequest
{
    public required StartupPreparationStatePort StatePort { get; init; }
    public required StartupPreparationUiPort UiPort { get; init; }
    public required StartupPreparationRuntimePort RuntimePort { get; init; }
    public required StartupPreparationLogPort LogPort { get; init; }
    public required StartupPreparationFailureText FirstRunPreparationFailureText { get; init; }
}

public sealed record StartupPreparationStatePort
{
    public required Func<ArchiveReadinessSnapshot> ReadLatestArchiveReadiness { get; init; }
    public required Action<ArchiveReadinessSnapshot> SetArchiveReadiness { get; init; }
    public required Action<Func<StartupPreparationState, StartupPreparationState>> UpdateStartupPreparationState
    {
        get;
        init;
    }

    public required Func<string, string, string> ClearLastErrorCode { get; init; }
}

public sealed record StartupPreparationUiPort
{
    public required Action<bool> ApplyFirstRunPreparationOverlay { get; init; }
    public required Func<AppDialogRequest, CancellationToken, Task> ShowFirstRunPreparationFailureAsync { get; init; }
}

public sealed record StartupPreparationRuntimePort
{
    public required Func<bool> ShouldBlockStartupForUnsupportedOperatingSystem { get; init; }
    public required Func<ModuleDownloadLinkContext> ReadModuleDownloadLinks { get; init; }
    public required Func<Fsr4VariantCatalog> ReadFsr4VariantCatalog { get; init; }
    public required Func<CancellationToken, Task<ArchiveReadinessFlowResult>> RefreshArchiveReadinessWithoutCoordinatorAsync
    {
        get;
        init;
    }

    public required Func<CancellationToken, Task> RecomputeSelectionAfterScanAsync { get; init; }
}

public sealed record StartupPreparationLogPort
{
    public required Action<string> LogAppInfo { get; init; }
    public required Action<string> LogAppWarning { get; init; }
    public required Action<string> LogInstallInfo { get; init; }
    public required Action<string> LogInstallWarning { get; init; }
}

public sealed record StartupPreparationFailureText
{
    public string Title { get; init; } = "";
    public string Summary { get; init; } = "";
    public string PrimaryButtonText { get; init; } = "";
}
