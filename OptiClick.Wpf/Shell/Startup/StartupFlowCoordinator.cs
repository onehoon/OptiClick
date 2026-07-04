using System.Diagnostics;

namespace OptiClick.Wpf.Shell.Startup;

public sealed class StartupFlowCoordinator
{
    public async Task RunInitialStartupAsync(
        StartupFlowRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var startupStopwatch = Stopwatch.StartNew();
        request.LogInfo("startup initialization started");
        request.LogInfo($"start version={request.AppVersion}");
        request.LogInfo($"local_data_root={request.LocalDataRoot}");
        request.LogInfo($"log_directory={request.LogDirectory}");
        request.LogInfo($"cache_archives={request.CacheArchivesDirectory}");
        request.LogInfo($"cache_manifest={request.CacheManifestDirectory}");
        request.LogInfo($"cache_payload={request.CachePayloadDirectory}");

        await request.WaitForStartupDialogsReadyAsync(cancellationToken);
        request.LogInfo("milestone startup_overlay_ready");
        request.StartStartupUpdateCheckInBackground();
        request.LogInfo("milestone app_update_background_started");

        var runtimeContextStopwatch = Stopwatch.StartNew();
        await request.RefreshRuntimeContextAsync(cancellationToken);
        request.LogInfo("milestone runtime_context_completed");
        request.LogInfo("startup runtime context completed");
        request.LogInfo($"startup runtime context duration_ms={runtimeContextStopwatch.ElapsedMilliseconds}");

        var runtimeCatalogStopwatch = Stopwatch.StartNew();
        await request.RefreshRuntimeDataCatalogForStartupAsync(cancellationToken);
        request.LogInfo("milestone runtime_catalog_completed");
        request.LogInfo("startup runtime catalog completed");
        request.LogInfo($"startup runtime catalog duration_ms={runtimeCatalogStopwatch.ElapsedMilliseconds}");
        request.StartStartupAnnouncementInBackground();
        request.LogInfo("milestone startup_announcement_background_started");

        // Device identity rules are best-effort and cache-oriented for startup.
        // Apply cached rules immediately, then keep remote sync in the background.
        var startupScanTask = RunStartupAutoScanWithLogsAsync(request, cancellationToken);
        var deviceRulesTask = ApplyCachedDeviceIdentityRulesWithLogsAsync(request, cancellationToken);
        await Task.WhenAll(startupScanTask, deviceRulesTask);

        request.LogInfo("milestone interactive_ready");
        request.StartDeviceIdentityRulesRefreshInBackground?.Invoke();
        request.LogInfo("milestone device_rules_refresh_background_started");
        request.StartSupportedGamesWikiRefreshInBackground();
        request.LogInfo("milestone wiki_background_started");
        request.StartGameMasterCoverPrefetchInBackground();
        request.LogInfo("milestone cover_prefetch_background_started");
        request.LogInfo("startup background tasks started");
        request.LogInfo("startup initialization completed");
        request.LogInfo($"startup initialization duration_ms={startupStopwatch.ElapsedMilliseconds}");
    }

    private static async Task RunStartupAutoScanWithLogsAsync(
        StartupFlowRequest request,
        CancellationToken cancellationToken)
    {
        var startupScanStopwatch = Stopwatch.StartNew();
        await request.RunStartupAutoScanAsync(cancellationToken);
        request.LogInfo("milestone startup_scan_completed");
        request.LogInfo("startup auto scan completed");
        request.LogInfo($"startup auto scan duration_ms={startupScanStopwatch.ElapsedMilliseconds}");
    }

    private static async Task ApplyCachedDeviceIdentityRulesWithLogsAsync(
        StartupFlowRequest request,
        CancellationToken cancellationToken)
    {
        var deviceRulesStopwatch = Stopwatch.StartNew();
        await request.RefreshDeviceIdentityRulesAsync(cancellationToken);
        request.LogInfo("milestone device_rules_cache_completed");
        request.LogInfo("startup device identity rules cache completed");
        request.LogInfo($"startup device identity rules duration_ms={deviceRulesStopwatch.ElapsedMilliseconds}");
    }
}
