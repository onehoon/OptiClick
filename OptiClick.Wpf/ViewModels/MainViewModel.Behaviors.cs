using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using OptiClick.Core.Runtime;
using OptiClick.Wpf.Install.Archives;
using OptiClick.Wpf.Install.Planning;
using OptiClick.Wpf.Install.UiState;
using OptiClick.Wpf.Models;
using OptiClick.Wpf.Services;
using OptiClick.Wpf.Shell.Games.GpuBundle;
using OptiClick.Wpf.Shell.Gpu;
using OptiClick.Wpf.Shell.Navigation;
using OptiClick.Wpf.Shell.Runtime;
using OptiClick.Wpf.Shell.RuntimeData;
using OptiClick.Wpf.Shell.Games;
using OptiClick.Wpf.Shell.Selection;
using OptiClick.Wpf.Shell.Startup;
using OptiClick.Wpf.Threading;

namespace OptiClick.Wpf.ViewModels;

public sealed partial class MainViewModel : ViewModelBase
{
    public async Task<bool> ShowStartupOperatingSystemBlockIfNeededAsync(CancellationToken cancellationToken = default)
    {
        if (!ShouldBlockStartupForUnsupportedOperatingSystem())
        {
            return false;
        }

        await _dialogPresenter.ShowSafelyAsync(
            _startupNoticePresenter.BuildWindows10StartupBlockDialog(Strings),
            cancellationToken);
        return true;
    }

    public async Task InitializeAsync()
    {
        try
        {
            await _startupFlowCoordinator.RunInitialStartupAsync(BuildStartupFlowRequest());
            UpdateStartupPreparationState(state => state with
            {
                LastErrorCode = ClearLastErrorCode(state.LastErrorCode, "startup_initialization_failed")
            });
        }
        catch (Exception ex)
        {
            LogError(MainViewModelLogCategories.App, "startup initialization failed", ex);
            UpdateStartupPreparationState(state => state with { LastErrorCode = "startup_initialization_failed" });
            SettingsStatusText = Strings.RuntimeStartupInitWarning;
        }
        finally
        {
            await ShowPendingStartupNoticesAsync();
        }
    }

    private StartupFlowRequest BuildStartupFlowRequest()
    {
        return new StartupFlowRequest
        {
            AppVersion = GetCurrentAppVersion(),
            LocalDataRoot = _localDataPathProvider.RootDirectory,
            LogDirectory = _appLogger.LogDirectory,
            CacheArchivesDirectory = _localDataPathProvider.ArchivesDirectory,
            CacheManifestDirectory = _localDataPathProvider.ManifestDirectory,
            CachePayloadDirectory = _localDataPathProvider.OptiScalerPayloadDirectory,
            RefreshRuntimeContextAsync = async ct =>
            {
                await RefreshRuntimeContextAsync(ct);
                UpdateStartupPreparationState(state => state with { RuntimeContextCompleted = true });
            },
            RefreshRuntimeDataCatalogForStartupAsync = async ct =>
            {
                await RefreshRuntimeDataCatalogForStartupAsync(ct);
                UpdateStartupPreparationState(state => state with { RuntimeCatalogCompleted = true });
            },
            WaitForStartupDialogsReadyAsync = WaitForStartupDialogsReadyAsync,
            RunStartupAutoScanAsync = async ct =>
            {
                await RunStartupAutoScanAsync(ct);
                UpdateStartupPreparationState(state => state with { StartupScanCompleted = true });
            },
            RefreshDeviceIdentityRulesAsync = async ct =>
            {
                await RefreshDeviceIdentityRulesAsync(ct);
                UpdateStartupPreparationState(state => state with { DeviceIdentityRulesCompleted = true });
            },
            StartStartupDialogsInBackground = () =>
            {
                UpdateStartupPreparationState(state => state with
                {
                    StartupDialogsStarted = true,
                    StartupDialogsRunning = true,
                    StartupDialogsCompleted = false,
                    StartupDialogsCanceled = false,
                    StartupDialogsFailed = false
                });
                StartStartupDialogsInBackground();
            },
            StartSupportedGamesWikiRefreshInBackground = StartSupportedGamesWikiRefreshInBackground,
            StartGameMasterCoverPrefetchInBackground = StartGameMasterCoverPrefetchInBackground,
            LogInfo = message => LogInfo(MainViewModelLogCategories.App, message)
        };
    }

    private void UpdateStartupPreparationState(Func<StartupPreparationState, StartupPreparationState> update)
    {
        ArgumentNullException.ThrowIfNull(update);

        var didChange = false;
        lock (_startupPreparationStateGate)
        {
            var next = update(_startupPreparationState);
            if (next == _startupPreparationState)
            {
                return;
            }

            _startupPreparationState = next;
            didChange = true;
        }

        if (didChange)
        {
            StartupOverlay.ApplyPreparationState(StartupPreparationState);
            OnPropertyChanged(nameof(StartupPreparationState));
        }
    }

    private static string ClearLastErrorCode(string lastErrorCode, string errorCode)
    {
        return string.Equals(lastErrorCode, errorCode, StringComparison.OrdinalIgnoreCase)
            ? ""
            : lastErrorCode;
    }

    private Task RefreshRuntimeDataCatalogForStartupAsync(CancellationToken cancellationToken = default)
    {
        return RefreshRuntimeDataCatalogAsync(RuntimeCatalogRefreshMode.BackgroundWarmup, cancellationToken);
    }

    private void StartArchiveReadinessWarmupInBackground()
    {
        LogInfo(MainViewModelLogCategories.App, "milestone archive_warmup_background_started");
        var cancellationTokenSource = _startupBackgroundTaskManager.CreateSource();
        var startupDialogsReadySource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        lock (_startupDialogsReadyGate)
        {
            _startupDialogsReadyTask = startupDialogsReadySource.Task;
        }

        _ = RunArchiveReadinessWarmupInBackgroundAsync(cancellationTokenSource, startupDialogsReadySource);
    }

    private async Task RunArchiveReadinessWarmupInBackgroundAsync(
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
            showFirstRunPreparationOverlay = await ShouldShowFirstRunPreparationOverlayAsync(cancellationToken);
            if (showFirstRunPreparationOverlay)
            {
                StartupOverlay.ApplyFirstRunPreparationOverlay(true);
                LogInfo(MainViewModelLogCategories.App, "milestone startup_overlay_shown");
                coverCacheBootstrapCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                coverCacheBootstrapTask = StartCoverCacheBootstrapForColdStartAsync(coverCacheBootstrapCancellation.Token);
            }
            else
            {
                UpdateStartupPreparationState(state => state with
                {
                    CoverCacheBootstrapState = CoverCacheBootstrapState.NotRequired
                });
                startupDialogsReadySource.TrySetResult();
            }

            UpdateStartupPreparationState(state => state with { ArchiveWarmupState = ArchiveReadinessWarmupState.Running });
            await _archiveReadinessWarmupController.StartAsync(
                async ct =>
                {
                    archiveReadinessResult = await _archiveReadinessRefreshCoordinator.RunBackgroundRefreshAsync(
                        RefreshArchiveReadinessCoreAsync,
                        ct);
                    ct.ThrowIfCancellationRequested();
                    if (!archiveReadinessResult.IsSuccess
                        || !AreRequiredStartupArchivesReady(archiveReadinessResult.Readiness))
                    {
                        throw new InvalidOperationException("Archive readiness refresh failed.");
                    }

                    await RecomputeSelectionAfterScanAsync(ct, navigateHome: false);
                },
                message => LogInfo(MainViewModelLogCategories.Install, message),
                message => LogWarning(MainViewModelLogCategories.Install, message),
                cancellationToken);
        }
        finally
        {
            var archiveWarmupState = _archiveReadinessWarmupController.State;
            if (showFirstRunPreparationOverlay)
            {
                var coverCacheBootstrapResult = await ResolveCoverCacheBootstrapCompletionAsync(
                    coverCacheBootstrapTask,
                    coverCacheBootstrapCancellation,
                    archiveWarmupState,
                    archiveReadinessResult);
                StartupOverlay.ApplyFirstRunPreparationOverlay(false);
                LogInfo(MainViewModelLogCategories.App, "milestone first_run_overlay_hidden");
                await CompleteFirstRunPreparationOverlayAsync(
                    archiveWarmupState,
                    archiveReadinessResult,
                    coverCacheBootstrapResult,
                    cancellationToken);
            }

            startupDialogsReadySource.TrySetResult();
            UpdateStartupPreparationState(state => state with
            {
                ArchiveWarmupState = archiveWarmupState,
                LastErrorCode = archiveWarmupState == ArchiveReadinessWarmupState.Failed
                    ? "archive_readiness_warmup_failed"
                    : archiveWarmupState == ArchiveReadinessWarmupState.Completed
                        ? ClearLastErrorCode(state.LastErrorCode, "archive_readiness_warmup_failed")
                    : state.LastErrorCode
            });
            _startupBackgroundTaskManager.Remove(cancellationTokenSource);
            coverCacheBootstrapCancellation?.Dispose();
        }
    }

    private async Task<bool> ShouldShowFirstRunPreparationOverlayAsync(CancellationToken cancellationToken)
    {
        if (ShouldBlockStartupForUnsupportedOperatingSystem())
        {
            return false;
        }

        if (AreRequiredStartupArchivesReady(_runtimeShellState.LatestArchiveReadiness))
        {
            return false;
        }

        var state = await _firstRunStateStore.LoadAsync(cancellationToken);
        if (state.FirstStartupCompleted || state.ArchivePreparedOnce)
        {
            return false;
        }

        if (StartupArchiveReadinessLocalProbe.TryBuildReadySnapshot(
                _localDataPathProvider,
                _runtimeShellState.ModuleDownloadLinks,
                out var localReadiness)
            && AreRequiredStartupArchivesReady(localReadiness))
        {
            _runtimeShellState.SetArchiveReadiness(localReadiness);
            await SaveFirstRunCompletedMarkerAsync(CancellationToken.None);
            return false;
        }

        return true;
    }

    private Task SaveFirstRunCompletedMarkerAsync(
        CancellationToken cancellationToken,
        CoverCacheBootstrapResult? coverCacheBootstrapResult = null)
    {
        return _firstRunStateStore.SaveAsync(
            new FirstRunState
            {
                FirstStartupCompleted = true,
                ArchivePreparedOnce = true,
                CoverCacheBootstrapAttempted = coverCacheBootstrapResult?.Attempted ?? false,
                CoverCacheBootstrapState = coverCacheBootstrapResult?.State.ToString() ?? "",
                CreatedAt = DateTimeOffset.UtcNow
            },
            cancellationToken);
    }

    private async Task CompleteFirstRunPreparationOverlayAsync(
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

        var readiness = archiveReadinessResult?.Readiness ?? _runtimeShellState.LatestArchiveReadiness;
        if (archiveWarmupState == ArchiveReadinessWarmupState.Completed
            && AreRequiredStartupArchivesReady(readiness)
            && IsCoverCacheBootstrapReady(coverCacheBootstrapResult.State))
        {
            await SaveFirstRunCompletedMarkerAsync(CancellationToken.None, coverCacheBootstrapResult);
            return;
        }

        await ShowFirstRunPreparationFailureAsync(CancellationToken.None);
    }

    private async Task<CoverCacheBootstrapResult> StartCoverCacheBootstrapForColdStartAsync(CancellationToken cancellationToken)
    {
        UpdateStartupPreparationState(state => state with
        {
            CoverCacheBootstrapState = CoverCacheBootstrapState.Pending
        });

        var progress = new Progress<CoverCacheBootstrapState>(
            nextState => UpdateStartupPreparationState(state => state with
            {
                CoverCacheBootstrapState = nextState
            }));

        try
        {
            var result = await _coverCacheBootstrapService.BootstrapAsync(progress, cancellationToken);
            UpdateStartupPreparationState(state => state with
            {
                CoverCacheBootstrapState = result.State
            });
            LogInfo(
                MainViewModelLogCategories.App,
                $"cover_cache_bootstrap completed state={NormalizeStatusCode(result.State.ToString(), "unknown")} attempted={(result.Attempted ? "true" : "false")} copied_files={result.CopiedFileCount} error={NormalizeStatusCode(result.ErrorCode, "none")}");
            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            LogWarning(MainViewModelLogCategories.App, "cover_cache_bootstrap skipped reason=canceled");
            throw;
        }
        catch (Exception ex)
        {
            LogWarning(MainViewModelLogCategories.App, $"cover_cache_bootstrap fallback_enabled type={ex.GetType().Name}");
            var fallback = CoverCacheBootstrapResult.FailedFallbackEnabled("cover_cache_bootstrap_failed");
            UpdateStartupPreparationState(state => state with
            {
                CoverCacheBootstrapState = fallback.State
            });
            return fallback;
        }
    }

    private async Task<CoverCacheBootstrapResult> ResolveCoverCacheBootstrapCompletionAsync(
        Task<CoverCacheBootstrapResult>? coverCacheBootstrapTask,
        CancellationTokenSource? coverCacheBootstrapCancellation,
        ArchiveReadinessWarmupState archiveWarmupState,
        ArchiveReadinessFlowResult? archiveReadinessResult)
    {
        if (coverCacheBootstrapTask is null)
        {
            return CoverCacheBootstrapResult.NotRequired();
        }

        var readiness = archiveReadinessResult?.Readiness ?? _runtimeShellState.LatestArchiveReadiness;
        if (archiveWarmupState == ArchiveReadinessWarmupState.Completed
            && AreRequiredStartupArchivesReady(readiness))
        {
            try
            {
                return await coverCacheBootstrapTask;
            }
            catch (OperationCanceledException)
            {
                return CoverCacheBootstrapResult.NotRequired();
            }
        }

        coverCacheBootstrapCancellation?.Cancel();
        try
        {
            await coverCacheBootstrapTask;
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            LogWarning(MainViewModelLogCategories.App, $"cover_cache_bootstrap abandoned_after_archive_failure type={ex.GetType().Name}");
        }

        return CoverCacheBootstrapResult.NotRequired();
    }

    private async Task ShowFirstRunPreparationFailureAsync(CancellationToken cancellationToken)
    {
        await _dialogPresenter.ShowSafelyAsync(
            new AppDialogRequest
            {
                Kind = AppDialogKind.Blocking,
                Severity = DialogSeverity.Blocking,
                Title = Strings.FirstRunPreparationFailedTitle,
                Summary = Strings.FirstRunPreparationFailedSummary,
                IsBlocking = true,
                CanClose = false,
                CloseOnOverlayClick = false,
                PrimaryButtonText = Strings.DialogButtonOk
            },
            cancellationToken);
    }

    private static bool AreRequiredStartupArchivesReady(ArchiveReadinessSnapshot readiness)
    {
        // All install archives are startup-critical. Missing "optional" caches can still make a later
        // game-specific install fail after the user reaches the final button, so first-run warmup must
        // verify the complete archive set instead of only the currently selected game's plan.
        return readiness.AreAllStartupArchivesReady();
    }

    private static bool IsCoverCacheBootstrapReady(CoverCacheBootstrapState state)
    {
        return state is CoverCacheBootstrapState.NotRequired
            or CoverCacheBootstrapState.Completed
            or CoverCacheBootstrapState.FailedFallbackEnabled;
    }

    private void StartStartupDialogsInBackground()
    {
        var cancellationTokenSource = _startupBackgroundTaskManager.CreateSource();
        _ = RunStartupDialogsInBackgroundAsync(cancellationTokenSource);
    }

    private async Task RunStartupDialogsInBackgroundAsync(CancellationTokenSource cancellationTokenSource)
    {
        var cancellationToken = cancellationTokenSource.Token;
        var canceled = false;
        var failed = false;
        try
        {
            await ShowStartupAnnouncementIfNeededAsync(cancellationToken);
            await ShowStartupUpdateCheckDialogAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            canceled = true;
            LogWarning(MainViewModelLogCategories.Startup, "startup dialogs skipped reason=canceled");
        }
        catch (Exception ex)
        {
            failed = true;
            LogWarning(MainViewModelLogCategories.Startup, $"startup dialogs failed type={ex.GetType().Name}");
        }
        finally
        {
            UpdateStartupPreparationState(state => state with
            {
                StartupDialogsRunning = false,
                StartupDialogsCompleted = !canceled && !failed,
                StartupDialogsCanceled = canceled,
                StartupDialogsFailed = failed,
                LastErrorCode = failed
                    ? "startup_dialogs_failed"
                    : !canceled
                        ? ClearLastErrorCode(state.LastErrorCode, "startup_dialogs_failed")
                        : state.LastErrorCode
            });
            _startupBackgroundTaskManager.Remove(cancellationTokenSource);
        }
    }

    private void StartGameMasterCoverPrefetchInBackground()
    {
        if (_isGameMasterCoverPrefetchStarted)
        {
            return;
        }

        _isGameMasterCoverPrefetchStarted = true;
        UpdateStartupPreparationState(state => state with
        {
            GameMasterCoverPrefetchStarted = true,
            GameMasterCoverPrefetchRunning = true,
            GameMasterCoverPrefetchCompleted = false,
            GameMasterCoverPrefetchCanceled = false,
            GameMasterCoverPrefetchFailed = false
        });
        var cancellationTokenSource = _startupBackgroundTaskManager.CreateSource();
        _ = RunGameMasterCoverPrefetchInBackgroundAsync(cancellationTokenSource);
    }

    public void CancelBackgroundWork()
    {
        _startupBackgroundTaskManager.CancelAll();
    }

    private async Task RunGameMasterCoverPrefetchInBackgroundAsync(CancellationTokenSource cancellationTokenSource)
    {
        var cancellationToken = cancellationTokenSource.Token;
        var prefetchStopwatch = Stopwatch.StartNew();
        var canceled = false;
        var failed = false;
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);

            var gameMaster = _runtimeShellState.LatestRuntimeData.GameMaster;
            if (gameMaster.Count == 0)
            {
                LogInfo(MainViewModelLogCategories.Wiki, "cover_prefetch skipped reason=no_game_master");
                return;
            }

            var prefetchTargets = GameMasterCoverPrefetchService.CollectTargets(gameMaster);
            if (prefetchTargets.Count == 0)
            {
                LogInfo(MainViewModelLogCategories.Wiki, "cover_prefetch skipped reason=no_targets");
                return;
            }

            var prioritizedTargets = ResolveHomeCoverPrefetchTargets(gameMaster);

            if (prioritizedTargets.Length == 0)
            {
                LogInfo(MainViewModelLogCategories.Wiki, "cover_prefetch skipped reason=no_home_targets");
                return;
            }

            LogInfo(
                MainViewModelLogCategories.Wiki,
                $"cover_prefetch started total={prefetchTargets.Count} home_priority={prioritizedTargets.Length} scope=home_only remaining=not_prefetched");
            var homeStageStopwatch = Stopwatch.StartNew();
            var homeSummary = await _gameMasterCoverPrefetchService.PrefetchAsync(
                prioritizedTargets,
                [],
                cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            await RefreshHomeCoversOnDispatcherAsync();

            LogInfo(
                MainViewModelLogCategories.Wiki,
                $"cover_prefetch completed total={homeSummary.Total} cached={homeSummary.Cached} downloaded={homeSummary.Downloaded} skipped={homeSummary.Skipped} failed={homeSummary.Failed} duration_ms={homeStageStopwatch.ElapsedMilliseconds}");
        }
        catch (OperationCanceledException)
        {
            canceled = true;
            LogWarning(MainViewModelLogCategories.Wiki, $"cover_prefetch skipped reason=canceled duration_ms={prefetchStopwatch.ElapsedMilliseconds}");
        }
        catch (Exception ex)
        {
            failed = true;
            LogWarning(
                MainViewModelLogCategories.Wiki,
                $"cover_prefetch failed type={ex.GetType().Name} duration_ms={prefetchStopwatch.ElapsedMilliseconds}");
        }
        finally
        {
            UpdateStartupPreparationState(state => state with
            {
                GameMasterCoverPrefetchRunning = false,
                GameMasterCoverPrefetchCompleted = !canceled && !failed,
                GameMasterCoverPrefetchCanceled = canceled,
                GameMasterCoverPrefetchFailed = failed,
                LastErrorCode = failed
                    ? "cover_prefetch_failed"
                    : !canceled
                        ? ClearLastErrorCode(state.LastErrorCode, "cover_prefetch_failed")
                        : state.LastErrorCode
            });
            _startupBackgroundTaskManager.Remove(cancellationTokenSource);
        }
    }

    private void QueueHomeCoverPrefetchInBackground(string reason)
    {
        if (Games.Count == 0)
        {
            return;
        }

        var normalizedReason = NormalizeStatusCode(reason, "unknown");
        if (Interlocked.CompareExchange(ref _homeCoverPrefetchRunning, 1, 0) != 0)
        {
            LogInfo(MainViewModelLogCategories.Wiki, $"home_cover_prefetch skipped reason=already_running source={normalizedReason}");
            return;
        }

        var cancellationTokenSource = _startupBackgroundTaskManager.CreateSource();
        _ = RunHomeCoverPrefetchInBackgroundAsync(cancellationTokenSource, normalizedReason);
    }

    private async Task RunHomeCoverPrefetchInBackgroundAsync(
        CancellationTokenSource cancellationTokenSource,
        string reason)
    {
        var cancellationToken = cancellationTokenSource.Token;
        var normalizedReason = NormalizeStatusCode(reason, "unknown");
        try
        {
            var gameMaster = _runtimeShellState.LatestRuntimeData.GameMaster;
            if (gameMaster.Count == 0)
            {
                return;
            }

            var homeTargets = ResolveHomeCoverPrefetchTargets(gameMaster);
            if (homeTargets.Length == 0)
            {
                return;
            }

            LogInfo(
                MainViewModelLogCategories.Wiki,
                $"home_cover_prefetch started reason={normalizedReason} total={homeTargets.Length}");
            var stopwatch = Stopwatch.StartNew();
            var summary = await _gameMasterCoverPrefetchService.PrefetchAsync(
                homeTargets,
                [],
                cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            await RefreshHomeCoversOnDispatcherAsync();
            LogInfo(
                MainViewModelLogCategories.Wiki,
                $"home_cover_prefetch completed reason={normalizedReason} total={summary.Total} cached={summary.Cached} downloaded={summary.Downloaded} skipped={summary.Skipped} failed={summary.Failed} duration_ms={stopwatch.ElapsedMilliseconds}");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            LogWarning(MainViewModelLogCategories.Wiki, $"home_cover_prefetch skipped reason=canceled source={normalizedReason}");
        }
        catch (Exception ex)
        {
            LogWarning(MainViewModelLogCategories.Wiki, $"home_cover_prefetch failed reason={normalizedReason} type={ex.GetType().Name}");
        }
        finally
        {
            Interlocked.Exchange(ref _homeCoverPrefetchRunning, 0);
            _startupBackgroundTaskManager.Remove(cancellationTokenSource);
        }
    }

    private async Task WaitForStartupDialogsReadyAsync(CancellationToken cancellationToken)
    {
        Task readyTask;
        lock (_startupDialogsReadyGate)
        {
            readyTask = _startupDialogsReadyTask;
        }

        await readyTask.WaitAsync(cancellationToken);
    }

    private async Task RefreshHomeCoversOnDispatcherAsync()
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.HasShutdownStarted)
        {
            return;
        }

        await dispatcher.InvokeAsync(() =>
        {
            Home.RequestVisibleCoverLoad();
        }, DispatcherPriority.Background);
    }

    private GameMasterCoverPrefetchTarget[] ResolveHomeCoverPrefetchTargets(
        IReadOnlyList<RuntimeDataGameProfile> gameMaster)
    {
        var prefetchTargets = GameMasterCoverPrefetchService.CollectTargets(gameMaster);
        if (prefetchTargets.Count == 0)
        {
            return [];
        }

        var homeCardKeys = ResolveHomeCardCoverMatchKeys();
        if (homeCardKeys.Count == 0)
        {
            return [];
        }

        return [..prefetchTargets.Where(target => IsHomeCardCoverTarget(target, homeCardKeys))];
    }

    private HashSet<string> ResolveHomeCardCoverMatchKeys()
    {
        if (Games.Count == 0)
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var card in Games)
        {
            var sourceUrl = (card.CoverImageSource ?? "").Trim();
            if (!string.IsNullOrWhiteSpace(sourceUrl) && sourceUrl.StartsWith("pack://application:", StringComparison.OrdinalIgnoreCase) == false)
            {
                var cacheSourceUrl = ResolveHomeCardSourceUrl(card.SourceModel);
                if (!string.IsNullOrWhiteSpace(cacheSourceUrl))
                {
                    keys.Add(BuildHomeCardMatchKey("source", cacheSourceUrl));
                }
            }

            var sourceGameId = (card.SourceModel?.GameId ?? "").Trim();
            if (!string.IsNullOrWhiteSpace(sourceGameId))
            {
                keys.Add(BuildHomeCardMatchKey("gameId", sourceGameId));
            }

            var entryGameId = (card.GameEntry?.GameId ?? "").Trim();
            if (!string.IsNullOrWhiteSpace(entryGameId))
            {
                keys.Add(BuildHomeCardMatchKey("gameId", entryGameId));
            }

            var sourceModelSteamAppId = (card.SourceModel?.CoverSteamAppId ?? "").Trim();
            if (!string.IsNullOrWhiteSpace(sourceModelSteamAppId))
            {
                keys.Add(BuildHomeCardMatchKey("steam", sourceModelSteamAppId));
            }

            var coverUrl = (card.SourceModel?.CoverUrl ?? "").Trim();
            if (!string.IsNullOrWhiteSpace(coverUrl))
            {
                keys.Add(BuildHomeCardMatchKey("source", coverUrl));
            }
        }

        return keys;
    }

    private static bool IsHomeCardCoverTarget(GameMasterCoverPrefetchTarget target, HashSet<string> homeCardKeys)
    {
        if (homeCardKeys.Count == 0)
        {
            return false;
        }

        var gameId = (target.GameId ?? "").Trim();
        if (!string.IsNullOrWhiteSpace(gameId) && homeCardKeys.Contains(BuildHomeCardMatchKey("gameId", gameId)))
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(target.SteamAppId)
            && homeCardKeys.Contains(BuildHomeCardMatchKey("steam", target.SteamAppId)))
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(target.SourceUrl)
            && homeCardKeys.Contains(BuildHomeCardMatchKey("source", target.SourceUrl)))
        {
            return true;
        }

        return false;
    }

    private static string ResolveHomeCardSourceUrl(ShellGameCardModel? sourceModel)
    {
        if (sourceModel is null)
        {
            return "";
        }

        if (!string.IsNullOrWhiteSpace(sourceModel.CoverUrl))
        {
            return sourceModel.CoverUrl.Trim();
        }

        if (!string.IsNullOrWhiteSpace(sourceModel.CoverSteamAppId))
        {
            return CoverImageCacheService.BuildSteamCoverUrl(sourceModel.CoverSteamAppId);
        }

        return "";
    }

    private static string BuildHomeCardMatchKey(string category, string value)
    {
        return $"{category}::{(value ?? "").Trim()}";
    }

    private bool ShouldBlockStartupForUnsupportedOperatingSystem()
    {
        var operatingSystemState = EnsureOperatingSystemPolicyEvaluated();
        return operatingSystemState.IsUnsupportedWindows10;
    }

    private void SetCurrentView(ShellViewKind view)
    {
        if (view != ShellViewKind.Home)
        {
            ClearSelectedGameContext();
        }

        if (!_navigationState.SetCurrentView(view))
        {
            return;
        }
        OnPropertyChanged(nameof(CurrentViewKind));
        OnPropertyChanged(nameof(IsHomeViewActive));
        OnPropertyChanged(nameof(IsSupportedGamesWikiViewActive));
        OnPropertyChanged(nameof(IsScanViewActive));
        OnPropertyChanged(nameof(IsSettingsViewActive));
        Navigation.Refresh();
        RefreshNavigationAndScanCommandStates();
        if (view == ShellViewKind.SupportedGamesWiki)
        {
            EnsureSupportedGamesWikiLoadedForView();
            QueueSupportedGamesWikiVisibleCoverLoad(0, 0);
        }
    }

    private void ClearSelectedGameContext()
    {
        if (SelectedGame is null && _selectionState.PendingPopupRequests.Count == 0)
        {
            return;
        }

        Interlocked.Increment(ref _selectionRequestVersion);
        _selectionState = new ShellInstallSelectionState
        {
            MultiGpuBlocked = _gpuSelectionCoordinator.MultiGpuBlocked,
            GpuSelectionPending = _gpuSelectionCoordinator.GpuSelectionPending
        };
        SetSelectedGame(null);
        SelectedGameAction.ApplySelectionBridgeState(_selectionState);
    }

    public async Task RefreshRuntimeContextAsync(CancellationToken cancellationToken = default)
    {
        await _runtimeContextCoordinator.RefreshAsync(
            new RuntimeContextCoordinatorRequest
            {
                Strings = Strings,
                SelectionState = _selectionState,
                ResolveRuntimeContextForGpuSelectionAsync = ResolveRuntimeContextForGpuSelectionAsync,
                ApplyRuntimeSummaryStateUpdate = ApplyRuntimeSummaryStateUpdate,
                ApplySelectionState = ApplySelectionStateAfterRuntimeContextRefresh,
                LogCategory = MainViewModelLogCategories.Runtime
            },
            cancellationToken);
    }

    private void ApplySelectionStateAfterRuntimeContextRefresh(ShellInstallSelectionState selectionState)
    {
        _selectionState = selectionState;
        SelectedGameAction.ApplySelectionBridgeState(_selectionState);
    }

    public async Task RefreshDeviceIdentityRulesAsync(CancellationToken cancellationToken = default)
    {
        await _deviceRulesRefreshLock.TryRunExclusiveAsync(
            async ct =>
        {
            var result = await _deviceIdentityRulesFlowController.RefreshAsync(ct);
            _flowLogDispatcher.Dispatch(result.Logs, MainViewModelLogCategories.Runtime);
            if (!result.DidRun || !result.IsSuccess)
            {
                return;
            }

            ApplyRuntimeSummaryStateUpdate(_runtimeSummaryStateController.Build(_runtimeShellState.LatestRuntimeContext, Strings));
        },
            cancellationToken);
    }

    public async Task RefreshRuntimeDataCatalogAsync(CancellationToken cancellationToken = default)
    {
        await RefreshRuntimeDataCatalogAsync(RuntimeCatalogRefreshMode.Inline, cancellationToken);
    }

    private async Task RefreshRuntimeDataCatalogAsync(
        RuntimeCatalogRefreshMode refreshMode,
        CancellationToken cancellationToken = default)
    {
        await _runtimeCatalogCoordinator.RefreshAsync(
            new RuntimeCatalogCoordinatorRequest
            {
                HasGameCardFactory = _shellGameCardViewModelFactory is not null,
                IsMultiGpuBlocked = _gpuSelectionCoordinator.MultiGpuBlocked,
                IsGpuSelectionPending = _gpuSelectionCoordinator.GpuSelectionPending,
                IsGpuManifestRestartRequired = _gpuManifestRestartRequired,
                LatestRemoteCatalogDetailErrorCode = _runtimeShellState.LatestRemoteCatalogDetailErrorCode,
                GpuManifestRestartRequiredErrorCode = GpuManifestRestartRequiredErrorCode,
                LatestRuntimeContext = _runtimeShellState.LatestRuntimeContext,
                SelectedLanguage = SelectedLanguage,
                Strings = Strings,
                RefreshMode = refreshMode,
                BuildRuntimeCatalogRequest = _flowRequestFactory.BuildRuntimeCatalogRequest,
                ApplyMultiGpuBlockedUiState = ApplyMultiGpuBlockedUiState,
                ApplyGpuManifestRestartRequiredState = ApplyGpuManifestRestartRequiredState,
                ShowGpuManifestRestartRequiredDialogOnceAsync = ShowGpuManifestRestartRequiredDialogOnceAsync,
                ApplySettingsStatusText = value => SettingsStatusText = value,
                ApplyRuntimeCatalogFlowResultAsync = ApplyRuntimeCatalogFlowResultAsync
            },
            cancellationToken);
    }

    private async Task ApplyRuntimeCatalogFlowResultAsync(
        RuntimeCatalogFlowResult result,
        RuntimeCatalogRefreshMode refreshMode,
        CancellationToken cancellationToken)
    {
        _flowLogDispatcher.Dispatch(result.Logs, MainViewModelLogCategories.Runtime);
        var update = _resultApplier.CreateRuntimeCatalogStateUpdate(
            result,
            NormalizeStatusCode(result.ErrorCode, MainViewModelStatusCodes.RuntimeDataFailed));
        ApplyStateUpdate(update);

        if (result.IsSuccess)
        {
            if (update.ShouldResetRemoteCatalogDialogGate)
            {
                _remoteCatalogDialogGate.Reset();
            }

            if (update.ShouldRefreshVisibleGames)
            {
                RefreshVisibleGamesFromScanMatches();
            }

            if (result.ShouldApplyRemoteDataState && SupportedGames.HasEntries)
            {
                RebuildSupportedGamesWikiRows();
            }

            if (update.ShouldRefreshArchiveReadiness)
            {
                if (refreshMode == RuntimeCatalogRefreshMode.BackgroundWarmup)
                {
                    StartArchiveReadinessWarmupInBackground();
                }
                else
                {
                    await RefreshArchiveReadinessAsync(cancellationToken);
                }
            }

            LogInfo(MainViewModelLogCategories.Remote, $"remote catalog loaded games={_runtimeShellState.LatestRemoteCatalog.Games.Count} visible_games={Games.Count}");
            return;
        }

        // Keep install strictly blocked when remote catalog (including GPU bundle) is not healthy.
        if (!string.IsNullOrWhiteSpace(_runtimeShellState.LatestRemoteCatalogErrorCode))
        {
            _selectionState = _selectionState with
            {
                SheetLoading = false,
                SheetReady = false,
                InstallButtonPresentation = new InstallButtonPresentation
                {
                    IsEnabled = false,
                    ShowInstalling = false,
                    IsLoadingBlinkReason = false,
                    ReasonCode = InstallButtonReasonCodes.SheetNotReady,
                    Text = ""
                }
            };
            SelectedGameAction.ApplySelectionBridgeState(_selectionState);
        }

        if (update.DialogRequest is not null)
        {
            await ShowRemoteCatalogDialogOnceAsync(update.DialogRequest, cancellationToken);
        }
    }

    private void ApplyRuntimeSummaryStateUpdate(RuntimeSummaryStateUpdate update)
    {
        _runtimeShellState.ApplyRuntimeSummary(update);
        RuntimeHeader.Apply(update);

        if (update.HasSelectedGpu)
        {
            LogInfo(
                MainViewModelLogCategories.Runtime,
                $"selected_gpu vendor={NormalizeStatusCode(update.SelectedGpuVendor, MainViewModelStatusCodes.Unknown)} name=\"{NormalizeStatusCode(update.SelectedGpuName, MainViewModelStatusCodes.Unknown)}\" source={NormalizeStatusCode(_gpuSelectionCoordinator.SelectedGpuLogSource, "runtime_context_selected")}");
            LogInfo(
                MainViewModelLogCategories.Runtime,
                $"gpu_logo source={(string.IsNullOrWhiteSpace(update.GpuLogoSource) ? "none" : NormalizeStatusCode(update.GpuLogoSource, "none"))}");
        }
        else
        {
            LogWarning(MainViewModelLogCategories.Runtime, "selected_gpu missing reason=no_supported_gpu");
            LogWarning(MainViewModelLogCategories.Runtime, "gpu_logo skipped reason=selected_gpu_missing");
        }
    }

    private async Task<RuntimeContext> ResolveRuntimeContextForGpuSelectionAsync(
        RuntimeContext context,
        CancellationToken cancellationToken)
    {
        return await _gpuSelectionCoordinator.ResolveAsync(
            new GpuSelectionCoordinatorRequest
            {
                Context = context ?? new RuntimeContext(),
                ResolveSupportedGpuCandidatesAsync = ResolveManifestSupportedGpuCandidatesAsync,
                PromptDualGpuSelectionAsync = PromptDualGpuSelectionAsync,
                ShowMultiGpuBlockedPopupAsync = ShowMultiGpuBlockedPopupAsync,
                ApplyMultiGpuBlockedUiState = ApplyMultiGpuBlockedUiState,
                ReadManifestRestartRequired = () => _gpuManifestRestartRequired,
                LogInfo = message => LogInfo(MainViewModelLogCategories.Runtime, message),
                LogWarning = message => LogWarning(MainViewModelLogCategories.Runtime, message)
            },
            cancellationToken);
    }

    private async Task ShowMultiGpuBlockedPopupAsync(CancellationToken cancellationToken)
    {
        await _dialogPresenter.ShowSafelyAsync(
            new AppDialogRequest
            {
                Kind = AppDialogKind.Blocking,
                Severity = DialogSeverity.Warning,
                Title = Strings.GpuUnsupportedConfigurationTitle,
                Summary = Strings.GpuUnsupportedConfigurationSummary,
                IsBlocking = true,
                CloseOnOverlayClick = false
            },
            cancellationToken);
    }

    private void ApplyMultiGpuBlockedUiState()
    {
        _scannedGameState.Clear();

        if (Games.Count > 0)
        {
            ReplaceGameCards([]);
        }
        else
        {
            SetSelectedGame(null);
        }

        _selectionState = new ShellInstallSelectionState
        {
            MultiGpuBlocked = true,
            GpuSelectionPending = false,
            InstallButtonPresentation = new InstallButtonPresentation
            {
                IsEnabled = false,
                ShowInstalling = false,
                IsLoadingBlinkReason = false,
                ReasonCode = InstallButtonReasonCodes.MultiGpuBlocked,
                Text = ""
            }
        };
        SelectedGameAction.ApplySelectionBridgeState(_selectionState);
        ScanStatusText = Strings.ScanBlockedUnsupportedGpuConfiguration;
    }

    private void ApplyGpuManifestRestartRequiredState(string detailErrorCode)
    {
        var normalizedDetailCode = NormalizeStatusCode(detailErrorCode, GpuManifestRestartRequiredErrorCode);
        _gpuManifestRestartRequired = true;
        _runtimeShellState.SetRemoteCatalogError(GpuManifestRestartRequiredErrorCode, normalizedDetailCode);
        SettingsStatusText = Format(Strings.RuntimeRemoteCatalogFailed, GpuManifestRestartRequiredErrorCode);
        ScanStatusText = Format(Strings.RuntimeCatalogNotReadyForScan, GpuManifestRestartRequiredErrorCode);
        _scannedGameState.Clear();
        if (Games.Count > 0)
        {
            ReplaceGameCards([], observeAutoSelection: false);
        }

        _selectionState = _selectionState with
        {
            SheetLoading = false,
            SheetReady = false,
            InstallButtonPresentation = new InstallButtonPresentation
            {
                IsEnabled = false,
                ShowInstalling = false,
                IsLoadingBlinkReason = false,
                ReasonCode = InstallButtonReasonCodes.SheetNotReady,
                Text = ""
            }
        };
        SelectedGameAction.ApplySelectionBridgeState(_selectionState);
    }

    private void ClearGpuManifestRestartRequiredState()
    {
        _gpuManifestRestartRequired = false;
        _gpuManifestRestartDialogShown = false;
        if (string.Equals(_runtimeShellState.LatestRemoteCatalogErrorCode, GpuManifestRestartRequiredErrorCode, StringComparison.OrdinalIgnoreCase))
        {
            _runtimeShellState.SetRemoteCatalogError("", "");
        }
    }

    private async Task ShowGpuManifestRestartRequiredDialogOnceAsync(string errorCode, CancellationToken cancellationToken)
    {
        if (_gpuManifestRestartDialogShown)
        {
            return;
        }

        _gpuManifestRestartDialogShown = true;
        var normalizedCode = NormalizeStatusCode(errorCode, GpuManifestRestartRequiredErrorCode);
        await _dialogPresenter.ShowSafelyAsync(
            new AppDialogRequest
            {
                Kind = AppDialogKind.Blocking,
                Severity = DialogSeverity.Blocking,
                Title = Strings.RuntimeCatalogFailedTitle,
                Summary = Strings.RuntimeCatalogFailedSummary,
                BulletItems =
                [
                    Strings.RuntimeCatalogFailedBullet1,
                    Strings.RuntimeCatalogFailedBullet2,
                    $"Error code: {normalizedCode}"
                ],
                ErrorCode = normalizedCode,
                IsBlocking = true,
                CanClose = false,
                CloseOnOverlayClick = false,
                PrimaryButtonText = Strings.DialogButtonOk
            },
            cancellationToken);
    }

    private async Task<IReadOnlyList<GpuInfo>> ResolveManifestSupportedGpuCandidatesAsync(
        RuntimeContext runtimeContext,
        IReadOnlyList<GpuInfo> detectedCandidates,
        CancellationToken cancellationToken)
    {
        if (detectedCandidates.Count == 0)
        {
            return Array.Empty<GpuInfo>();
        }

        var remote = runtimeContext.RemoteData ?? new RemoteDataOptions();
        var manifestEndpoint = (remote.GpuBundleManifestUrl ?? "").Trim();
        if (string.IsNullOrWhiteSpace(manifestEndpoint))
        {
            if (!remote.AllowMockGpuManifestFallback)
            {
                const string code = "gpu_bundle_manifest_endpoint_missing";
                LogWarning(
                    MainViewModelLogCategories.Runtime,
                    $"gpu manifest endpoint missing code={code} restart_required=true");
                ApplyGpuManifestRestartRequiredState(code);
                await ShowGpuManifestRestartRequiredDialogOnceAsync(code, cancellationToken);
                return Array.Empty<GpuInfo>();
            }

            // Empty manifest endpoints are allowed only for local/dev mock contexts.
            LogWarning(
                MainViewModelLogCategories.Runtime,
                "gpu manifest endpoint missing; fallback to detected_gpu_candidates");
            return detectedCandidates;
        }

        var manifestRequest = BuildGpuBundleManifestFetchRequest(runtimeContext);
        var manifestResult = await _gpuBundleManifestClient.FetchAsync(
            manifestEndpoint,
            manifestRequest,
            cancellationToken);
        if (!manifestResult.IsSuccess)
        {
            var code = manifestResult.IsSkipped
                ? "gpu_bundle_manifest_skipped"
                : NormalizeStatusCode(manifestResult.ErrorCode, "gpu_bundle_manifest_failed");
            LogWarning(
                MainViewModelLogCategories.Runtime,
                $"gpu manifest fetch failed code={code} restart_required=true");
            ApplyGpuManifestRestartRequiredState(code);
            await ShowGpuManifestRestartRequiredDialogOnceAsync(code, cancellationToken);
            return Array.Empty<GpuInfo>();
        }

        ClearGpuManifestRestartRequiredState();
        var supported = new List<GpuInfo>(detectedCandidates.Count);
        foreach (var candidate in detectedCandidates)
        {
            var match = _gpuBundleManifestRuleResolver.Resolve(
                manifestResult.Manifest,
                runtimeContext with { SelectedGpu = candidate });
            if (match.IsMatched && !match.IsUnsupported)
            {
                supported.Add(candidate);
                continue;
            }

            var code = NormalizeStatusCode(match.ErrorCode, "bundle_rule_not_matched");
            LogInfo(
                MainViewModelLogCategories.Runtime,
                $"gpu candidate excluded vendor={NormalizeStatusCode(candidate.Vendor, MainViewModelStatusCodes.Unknown)} name=\"{NormalizeStatusCode(candidate.Name, MainViewModelStatusCodes.Unknown)}\" code={code}");
        }

        return supported;
    }

    private GpuBundleManifestFetchRequest BuildGpuBundleManifestFetchRequest(RuntimeContext runtimeContext)
    {
        var selectedGpu = runtimeContext.SelectedGpu
                          ?? runtimeContext.Gpus?.FirstOrDefault(static gpu => gpu.IsPrimary)
                          ?? runtimeContext.Gpus?.FirstOrDefault()
                          ?? new GpuInfo();
        return new GpuBundleManifestFetchRequest
        {
            Vendor = GpuSelectionCoordinator.NormalizeVendorForManifestRequest(selectedGpu.Vendor, selectedGpu.Name),
            GpuRaw = GpuSelectionCoordinator.NormalizeWhitespace(selectedGpu.Name),
            DeviceManufacturer = GpuSelectionCoordinator.NormalizeWhitespace(runtimeContext.Device?.Manufacturer ?? ""),
            DeviceModel = GpuSelectionCoordinator.NormalizeWhitespace(runtimeContext.Device?.Model ?? ""),
            RequestSource = "app",
            AppVersion = GetCurrentAppVersion()
        };
    }

    private async Task<GpuInfo?> PromptDualGpuSelectionAsync(
        GpuInfo firstGpu,
        GpuInfo secondGpu,
        CancellationToken cancellationToken)
    {
        var request = new AppDialogRequest
        {
            Kind = AppDialogKind.GpuSelection,
            Severity = DialogSeverity.Warning,
            Title = IsKoreanUi ? "GPU 선택" : "GPU Selection",
            Summary = IsKoreanUi
                ? "듀얼 GPU가 감지되었습니다.\nOptiScaler를 어떤 GPU 기준으로 설치할지 선택해 주세요.\n선택한 GPU 기준으로 설치됩니다.\n선택한 GPU와 다른 GPU로 게임을 실행하면 정상 동작하지 않을 수 있습니다."
                : "Dual GPUs were detected.\nSelect which GPU OptiScaler should be installed for.\nInstallation will use settings for the selected GPU.\nIt may not work correctly if the game is run on the other GPU.",
            PrimaryButtonText = GpuSelectionCoordinator.BuildGpuSelectionButtonText(firstGpu, 1),
            SecondaryButtonText = GpuSelectionCoordinator.BuildGpuSelectionButtonText(secondGpu, 2),
            PrimaryResult = AppDialogResult.Continue,
            SecondaryResult = AppDialogResult.Cancel,
            IsBlocking = true,
            CanClose = false,
            CloseOnOverlayClick = false
        };

        var result = await _dialogPresenter.ShowSafelyAsync(request, cancellationToken);
        if (result == AppDialogResult.Continue)
        {
            return firstGpu;
        }

        if (result == AppDialogResult.Cancel)
        {
            return secondGpu;
        }

        return null;
    }
}
