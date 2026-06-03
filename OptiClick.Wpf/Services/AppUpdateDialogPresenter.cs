using System.Globalization;
using OptiClick.Wpf.Localization;
using OptiClick.Wpf.Models;

namespace OptiClick.Wpf.Services;

public sealed class AppUpdateDialogPresenter
{
    public AppDialogRequest BuildNoUpdateDialog(string currentVersion, AppStrings strings)
    {
        _ = currentVersion;
        var message = strings.UpdateLatestMessage;
        return new AppDialogRequest
        {
            Title = strings.UpdateCheckTitle,
            Summary = message,
            Kind = AppDialogKind.Info,
            Severity = DialogSeverity.Info
        };
    }

    public AppDialogRequest BuildUpdateAvailableDialog(AppUpdateInfo updateInfo, AppStrings strings)
    {
        ArgumentNullException.ThrowIfNull(updateInfo);

        return new AppDialogRequest
        {
            Title = strings.UpdateAvailableTitle,
            Summary = strings.UpdateAvailableSummary,
            Kind = AppDialogKind.Warning,
            Severity = DialogSeverity.Warning,
            BulletItems = [],
            PrimaryButtonText = strings.UpdateAvailablePrimaryButton,
            SecondaryButtonText = strings.UpdateAvailableSecondaryButton,
            PrimaryResult = AppDialogResult.Continue,
            SecondaryResult = AppDialogResult.Cancel
        };
    }

    public AppDialogRequest BuildPrepareFailedDialog(string errorCode, AppStrings strings)
    {
        return new AppDialogRequest
        {
            Title = strings.UpdateFailedTitle,
            Summary = strings.UpdatePrepareFailedSummary,
            Kind = AppDialogKind.Warning,
            Severity = DialogSeverity.Warning,
            BulletItems =
            [
                Format(strings.UpdateFailedReasonFormat, errorCode ?? ""),
                strings.UpdateTryAgainLater
            ]
        };
    }

    public AppDialogRequest BuildLaunchFailedDialog(string errorCode, AppStrings strings)
    {
        return new AppDialogRequest
        {
            Title = strings.UpdateFailedTitle,
            Summary = strings.UpdateLaunchFailedSummary,
            Kind = AppDialogKind.Warning,
            Severity = DialogSeverity.Warning,
            BulletItems =
            [
                Format(strings.UpdateFailedReasonFormat, errorCode ?? ""),
                strings.UpdateRunCopiedInstaller
            ]
        };
    }

    private static string Format(string template, params object[] args)
    {
        return string.Format(CultureInfo.CurrentCulture, template ?? "", args ?? []);
    }
}
