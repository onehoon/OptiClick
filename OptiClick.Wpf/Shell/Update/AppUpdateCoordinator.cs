using OptiClick.Wpf.Models;
using OptiClick.Wpf.Services;

namespace OptiClick.Wpf.Shell.Update;

public sealed class AppUpdateCoordinator
{
    private readonly AppUpdateFlowController _appUpdateFlowController;

    public AppUpdateCoordinator(AppUpdateFlowController appUpdateFlowController)
    {
        _appUpdateFlowController = appUpdateFlowController ?? throw new ArgumentNullException(nameof(appUpdateFlowController));
    }

    public async Task<AppUpdateCoordinatorResult> BeginCheckAsync(
        AppUpdateCoordinatorRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Text);

        if (request.IsAppUpdateInProgress)
        {
            return new AppUpdateCoordinatorResult
            {
                ShouldContinue = false,
                StatusText = request.Trigger == AppUpdateTrigger.Manual
                    ? request.Text.UpdateAlreadyInProgress
                    : ""
            };
        }

        var checkResult = await _appUpdateFlowController.CheckForUpdateAsync(
            new AppUpdateFlowRequest
            {
                Text = request.Text.FlowText
            },
            cancellationToken);

        return new AppUpdateCoordinatorResult
        {
            ShouldContinue = true,
            ShouldShowDialog = checkResult.IsUpdateAvailable,
            IsUpdateAvailable = checkResult.IsUpdateAvailable,
            StatusText = request.Trigger == AppUpdateTrigger.Manual
                ? checkResult.StatusText
                : "",
            MissingUpdateInfoLogMessage = request.Trigger == AppUpdateTrigger.Startup
                ? "update info missing after startup update availability check"
                : "update info missing after update availability check",
            DialogRequest = checkResult.DialogRequest,
            UpdateInfo = checkResult.UpdateInfo,
            Logs = checkResult.Logs
        };
    }

    public static bool IsDialogConfirmed(AppDialogResult result)
    {
        return result == AppDialogResult.Continue || result == AppDialogResult.Ok;
    }
}
