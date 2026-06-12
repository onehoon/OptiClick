using OptiClick.Wpf.Localization;

namespace OptiClick.Wpf.Install.Flow;

public sealed record UninstallFlowText
{
    public required string InstallManagementUninstallButton { get; init; }
    public required string UninstallNoRemovableItemsSummary { get; init; }
    public required string DialogButtonOk { get; init; }
    public required string UninstallValidationFailedTitle { get; init; }
    public required string UninstallValidationFailedSummary { get; init; }
    public required string UninstallConfirmationSummary { get; init; }
    public required string UninstallConfirmationTitle { get; init; }
    public required string DialogButtonCancel { get; init; }
    public required string OperationOverlayUninstalling { get; init; }
    public required string UninstallInProgressStatus { get; init; }
    public required string UninstallCompletedTitle { get; init; }
    public required string UninstallCompletedSummary { get; init; }
    public required string UninstallPartialFailedTitle { get; init; }
    public required string UninstallPartialFailedSummary { get; init; }
    public required string UninstallFailedTitle { get; init; }
    public required string UninstallFailedSummary { get; init; }

    public static UninstallFlowText FromAppStrings(AppStrings strings)
    {
        ArgumentNullException.ThrowIfNull(strings);
        return new UninstallFlowText
        {
            InstallManagementUninstallButton = strings.InstallManagementUninstallButton,
            UninstallNoRemovableItemsSummary = strings.UninstallNoRemovableItemsSummary,
            DialogButtonOk = strings.DialogButtonOk,
            UninstallValidationFailedTitle = strings.UninstallValidationFailedTitle,
            UninstallValidationFailedSummary = strings.UninstallValidationFailedSummary,
            UninstallConfirmationSummary = strings.UninstallConfirmationSummary,
            UninstallConfirmationTitle = strings.UninstallConfirmationTitle,
            DialogButtonCancel = strings.DialogButtonCancel,
            OperationOverlayUninstalling = strings.OperationOverlayUninstalling,
            UninstallInProgressStatus = strings.UninstallInProgressStatus,
            UninstallCompletedTitle = strings.UninstallCompletedTitle,
            UninstallCompletedSummary = strings.UninstallCompletedSummary,
            UninstallPartialFailedTitle = strings.UninstallPartialFailedTitle,
            UninstallPartialFailedSummary = strings.UninstallPartialFailedSummary,
            UninstallFailedTitle = strings.UninstallFailedTitle,
            UninstallFailedSummary = strings.UninstallFailedSummary
        };
    }
}
