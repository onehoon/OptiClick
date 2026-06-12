using System;
using System.Threading;
using System.Threading.Tasks;
using OptiClick.Wpf.Install.Flow;
using OptiClick.Wpf.Models;
using OptiClick.Wpf.Shell.Games;
using OptiClick.Wpf.Shell.Selection;

namespace OptiClick.Wpf.ViewModels;

internal sealed class MainUninstallInteractionController
{
    public async Task HandleUninstallAsync(
        MainUninstallInteractionContext context,
        GameCardViewModel selectedGame,
        CancellationToken cancellationToken = default)
    {
        var selectionStateBeforeExecution = context.State.ReadSelectionState();
        var selectedShellGame = context.State.ResolveShellGame(selectedGame);
        var targetPath = context.State.ResolveSelectedGameTargetPath(selectedGame);
        var selectionSnapshot = UninstallFlowSelectionSnapshotMapper.FromSelectionState(selectionStateBeforeExecution);

        var request = context.Services.BuildUninstallRequest(
            selectedShellGame,
            targetPath,
            selectionSnapshot);

        await context.Services.RunUninstallFlowAsync(request, cancellationToken);
    }

    public async Task RefreshSelectionAfterUninstallAsync(
        MainUninstallSelectionRefreshContext context,
        GameCardViewModel selectedGame,
        CancellationToken cancellationToken)
    {
        var buttonBefore = context.State.ReadInstallButtonText();
        try
        {
            var selectedGameId = context.State.ReadSelectedGameId(selectedGame);
            var refreshedCard = context.Services.TryRefreshVisibleCard(selectedGameId);
            var gameToSelect = selectedGame;
            if (refreshedCard is not null)
            {
                gameToSelect = refreshedCard;
            }

            await context.Services.SelectGameAsync(
                gameToSelect,
                cancellationToken,
                false,
                false);
            var badgeAfter = gameToSelect?.StatusBadge ?? "";
            var badgeAfterNormalized = NormalizeStatusCode(badgeAfter, "none");
            var selectedGameIdNormalized = NormalizeStatusCode(selectedGameId, "none");
            var buttonBeforeNormalized = NormalizeStatusCode(buttonBefore, "none");
            var buttonAfterNormalized = NormalizeStatusCode(context.State.ReadInstallButtonText(), "none");
            var cardRefreshed = (refreshedCard is not null).ToString().ToLowerInvariant();
            context.Services.LogSuccess(
                $"badge refresh result=success game_id={selectedGameIdNormalized} card_refreshed={cardRefreshed} badge_after={badgeAfterNormalized} button_before={buttonBeforeNormalized} button_after={buttonAfterNormalized}");
        }
        catch (OperationCanceledException)
        {
            context.Services.LogCancel();
        }
        catch (Exception ex)
        {
            context.Services.LogFailure("badge refresh result=failed", ex);
        }
    }

    private static string NormalizeStatusCode(string? value, string fallback)
    {
        var normalized = (value ?? "").Trim();
        return string.IsNullOrWhiteSpace(normalized) ? fallback : normalized;
    }
}

internal sealed class MainUninstallInteractionState
{
    public required Func<ShellInstallSelectionState> ReadSelectionState { get; init; }
    public required Func<GameCardViewModel, ShellGameCardModel> ResolveShellGame { get; init; }
    public required Func<GameCardViewModel, string> ResolveSelectedGameTargetPath { get; init; }
}

internal sealed class MainUninstallInteractionServices
{
    public required Func<ShellGameCardModel, string, UninstallFlowSelectionSnapshot, UninstallFlowCoordinatorRequest> BuildUninstallRequest { get; init; }
    public required Func<UninstallFlowCoordinatorRequest, CancellationToken, Task> RunUninstallFlowAsync { get; init; }
}

internal sealed class MainUninstallInteractionContext
{
    public required MainUninstallInteractionState State { get; init; }
    public required MainUninstallInteractionServices Services { get; init; }
}

internal sealed class MainUninstallSelectionRefreshContext
{
    public required MainUninstallSelectionRefreshState State { get; init; }
    public required MainUninstallSelectionRefreshServices Services { get; init; }
}

internal sealed class MainUninstallSelectionRefreshState
{
    public required Func<GameCardViewModel, string> ReadSelectedGameId { get; init; }
    public required Func<string> ReadInstallButtonText { get; init; }
}

internal sealed class MainUninstallSelectionRefreshServices
{
    public required Func<string, GameCardViewModel?> TryRefreshVisibleCard { get; init; }
    public required Func<GameCardViewModel?, CancellationToken, bool, bool, Task> SelectGameAsync { get; init; }
    public required Action<string> LogSuccess { get; init; }
    public required Action LogCancel { get; init; }
    public required Action<string, Exception> LogFailure { get; init; }
}
