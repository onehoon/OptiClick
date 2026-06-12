using OptiClick.Wpf.ViewModels.Features.Shell;

namespace OptiClick.Wpf.ViewModels.Ports.Ui;

internal sealed record MainShellUiPortCompositionInput
{
    public required IMainShellUiPortAccess Access { get; init; }
    public required Func<MainShellInteractionFeatureFacade> ResolveShellInteractionFeature { get; init; }
}

internal static class MainShellUiPortComposer
{
    public static MainShellFacadeUiPort Compose(MainShellUiPortCompositionInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var access = input.Access;

        return new MainShellFacadeUiPort
        {
            ReadStartupOverlay = () => access.StartupOverlay,
            ReadCurrentViewKind = () => access.CurrentViewKind,
            SetCurrentView = access.SetCurrentView,
            ReadOpenGameSupportRequestCommand = () => access.OpenGameSupportRequestCommand,
            HasSupportedGamesEntries = () => access.SupportedGamesHasEntries,
            RebuildSupportedGamesRows = access.RebuildSupportedGamesRows,
            RefreshSupportedGamesAfterLanguageChange = access.RefreshSupportedGamesAfterLanguageChange,
            ApplySelectedGameLocalization = access.ApplySelectedGameLocalization,
            StartSupportedGamesWikiRefreshInBackground = access.StartSupportedGamesWikiRefreshInBackground,
            ShowDetails = () => input.ResolveShellInteractionFeature().ShowDetailsDialog(),
            OpenLogFolder = () => input.ResolveShellInteractionFeature().OpenLogFolder(),
            OpenSupportRequest = () => input.ResolveShellInteractionFeature().OpenSupportRequest()
        };
    }
}
