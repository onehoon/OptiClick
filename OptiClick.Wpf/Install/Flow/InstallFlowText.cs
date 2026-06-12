using OptiClick.Wpf.Localization;

namespace OptiClick.Wpf.Install.Flow;

public sealed record InstallFlowText
{
    public required string InstallDependenciesMissing { get; init; }
    public required string InstallBlocked { get; init; }
    public required string InstallFailedConfigApply { get; init; }
    public required string InstallCompleted { get; init; }
    public required string InstallCompletedWithName { get; init; }
    public required string InstallFailed { get; init; }
    public required string InstallPostCompletedWithNameTemplate { get; init; }
    public required string InstallCompleteDialogTitle { get; init; }

    public static InstallFlowText FromAppStrings(AppStrings strings)
    {
        ArgumentNullException.ThrowIfNull(strings);
        return new InstallFlowText
        {
            InstallDependenciesMissing = strings.InstallDependenciesMissing,
            InstallBlocked = strings.InstallBlocked,
            InstallFailedConfigApply = strings.InstallFailedConfigApply,
            InstallCompleted = strings.InstallCompleted,
            InstallCompletedWithName = strings.InstallCompletedWithName,
            InstallFailed = strings.InstallFailed,
            InstallPostCompletedWithNameTemplate = strings.InstallPostCompletedWithNameTemplate,
            InstallCompleteDialogTitle = strings.InstallCompleteDialogTitle
        };
    }
}
