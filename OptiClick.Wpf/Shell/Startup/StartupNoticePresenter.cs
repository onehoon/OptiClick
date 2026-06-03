using OptiClick.Wpf.Localization;
using OptiClick.Wpf.Models;
using OptiClick.Core.Runtime;

namespace OptiClick.Wpf.Shell.Startup;

public sealed class StartupNoticePresenter
{
    public AppDialogRequest BuildWindows10StartupBlockDialog(AppStrings strings)
    {
        return new AppDialogRequest
        {
            Title = strings.Windows10NotSupportedTitle,
            Summary = strings.Windows10NotSupportedSummary,
            Kind = AppDialogKind.Blocking,
            Severity = DialogSeverity.Blocking,
            IsBlocking = true,
            CanClose = false,
            BulletItems = Array.Empty<string>(),
            PrimaryButtonText = strings.Windows10NotSupportedPrimaryButton
        };
    }

    public AppDialogRequest BuildAdministratorRelaunchCancelledDialog(AppStrings strings)
    {
        return new AppDialogRequest
        {
            Kind = AppDialogKind.Warning,
            Severity = DialogSeverity.Warning,
            Title = strings.StartupAdminCancelledTitle,
            Summary = strings.StartupAdminCancelledSummary,
            BulletItems =
            [
                strings.StartupAdminCancelledBullet1,
                strings.StartupAdminCancelledBullet2,
                strings.StartupAdminCancelledBullet3
            ]
        };
    }

    public AppDialogRequest BuildStartupAnnouncementDialog(
        string title,
        IReadOnlyList<AppDialogSection> sections,
        AppLanguage language)
    {
        return new AppDialogRequest
        {
            Kind = AppDialogKind.StartupAnnouncement,
            Severity = DialogSeverity.Warning,
            Title = title ?? "",
            BulletItems = Array.Empty<string>(),
            Sections = sections ?? Array.Empty<AppDialogSection>(),
            PrimaryButtonText = language == AppLanguage.Korean ? "\uD655\uC778" : "OK",
            SecondaryButtonText = "",
            PrimaryResult = AppDialogResult.Ok,
            SecondaryResult = AppDialogResult.None
        };
    }
}
