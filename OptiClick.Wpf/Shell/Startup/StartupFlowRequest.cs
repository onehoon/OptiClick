namespace OptiClick.Wpf.Shell.Startup;

public sealed class StartupFlowRequest
{
    public string AppVersion { get; init; } = "";
    public string LocalDataRoot { get; init; } = "";
    public string LogDirectory { get; init; } = "";
    public string CacheArchivesDirectory { get; init; } = "";
    public string CacheManifestDirectory { get; init; } = "";
    public string CachePayloadDirectory { get; init; } = "";

    public required Func<CancellationToken, Task> RefreshRuntimeContextAsync { get; init; }
    public required Func<CancellationToken, Task> RefreshRuntimeDataCatalogForStartupAsync { get; init; }
    public required Func<CancellationToken, Task> WaitForStartupDialogsReadyAsync { get; init; }
    public required Func<CancellationToken, Task> RunStartupAutoScanAsync { get; init; }
    public required Func<CancellationToken, Task> RefreshDeviceIdentityRulesAsync { get; init; }

    public required Action StartStartupDialogsInBackground { get; init; }
    public required Action StartSupportedGamesWikiRefreshInBackground { get; init; }
    public required Action StartGameMasterCoverPrefetchInBackground { get; init; }

    public required Action<string> LogInfo { get; init; }
}
