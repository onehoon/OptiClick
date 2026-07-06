namespace OptiClick.Wpf.Services;

public sealed class AppUpdateFlowController
{
    private readonly IVelopackAppUpdateService _velopackAppUpdateService;
    private readonly AppUpdateDialogPresenter _dialogPresenter;

    public AppUpdateFlowController(
        IVelopackAppUpdateService velopackAppUpdateService,
        AppUpdateDialogPresenter? dialogPresenter = null)
    {
        _velopackAppUpdateService = velopackAppUpdateService ?? throw new ArgumentNullException(nameof(velopackAppUpdateService));
        _dialogPresenter = dialogPresenter ?? new AppUpdateDialogPresenter();
    }

    public async Task<AppUpdateCheckResult> CheckForUpdateAsync(
        AppUpdateFlowRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Text);

        var logs = new List<AppUpdateFlowLogEntry>();
        AppUpdateInfo? updateInfo;
        try
        {
            updateInfo = await _velopackAppUpdateService.CheckForUpdateAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logs.Add(Warning("update check canceled"));
            return new AppUpdateCheckResult { IsUpdateAvailable = false, Logs = logs };
        }
        catch (Exception ex)
        {
            logs.Add(Warning($"update check failed type={ex.GetType().Name}"));
            return new AppUpdateCheckResult { IsUpdateAvailable = false, Logs = logs };
        }

        if (updateInfo is null)
        {
            var noUpdateDialog = _dialogPresenter.BuildNoUpdateDialog(request.Text);
            logs.Add(Info("no update available"));
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
            DialogRequest = _dialogPresenter.BuildUpdateAvailableDialog(updateInfo, request.Text),
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
        try
        {
            await _velopackAppUpdateService.ApplyUpdateAndRestartAsync(request.UpdateInfo, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logs.Add(Warning("update apply canceled"));
            return new AppUpdateExecutionFlowResult
            {
                IsSuccess = false,
                ShouldShutdown = false,
                StatusText = request.Text.UpdateFailed,
                DialogRequest = _dialogPresenter.BuildUpdateFailedDialog(request.Text),
                Logs = logs
            };
        }
        catch (Exception ex)
        {
            logs.Add(Warning($"update apply failed type={ex.GetType().Name}"));
            return new AppUpdateExecutionFlowResult
            {
                IsSuccess = false,
                ShouldShutdown = false,
                StatusText = request.Text.UpdateFailed,
                DialogRequest = _dialogPresenter.BuildUpdateFailedDialog(request.Text),
                Logs = logs
            };
        }

        logs.Add(Info($"update applied version={request.UpdateInfo.LatestVersion}"));
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
}
