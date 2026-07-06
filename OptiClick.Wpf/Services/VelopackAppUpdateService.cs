using Velopack;
using Velopack.Locators;
using Velopack.Sources;
using OptiClick.Wpf.Logging;

namespace OptiClick.Wpf.Services;

public interface IVelopackAppUpdateService
{
    Task<AppUpdateInfo?> CheckForUpdateAsync(CancellationToken cancellationToken = default);

    Task ApplyUpdateAndRestartAsync(AppUpdateInfo updateInfo, CancellationToken cancellationToken = default);
}

public sealed class VelopackAppUpdateService : IVelopackAppUpdateService
{
    public const string GithubRepoUrl = "https://github.com/onehoon/OptiClick";
    public const string PreviewChannelMarker = "preview";

    private readonly IAppLogger _logger;
    private readonly Lazy<UpdateManager?> _updateManager;

    public VelopackAppUpdateService(
        string githubRepoUrl = GithubRepoUrl,
        bool? includePreReleases = null,
        IAppLogger? logger = null)
    {
        _logger = logger ?? NullAppLogger.Instance;
        _updateManager = new Lazy<UpdateManager?>(() =>
        {
            try
            {
                var resolvedIncludePreReleases = includePreReleases ?? IsRunningPreviewChannel();
                var source = new GithubSource(githubRepoUrl, null, resolvedIncludePreReleases);
                return new UpdateManager(source);
            }
            catch (InvalidOperationException)
            {
                // No Velopack locator is available outside of an installed app process
                // (e.g. unit tests, dev-time runs before VelopackApp.Build().Run() executes).
                return null;
            }
        });
    }

    public async Task<AppUpdateInfo?> CheckForUpdateAsync(CancellationToken cancellationToken = default)
    {
        var updateManager = _updateManager.Value;
        if (updateManager is null)
        {
            _logger.Info("app-update", "update check skipped reason=velopack_locator_unavailable");
            return null;
        }

        if (!updateManager.IsInstalled)
        {
            _logger.Info("app-update", "update check skipped reason=not_velopack_installed");
            return null;
        }

        var velopackUpdateInfo = await updateManager.CheckForUpdatesAsync();
        if (velopackUpdateInfo is null)
        {
            return null;
        }

        return new AppUpdateInfo(
            updateManager.CurrentVersion?.ToString() ?? "",
            velopackUpdateInfo.TargetFullRelease.Version.ToString(),
            velopackUpdateInfo.TargetFullRelease.NotesMarkdown,
            velopackUpdateInfo);
    }

    public async Task ApplyUpdateAndRestartAsync(AppUpdateInfo updateInfo, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(updateInfo);

        var updateManager = _updateManager.Value
            ?? throw new InvalidOperationException("Velopack update manager is unavailable in this process.");

        await updateManager.DownloadUpdatesAsync(updateInfo.VelopackUpdateInfo, cancelToken: cancellationToken);
        updateManager.ApplyUpdatesAndRestart(
            updateInfo.VelopackUpdateInfo.TargetFullRelease,
            [AppUpdateStartupArguments.ForegroundAfterUpdate]);
    }

    private static bool IsRunningPreviewChannel()
    {
        // The channel is recorded on-disk at install time (via `vpk pack --channel`), so this
        // reflects which build the user actually has installed rather than a manual override.
        var channel = VelopackLocator.Current?.Channel ?? "";
        return channel.Contains(PreviewChannelMarker, StringComparison.OrdinalIgnoreCase);
    }
}
