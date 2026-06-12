using OptiClick.Wpf.Localization;

namespace OptiClick.Wpf.Shell.Runtime;

public sealed record RuntimeContextCoordinatorText
{
    public required RuntimeSummaryStateText SummaryText { get; init; }

    public static RuntimeContextCoordinatorText FromAppStrings(AppStrings strings)
    {
        ArgumentNullException.ThrowIfNull(strings);
        return new RuntimeContextCoordinatorText
        {
            SummaryText = RuntimeSummaryStateText.FromAppStrings(strings)
        };
    }
}
