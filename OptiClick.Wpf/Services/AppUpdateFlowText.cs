using OptiClick.Wpf.Localization;

namespace OptiClick.Wpf.Services;

public sealed record AppUpdateFlowText
{
    public required string UpdateCheckTitle { get; init; }
    public required string UpdateLatestMessage { get; init; }
    public required string UpdateAvailableTitle { get; init; }
    public required string UpdateAvailableSummary { get; init; }
    public required string UpdateAvailablePrimaryButton { get; init; }
    public required string UpdateAvailableSecondaryButton { get; init; }
    public required string UpdateFailed { get; init; }
    public required string UpdateFailedTitle { get; init; }
    public required string UpdatePrepareFailedSummary { get; init; }
    public required string UpdateFailedReasonFormat { get; init; }
    public required string UpdateTryAgainLater { get; init; }
    public required string UpdateLaunchFailedSummary { get; init; }
    public required string UpdateRunCopiedInstaller { get; init; }
    public required string UpdateLaunchedClosing { get; init; }

    public static AppUpdateFlowText FromAppStrings(AppStrings strings)
    {
        ArgumentNullException.ThrowIfNull(strings);
        return new AppUpdateFlowText
        {
            UpdateCheckTitle = strings.UpdateCheckTitle,
            UpdateLatestMessage = strings.UpdateLatestMessage,
            UpdateAvailableTitle = strings.UpdateAvailableTitle,
            UpdateAvailableSummary = strings.UpdateAvailableSummary,
            UpdateAvailablePrimaryButton = strings.UpdateAvailablePrimaryButton,
            UpdateAvailableSecondaryButton = strings.UpdateAvailableSecondaryButton,
            UpdateFailed = strings.UpdateFailed,
            UpdateFailedTitle = strings.UpdateFailedTitle,
            UpdatePrepareFailedSummary = strings.UpdatePrepareFailedSummary,
            UpdateFailedReasonFormat = strings.UpdateFailedReasonFormat,
            UpdateTryAgainLater = strings.UpdateTryAgainLater,
            UpdateLaunchFailedSummary = strings.UpdateLaunchFailedSummary,
            UpdateRunCopiedInstaller = strings.UpdateRunCopiedInstaller,
            UpdateLaunchedClosing = strings.UpdateLaunchedClosing
        };
    }
}
