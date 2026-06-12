using OptiClick.Wpf.ViewModels.Features.Selection;

namespace OptiClick.Wpf.ViewModels.Ports.Selection;

internal sealed record MainShellSelectionPortCompositionInput
{
    public required IMainShellSelectionPortAccess Access { get; init; }
    public required Func<MainSelectionFeatureFacade> ResolveSelectionFeature { get; init; }
}

internal static class MainShellSelectionPortComposer
{
    public static MainShellFacadeSelectionPort Compose(MainShellSelectionPortCompositionInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var access = input.Access;

        return new MainShellFacadeSelectionPort
        {
            ReadVisibleCards = () => access.VisibleCards,
            ReadVisibleGameCount = () => access.VisibleGameCount,
            ReadSelectedGame = () => access.SelectedGame,
            SetSelectedGame = access.SetSelectedGame,
            ReadSelectionState = () => access.SelectionState,
            ApplySelectionState = value => access.SelectionState = value,
            ApplySelectionStateFromRuntimeCatalog = access.ApplyRuntimeCatalogSelectionState,
            ApplySelectionBridgeState = access.ApplySelectionBridgeState,
            ApplyPrecheckRunningIntermediate = access.ApplyPrecheckRunningIntermediate,
            IsInstallExecutionInProgress = () => access.IsInstallExecutionInProgress,
            IsAppUpdateInProgress = () => access.IsAppUpdateInProgress,
            ReadSuppressHomeNavigationForAutoSelection = () => access.SuppressHomeNavigationForAutoSelection,
            SetSuppressHomeNavigationForAutoSelection =
                value => access.SuppressHomeNavigationForAutoSelection = value,
            IncrementSelectionVersion = access.IncrementSelectionVersion,
            ReadSelectionVersion = access.ReadSelectionVersion,
            RecomputeSelectionAfterScanAsync =
                (cancellationToken, navigateHome) => input.ResolveSelectionFeature()
                    .RecomputeSelectionAfterScanAsync(cancellationToken, navigateHome),
            RefreshVisibleGamesAfterLanguageChangeAsync =
                cancellationToken => input.ResolveSelectionFeature()
                    .RefreshVisibleGamesAfterLanguageChangeAsync(cancellationToken),
            BuildScanRequest = scanFolders => input.ResolveSelectionFeature().BuildScanRequest(scanFolders),
            RefreshVisibleGamesFromScanMatches =
                () => input.ResolveSelectionFeature().RefreshVisibleGamesFromScanMatches(),
            ReplaceGameCards = (cards, observeAutoSelection) =>
                input.ResolveSelectionFeature().ReplaceGameCards(cards, observeAutoSelection),
            TryRefreshVisibleCard = gameId =>
                input.ResolveSelectionFeature().TryRefreshVisibleGameCardsAfterInstall(gameId),
            SelectGameAsync = (game, cancellationToken, navigateHome, showPendingPopups) =>
                input.ResolveSelectionFeature().SelectGameCardAsync(
                    game,
                    cancellationToken,
                    navigateHome,
                    showPendingPopups),
            RefreshSelectionForInstallAsync =
                (game, cancellationToken, navigateHome, showPendingPopups) =>
                    input.ResolveSelectionFeature().SelectGameCardAsync(
                        game,
                        cancellationToken,
                        navigateHome,
                        showPendingPopups),
            ResolveSelectedIndex = access.ResolveSelectedIndex
        };
    }
}
