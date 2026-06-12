using OptiClick.Wpf.Localization;
using OptiClick.Wpf.Services;

namespace OptiClick.Wpf.Shell.Update;

public sealed record AppUpdateCoordinatorText
{
    public required string UpdateAlreadyInProgress { get; init; }
    public required AppUpdateFlowText FlowText { get; init; }

    public static AppUpdateCoordinatorText FromAppStrings(AppStrings strings)
    {
        ArgumentNullException.ThrowIfNull(strings);
        return new AppUpdateCoordinatorText
        {
            UpdateAlreadyInProgress = strings.UpdateAlreadyInProgress,
            FlowText = AppUpdateFlowText.FromAppStrings(strings)
        };
    }
}
