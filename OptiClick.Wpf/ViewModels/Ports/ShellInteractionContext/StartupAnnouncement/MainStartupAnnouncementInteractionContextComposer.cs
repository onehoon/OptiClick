using OptiClick.Wpf.Logging;

namespace OptiClick.Wpf.ViewModels.Ports.ShellInteractionContext.StartupAnnouncement;

internal sealed record MainStartupAnnouncementInteractionContextCompositionInput
{
    public required MainAppResolvedDependencies AppDependencies { get; init; }
    public required MainShellResolvedDependencies ShellDependencies { get; init; }
    public required IMainStartupAnnouncementInteractionAccess Access { get; init; }
}

internal static class MainStartupAnnouncementInteractionContextComposer
{
    public static MainStartupAnnouncementInteractionContextInput Compose(
        MainStartupAnnouncementInteractionContextCompositionInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var access = input.Access;

        return new MainStartupAnnouncementInteractionContextInput
        {
            ReadLatestRuntimeData = () => access.LatestRuntimeData,
            ReadSelectedLanguage = () => access.SelectedLanguage,
            ReadSelectedGpuVendor = () => access.LatestRuntimeContext.SelectedGpu?.Vendor ?? "",
            DispatchFlowLogs = input.ShellDependencies.FlowLogDispatcher.Dispatch,
            ShowDialogAsync = input.ShellDependencies.DialogPresenter.ShowSafelyAsync,
            OpenExternalUrl = input.AppDependencies.ExternalUrlLauncher.OpenUrl,
            LogWarning = message => input.AppDependencies.AppLogger.Warning(MainViewModelLogCategories.Startup, message)
        };
    }
}
