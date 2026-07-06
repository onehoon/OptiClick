using OptiClick.Wpf.Models;

namespace OptiClick.Wpf.Services;

public sealed class AppUpdateDialogPresenter
{
    public AppDialogRequest BuildNoUpdateDialog(AppUpdateFlowText text)
    {
        return new AppDialogRequest
        {
            Title = text.UpdateCheckTitle,
            Summary = text.UpdateLatestMessage,
            Kind = AppDialogKind.Info,
            Severity = DialogSeverity.Info
        };
    }

    public AppDialogRequest BuildUpdateAvailableDialog(AppUpdateInfo updateInfo, AppUpdateFlowText text)
    {
        ArgumentNullException.ThrowIfNull(updateInfo);

        return new AppDialogRequest
        {
            Title = text.UpdateAvailableTitle,
            Summary = text.UpdateAvailableSummary,
            Kind = AppDialogKind.Warning,
            Severity = DialogSeverity.Warning,
            BulletItems = [],
            PrimaryButtonText = text.UpdateAvailablePrimaryButton,
            SecondaryButtonText = text.UpdateAvailableSecondaryButton,
            PrimaryResult = AppDialogResult.Continue,
            SecondaryResult = AppDialogResult.Cancel
        };
    }

    public AppDialogRequest BuildUpdateFailedDialog(AppUpdateFlowText text)
    {
        return new AppDialogRequest
        {
            Title = text.UpdateFailedTitle,
            Summary = text.UpdatePrepareFailedSummary,
            Kind = AppDialogKind.Warning,
            Severity = DialogSeverity.Warning,
            BulletItems = [text.UpdateTryAgainLater]
        };
    }
}
