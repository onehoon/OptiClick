using OptiClick.Wpf.Localization;
using OptiClick.Wpf.Models;

namespace OptiClick.Wpf.Services;

public sealed class AppUpdateDialogPresenter
{
    public AppDialogRequest BuildNoUpdateDialog(string currentVersion, AppUpdateFlowText text)
    {
        _ = currentVersion;
        var message = text.UpdateLatestMessage;
        return new AppDialogRequest
        {
            Title = text.UpdateCheckTitle,
            Summary = message,
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

    public AppDialogRequest BuildPrepareFailedDialog(string errorCode, AppUpdateFlowText text)
    {
        return new AppDialogRequest
        {
            Title = text.UpdateFailedTitle,
            Summary = text.UpdatePrepareFailedSummary,
            Kind = AppDialogKind.Warning,
            Severity = DialogSeverity.Warning,
            BulletItems =
            [
                LocalizedTextFormatter.Format(text.UpdateFailedReasonFormat, errorCode ?? ""),
                text.UpdateTryAgainLater
            ]
        };
    }

    public AppDialogRequest BuildLaunchFailedDialog(string errorCode, AppUpdateFlowText text)
    {
        return new AppDialogRequest
        {
            Title = text.UpdateFailedTitle,
            Summary = text.UpdateLaunchFailedSummary,
            Kind = AppDialogKind.Warning,
            Severity = DialogSeverity.Warning,
            BulletItems =
            [
                LocalizedTextFormatter.Format(text.UpdateFailedReasonFormat, errorCode ?? ""),
                text.UpdateRunCopiedInstaller
            ]
        };
    }

}
