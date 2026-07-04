namespace OptiClick.Wpf.Services;

public sealed class AppUpdateFlowController
{
    private readonly IAppUpdateService _appUpdateService;
    private readonly IPublicAppUpdateService? _publicAppUpdateService;
    private readonly IAppUpdateExecutionService _appUpdateExecutionService;
    private readonly IExternalUrlLauncher _externalUrlLauncher;
    private readonly AppUpdateDialogPresenter _dialogPresenter;

    public AppUpdateFlowController(
        IAppUpdateService appUpdateService,
        IAppUpdateExecutionService appUpdateExecutionService,
        IExternalUrlLauncher externalUrlLauncher,
        IPublicAppUpdateService? publicAppUpdateService = null,
        AppUpdateDialogPresenter? dialogPresenter = null)
    {
        _appUpdateService = appUpdateService ?? throw new ArgumentNullException(nameof(appUpdateService));
        _publicAppUpdateService = publicAppUpdateService;
        _appUpdateExecutionService = appUpdateExecutionService ?? throw new ArgumentNullException(nameof(appUpdateExecutionService));
        _externalUrlLauncher = externalUrlLauncher ?? throw new ArgumentNullException(nameof(externalUrlLauncher));
        _dialogPresenter = dialogPresenter ?? new AppUpdateDialogPresenter();
    }

    public AppUpdateCheckResult CheckForUpdate(AppUpdateFlowRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Text);

        var logs = new List<AppUpdateFlowLogEntry>();
        if (!_appUpdateService.TryResolveUpdate(request.LatestRuntimeData, request.CurrentVersion, out var updateInfo)
            || updateInfo is null)
        {
            var noUpdateDialog = _dialogPresenter.BuildNoUpdateDialog(request.CurrentVersion, request.Text);
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
            ShouldExecuteImmediately = updateInfo.IsForced,
            UpdateInfo = updateInfo,
            DialogRequest = updateInfo.IsForced
                ? null
                : _dialogPresenter.BuildUpdateAvailableDialog(updateInfo, request.Text),
            Logs = logs
        };
    }

    public async Task<AppUpdateCheckResult> CheckForUpdateAsync(
        AppUpdateFlowRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Text);

        if (_publicAppUpdateService is null)
        {
            return CheckForUpdate(request);
        }

        var logs = new List<AppUpdateFlowLogEntry>();
        var publicResult = await _publicAppUpdateService.TryResolveUpdateAsync(
            request.CurrentVersion,
            cancellationToken);
        if (!publicResult.IsSuccess)
        {
            logs.Add(Warning($"public app update check failed code={NormalizeStatusCode(publicResult.ErrorCode, "failed")}"));
            return new AppUpdateCheckResult
            {
                IsUpdateAvailable = false,
                Logs = logs
            };
        }

        if (!publicResult.IsUpdateAvailable || publicResult.UpdateInfo is null)
        {
            var noUpdateDialog = _dialogPresenter.BuildNoUpdateDialog(request.CurrentVersion, request.Text);
            logs.Add(Info($"public no update available current={request.CurrentVersion}"));
            return new AppUpdateCheckResult
            {
                IsUpdateAvailable = false,
                StatusText = noUpdateDialog.Summary,
                DialogRequest = noUpdateDialog,
                Logs = logs
            };
        }

        var updateInfo = publicResult.UpdateInfo;
        logs.Add(Info($"public update available current={updateInfo.CurrentVersion} latest={updateInfo.LatestVersion} forced={updateInfo.IsForced}"));
        return new AppUpdateCheckResult
        {
            IsUpdateAvailable = true,
            ShouldExecuteImmediately = updateInfo.IsForced,
            UpdateInfo = updateInfo,
            DialogRequest = updateInfo.IsForced
                ? null
                : _dialogPresenter.BuildUpdateAvailableDialog(updateInfo, request.Text),
            Logs = logs
        };
    }

    public async Task<AppUpdateExecutionFlowResult> ExecuteConfirmedUpdateAsync(
        AppUpdateConfirmedRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Text);
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
                StatusText = request.Text.UpdateFailed,
                DialogRequest = _dialogPresenter.BuildPrepareFailedDialog(dialogCode, request.Text),
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
                StatusText = request.Text.UpdateFailed,
                DialogRequest = _dialogPresenter.BuildLaunchFailedDialog(dialogCode, request.Text),
                Logs = logs
            };
        }

        logs.Add(Info($"installer launched version={request.UpdateInfo.LatestVersion}"));
        return new AppUpdateExecutionFlowResult
        {
            IsSuccess = true,
            ShouldShutdown = true,
            StatusText = request.Text.UpdateLaunchedClosing,
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
