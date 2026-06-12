using OptiClick.Wpf.Localization;

namespace OptiClick.Wpf.ViewModels;

internal sealed record InstallManagementDialogText
{
    public required string DialogTitle { get; init; }
    public required string InstalledSummary { get; init; }
    public required string UpdateAvailableSummary { get; init; }
    public required string ChooseAction { get; init; }
    public required string UninstallButton { get; init; }
    public required string UpdateButton { get; init; }
    public required string ReinstallButton { get; init; }
    public required string CancelButton { get; init; }

    public static InstallManagementDialogText FromAppStrings(AppStrings strings)
    {
        ArgumentNullException.ThrowIfNull(strings);
        return new InstallManagementDialogText
        {
            DialogTitle = strings.InstallManagementDialogTitle,
            InstalledSummary = strings.InstallManagementInstalledSummary,
            UpdateAvailableSummary = strings.InstallManagementUpdateAvailableSummary,
            ChooseAction = strings.InstallManagementChooseAction,
            UninstallButton = strings.InstallManagementUninstallButton,
            UpdateButton = strings.InstallButtonUpdate,
            ReinstallButton = strings.InstallButtonReinstall,
            CancelButton = strings.DialogButtonCancel
        };
    }
}
