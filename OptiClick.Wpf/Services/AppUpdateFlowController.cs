namespace OptiClick.Wpf.Services;

public sealed class AppUpdateFlowController
{
    private readonly IAppUpdateService _appUpdateService;
    private readonly IAppUpdateExecutionService _appUpdateExecutionService;
    private readonly IExternalUrlLauncher _externalUrlLauncher;
    private readonly AppUpdateDialogPresenter _dialogPresenter;

    public AppUpdateFlowController(
        IAppUpdateService appUpdateService,
        IAppUpdateExecutionService appUpdateExecutionService,
        IExternalUrlLauncher externalUrlLauncher,
        AppUpdateDialogPresenter? dialogPresenter = null)
    {
        _appUpdateService = appUpdateService ?? throw new ArgumentNullException(nameof(appUpdateService));
        _appUpdateExecutionService = appUpdateExecutionService ?? throw new ArgumentNullException(nameof(appUpdateExecutionService));
        _externalUrlLauncher = externalUrlLauncher ?? throw new ArgumentNullException(nameof(externalUrlLauncher));
        _dialogPresenter = dialogPresenter ?? new AppUpdateDialogPresenter();
    }

    public AppUpdateCheckResult CheckForUpdate(AppUpdateFlowRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Strings);

        var logs = new List<AppUpdateFlowLogEntry>();
        if (!_appUpdateService.TryResolveUpdate(request.LatestRuntimeData, request.CurrentVersion, out var updateInfo)
            || updateInfo is null)
        {
            var noUpdateDialog = _dialogPresenter.BuildNoUpdateDialog(request.CurrentVersion, request.Strings);
            logs.Add(Info($"no update available current={request.CurrentVersion}"));
            return new AppUpdateCheckResult
            {
                IsUpdateAvailable = false,
                StatusText = noUpdateDialog.Summary,
                DialogRequest = noUpdateDialog,
                Logs = logs
            };
        }

        logs.Add(Info($"update available current={updateInfo.CurrentVersion} latest={updateInfo.LatestVersion}"));
        return new AppUpdateCheckResult
        {
            IsUpdateAvailable = true,
            UpdateInfo = updateInfo,
            DialogRequest = _dialogPresenter.BuildUpdateAvailableDialog(updateInfo, request.Strings),
            Logs = logs
        };
    }

    public async Task<AppUpdateExecutionFlowResult> ExecuteConfirmedUpdateAsync(
        AppUpdateConfirmedRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Strings);
        ArgumentNullException.ThrowIfNull(request.UpdateInfo);

        var logs = new List<AppUpdateFlowLogEntry>();
        var prepareResult = await _appUpdateExecutionService.PrepareAndCopyUpdateAsync(request.UpdateInfo, cancellationToken);
        if (!prepareResult.IsSuccess || prepareResult.PreparedFile is null)
        {
            var logCode = NormalizeStatusCode(prepareResult.ErrorCode, "failed");
            var dialogCode = NormalizeStatusCode(prepareResult.ErrorCode, "unknown_error");
            logs.Add(Warning($"prepare failed code={logCode}"));
            return new AppUpdateExecutionFlowResult
            {
                IsSuccess = false,
                ShouldShutdown = false,
                StatusText = request.Strings.UpdateFailed,
                DialogRequest = _dialogPresenter.BuildPrepareFailedDialog(dialogCode, request.Strings),
                Logs = logs
            };
        }

        var releaseOpened = _externalUrlLauncher.OpenUrl(AppUpdateExecutionService.LatestReleaseUrl);
        if (!releaseOpened)
        {
            logs.Add(Warning("latest release page open failed"));
        }

        var launchResult = _appUpdateExecutionService.TryLaunchCopiedExecutable(prepareResult.PreparedFile.FinalCopiedExePath);
        if (!launchResult.IsSuccess)
        {
            var logCode = NormalizeStatusCode(launchResult.ErrorCode, "failed");
            var dialogCode = NormalizeStatusCode(launchResult.ErrorCode, "unknown_error");
            logs.Add(Warning($"launch failed code={logCode}"));
            return new AppUpdateExecutionFlowResult
            {
                IsSuccess = false,
                ShouldShutdown = false,
                StatusText = request.Strings.UpdateFailed,
                DialogRequest = _dialogPresenter.BuildLaunchFailedDialog(dialogCode, request.Strings),
                Logs = logs
            };
        }

        logs.Add(Info($"installer launched version={request.UpdateInfo.LatestVersion}"));
        return new AppUpdateExecutionFlowResult
        {
            IsSuccess = true,
            ShouldShutdown = true,
            StatusText = request.Strings.UpdateLaunchedClosing,
            Logs = logs
        };
    }

    private static AppUpdateFlowLogEntry Info(string message)
    {
        return new AppUpdateFlowLogEntry
        {
            Level = "info",
            Category = "app-update",
            Message = message
        };
    }

    private static AppUpdateFlowLogEntry Warning(string message)
    {
        return new AppUpdateFlowLogEntry
        {
            Level = "warning",
            Category = "app-update",
            Message = message
        };
    }

    private static string NormalizeStatusCode(string? value, string fallback)
    {
        var normalized = (value ?? "").Trim();
        return string.IsNullOrWhiteSpace(normalized) ? fallback : normalized;
    }
}
