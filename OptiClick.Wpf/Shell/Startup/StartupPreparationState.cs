namespace OptiClick.Wpf.Shell.Startup;

public sealed record StartupPreparationState
{
    public static StartupPreparationState Empty { get; } = new();

    public bool RuntimeContextCompleted { get; init; }
    public bool RuntimeCatalogCompleted { get; init; }
    public bool StartupScanCompleted { get; init; }
    public bool DeviceIdentityRulesCompleted { get; init; }

    public bool StartupDialogsStarted { get; init; }
    public bool StartupDialogsRunning { get; init; }
    public bool StartupDialogsCompleted { get; init; }
    public bool StartupDialogsCanceled { get; init; }
    public bool StartupDialogsFailed { get; init; }

    public bool SupportedGamesWikiRefreshStarted { get; init; }
    public bool SupportedGamesWikiRefreshRunning { get; init; }
    public bool SupportedGamesWikiRefreshCompleted { get; init; }
    public bool SupportedGamesWikiRefreshCanceled { get; init; }
    public bool SupportedGamesWikiRefreshFailed { get; init; }

    public bool GameMasterCoverPrefetchStarted { get; init; }
    public bool GameMasterCoverPrefetchRunning { get; init; }
    public bool GameMasterCoverPrefetchCompleted { get; init; }
    public bool GameMasterCoverPrefetchCanceled { get; init; }
    public bool GameMasterCoverPrefetchFailed { get; init; }

    public CoverCacheBootstrapState CoverCacheBootstrapState { get; init; } = CoverCacheBootstrapState.NotRequired;
    public bool CoverCacheBootstrapReady =>
        CoverCacheBootstrapState is CoverCacheBootstrapState.NotRequired
            or CoverCacheBootstrapState.Completed
            or CoverCacheBootstrapState.FailedFallbackEnabled;

    public ArchiveReadinessWarmupState ArchiveWarmupState { get; init; } = ArchiveReadinessWarmupState.NotStarted;
    public bool ArchiveWarmupRunning => ArchiveWarmupState == ArchiveReadinessWarmupState.Running;
    public bool ArchiveWarmupCompleted => ArchiveWarmupState == ArchiveReadinessWarmupState.Completed;
    public bool ArchiveWarmupCanceled => ArchiveWarmupState == ArchiveReadinessWarmupState.Canceled;
    public bool ArchiveWarmupFailed => ArchiveWarmupState == ArchiveReadinessWarmupState.Failed;

    public string LastErrorCode { get; init; } = "";
}
