using Velopack;
using Velopack.Sources;

namespace OptiClick.Wpf.Services;

public interface IVelopackAppUpdateService
{
    Task<AppUpdateInfo?> CheckForUpdateAsync(CancellationToken cancellationToken = default);

    Task ApplyUpdateAndRestartAsync(AppUpdateInfo updateInfo, CancellationToken cancellationToken = default);
}

public sealed class VelopackAppUpdateService : IVelopackAppUpdateService
{
    public const string GithubRepoUrl = "https://github.com/onehoon/OptiClick";

    private readonly Lazy<UpdateManager?> _updateManager;

    public VelopackAppUpdateService(string githubRepoUrl = GithubRepoUrl, bool includePreReleases = false)
    {
        _updateManager = new Lazy<UpdateManager?>(() =>
        {
            try
            {
                var source = new GithubSource(githubRepoUrl, null, includePreReleases);
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
        if (updateManager is null || !updateManager.IsInstalled)
        {
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
}
