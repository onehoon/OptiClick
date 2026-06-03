using OptiClick.Wpf.Localization;
using OptiClick.Wpf.Models;

namespace OptiClick.Wpf.Shell.Settings;

public sealed class SettingsDialogPresenter
{
    public AppDialogRequest BuildSettingsInfoDialog(AppStrings strings)
    {
        return new AppDialogRequest
        {
            Title = strings.SettingsInfoTitle,
            Summary = strings.SettingsInfoSummary,
            Kind = AppDialogKind.Info,
            Severity = DialogSeverity.Info,
            BulletItems =
            [
                strings.SettingsInfoBulletPreviewOnly,
                strings.SettingsInfoBulletNoConfigMutation,
                strings.SettingsInfoBulletStableUxLater
            ]
        };
    }

    public AppDialogRequest BuildDeferredFeatureDialog(string title, string summary, AppStrings strings)
    {
        return new AppDialogRequest
        {
            Title = title,
            Summary = summary,
            Kind = AppDialogKind.Info,
            Severity = DialogSeverity.Info,
            BulletItems =
            [
                strings.SettingsInfoBulletPreviewOnly,
                strings.InstallNoFilesNoNetworkNoSettings
            ]
        };
    }

    public AppDialogRequest BuildWarningSampleDialog(AppStrings strings)
    {
        return new AppDialogRequest
        {
            Title = strings.DialogWarningSampleTitle,
            Summary = strings.DialogWarningSampleSummary,
            Kind = AppDialogKind.Warning,
            Severity = DialogSeverity.Warning,
            BulletItems =
            [
                strings.DialogWarningSampleBulletRecoverable,
                strings.DialogWarningSampleBulletNoWorkflow
            ]
        };
    }

    public AppDialogRequest BuildBlockingSampleDialog(AppStrings strings)
    {
        return new AppDialogRequest
        {
            Title = strings.DialogBlockingSampleTitle,
            Summary = strings.DialogBlockingSampleSummary,
            Kind = AppDialogKind.Blocking,
            Severity = DialogSeverity.Blocking,
            IsBlocking = true,
            BulletItems =
            [
                strings.DialogBlockingSampleBulletMissingCondition,
                strings.DialogBlockingSampleBulletNoContinue
            ]
        };
    }

    public AppDialogRequest BuildModConflictSampleDialog(AppStrings strings)
    {
        return new AppDialogRequest
        {
            Title = strings.DialogModConflictSampleTitle,
            Summary = strings.DialogModConflictSampleSummary,
            Kind = AppDialogKind.ModConflict,
            Severity = DialogSeverity.Blocking,
            IsBlocking = true,
            BulletItems =
            [
                strings.DialogModConflictSampleBulletLoaderInstalled,
                strings.DialogModConflictSampleBulletRemoveConflict
            ]
        };
    }
}
