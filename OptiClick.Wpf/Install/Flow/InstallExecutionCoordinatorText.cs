using OptiClick.Wpf.Localization;

namespace OptiClick.Wpf.Install.Flow;

public sealed record InstallExecutionCoordinatorText
{
    public required string OperationOverlayInstalling { get; init; }
    public required string OperationOverlayUpdating { get; init; }
    public required string OperationOverlayReinstalling { get; init; }

    public static InstallExecutionCoordinatorText FromAppStrings(AppStrings strings)
    {
        ArgumentNullException.ThrowIfNull(strings);
        return new InstallExecutionCoordinatorText
        {
            OperationOverlayInstalling = strings.OperationOverlayInstalling,
            OperationOverlayUpdating = strings.OperationOverlayUpdating,
            OperationOverlayReinstalling = strings.OperationOverlayReinstalling
        };
    }
}
