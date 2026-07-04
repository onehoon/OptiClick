using OptiClick.Wpf.ViewModels.Features.Shell;
using OptiClick.Wpf.ViewModels.Features.Startup;

namespace OptiClick.Wpf.ViewModels.Ports.Startup;

internal sealed record MainShellStartupPortCompositionInput
{
    public required IMainShellStartupPortAccess Access { get; init; }
    public required Func<MainShellInteractionFeatureFacade> ResolveShellInteractionFeature { get; init; }
    public required Func<MainStartupFeatureFacade> ResolveStartupFeature { get; init; }
}

internal static class MainShellStartupPortComposer
{
    public static MainShellFacadeStartupPort Compose(MainShellStartupPortCompositionInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var access = input.Access;

        return new MainShellFacadeStartupPort
        {
            UpdateStartupPreparationState = access.UpdateStartupPreparationState,
            ClearLastErrorCode = access.ClearLastErrorCode,
            ShowPendingStartupNoticesAsync =
                cancellationToken => input.ResolveShellInteractionFeature()
                    .ShowPendingStartupNoticesAsync(cancellationToken),
            RunStartupAutoScanAsync = access.RunStartupAutoScanAsync,
            StartStartupDialogsInBackground =
                () => input.ResolveStartupFeature().StartStartupDialogsInBackground(),
            StartStartupUpdateCheckInBackground =
                () => input.ResolveStartupFeature().StartStartupUpdateCheckInBackground(),
            StartStartupAnnouncementInBackground =
                () => input.ResolveStartupFeature().StartStartupAnnouncementInBackground(),
            StartGameMasterCoverPrefetchInBackground =
                () => input.ResolveStartupFeature().StartGameMasterCoverPrefetchInBackground(),
            QueueHomeCoverPrefetchInBackground =
                reason => input.ResolveStartupFeature().QueueHomeCoverPrefetchInBackground(reason),
            StartStartupPreparationAsync =
                cancellationToken => input.ResolveStartupFeature().StartStartupPreparationAsync(cancellationToken)
        };
    }
}
