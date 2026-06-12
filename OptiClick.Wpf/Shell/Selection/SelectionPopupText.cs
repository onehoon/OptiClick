using OptiClick.Wpf.Localization;

namespace OptiClick.Wpf.Shell.Selection;

public sealed record SelectionPopupText
{
    public required string SettingsWarning { get; init; }
    public required string InstallNoticeDialogTitle { get; init; }
    public required string InstallDialogTitle { get; init; }
    public required string DialogButtonOk { get; init; }

    public static SelectionPopupText FromAppStrings(AppStrings strings)
    {
        ArgumentNullException.ThrowIfNull(strings);
        return new SelectionPopupText
        {
            SettingsWarning = strings.SettingsWarning,
            InstallNoticeDialogTitle = strings.InstallNoticeDialogTitle,
            InstallDialogTitle = strings.InstallDialogTitle,
            DialogButtonOk = strings.DialogButtonOk
        };
    }
}
