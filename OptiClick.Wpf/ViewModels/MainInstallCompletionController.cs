using System;
using System.Threading;
using System.Threading.Tasks;
using OptiClick.Wpf.Install.Flow;
using OptiClick.Wpf.Install.Planning;
using OptiClick.Wpf.Install.Presentation;
using OptiClick.Wpf.Install.UiState;
using OptiClick.Wpf.Shell.Flow;
using OptiClick.Wpf.Shell.Games;
using OptiClick.Wpf.Shell.RuntimeData;
using OptiClick.Wpf.Models;

namespace OptiClick.Wpf.ViewModels;

internal sealed class MainInstallCompletionController
{
    public async Task ApplyInstallExecutionResultAsync(
        MainInstallCompletionContext context,
        InstallFlowResult result,
        CancellationToken cancellationToken)
    {
        context.Services.DispatchFlowLogs(result.Logs, MainViewModelLogCategories.Install);
        var update = context.Services.CreateInstallStateUpdate(result);

        var finalSuccess = result.ApplyResult?.FinalSuccess == true;
        if (!result.DidStart || result.WasBlocked || !finalSuccess)
        {
            context.Services.ApplyDeferredStateUpdate(update);
            context.Services.LogInfo("badge refresh result=skipped reason=install_not_completed");
            return;
        }

        var completionDialog = update.PopupRequest is null
            ? null
            : context.Services.BuildCompletionDialog(update);
        context.Services.ApplyStateUpdate(update with
        {
            PopupRequest = null
        });

        await context.Services.RefreshSelectionAfterSuccessfulInstallAsync(cancellationToken);
        OpenInstallPostUrlIfNeeded(context);

        if (completionDialog is null)
        {
            return;
        }

        context.Services.LogInfo("popup show source=install_post");
        var dialogResult = await context.Services.ShowCompletionDialogAsync(completionDialog, cancellationToken);
        context.Services.LogInfo($"popup result source=install_post result={dialogResult}");
        if (dialogResult == AppDialogResult.Ok)
        {
            context.Services.ClearSelectedGameContext();
            context.Services.LogInfo("selection reset result=success reason=install_completion_acknowledged");
        }
        else
        {
            context.Services.LogInfo(
                $"selection reset result=skipped reason=install_completion_not_acknowledged dialog_result={dialogResult}");
        }
    }

    public async Task RefreshSelectionAfterSuccessfulInstallAsync(
        MainInstallCompletionSelectionRefreshContext context,
        CancellationToken cancellationToken)
    {
        var selectedGame = context.State.ReadSelectedGame();
        if (selectedGame is null)
        {
            context.Services.LogWarn("install completion badge refresh skipped reason=no_selected_game");
            return;
        }

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

            await context.Services.SelectGameAsync(gameToSelect, cancellationToken, false, false);
            var badgeAfter = gameToSelect?.StatusBadge ?? "";
            var badgeAfterNormalized = NormalizeStatusCode(badgeAfter, "none");
            var refreshed = (refreshedCard is not null).ToString().ToLowerInvariant();
            var gameIdNormalized = NormalizeStatusCode(selectedGameId, "none");
            var before = NormalizeStatusCode(buttonBefore, "none");
            var after = NormalizeStatusCode(context.State.ReadInstallButtonText(), "none");
            context.Services.LogInfo(
                $"badge refresh result=success game_id={gameIdNormalized} card_refreshed={refreshed} badge_after={badgeAfterNormalized} button_before={before} button_after={after}");
        }
        catch (OperationCanceledException)
        {
            context.Services.LogWarn("badge refresh result=canceled");
        }
        catch (Exception ex)
        {
            context.Services.LogError("badge refresh result=failed", ex);
        }
    }

    private static string NormalizeStatusCode(string? value, string fallback)
    {
        var normalized = (value ?? "").Trim();
        return string.IsNullOrWhiteSpace(normalized) ? fallback : normalized;
    }

    private static void OpenInstallPostUrlIfNeeded(MainInstallCompletionContext context)
    {
        var selectedGame = context.Services.ReadSelectedGame();
        if (selectedGame is null)
        {
            return;
        }

        var shellGame = ShellGameCardMapper.Map(selectedGame);
        var rawUrl = CoalesceTrimmed(
            shellGame.ExternalGuidePostUrl,
            string.Equals(shellGame.ExternalGuideUrlTrigger, "install_post", StringComparison.OrdinalIgnoreCase)
                ? shellGame.ExternalGuideUrl
                : "");
        if (string.IsNullOrWhiteSpace(rawUrl))
        {
            return;
        }

        if (!ExternalMessageUrlValidator.TryNormalizeHttpUrl(rawUrl, out var normalizedUrl))
        {
            context.Services.LogWarn("external guide url open skipped trigger=install_post reason=invalid_url");
            return;
        }

        try
        {
            if (!context.Services.OpenExternalUrl(normalizedUrl))
            {
                context.Services.LogWarn("external guide url open result=failed trigger=install_post");
            }
            else
            {
                context.Services.LogInfo("external guide url open result=success trigger=install_post");
            }
        }
        catch (Exception ex)
        {
            context.Services.LogWarn($"external guide url open result=failed trigger=install_post type={ex.GetType().Name}");
        }
    }

    private static string CoalesceTrimmed(params string[] values)
    {
        foreach (var value in values)
        {
            var normalized = (value ?? "").Trim();
            if (!string.IsNullOrWhiteSpace(normalized))
            {
                return normalized;
            }
        }

        return "";
    }
}

internal sealed class MainInstallCompletionContext
{
    public required MainInstallCompletionServices Services { get; init; }
}

internal sealed class MainInstallCompletionServices
{
    public required Action<IReadOnlyList<IFlowLogEntry>, string> DispatchFlowLogs { get; init; }
    public required Func<InstallFlowResult, MainViewModelStateUpdate> CreateInstallStateUpdate { get; init; }
    public required Action<MainViewModelStateUpdate> ApplyStateUpdate { get; init; }
    public required Action<MainViewModelStateUpdate> ApplyDeferredStateUpdate { get; init; }
    public required Func<MainViewModelStateUpdate, AppDialogRequest?> BuildCompletionDialog { get; init; }
    public required Func<CancellationToken, Task> RefreshSelectionAfterSuccessfulInstallAsync { get; init; }
    public required Func<AppDialogRequest, CancellationToken, Task<AppDialogResult>> ShowCompletionDialogAsync { get; init; }
    public required Func<GameCardViewModel?> ReadSelectedGame { get; init; }
    public required Func<string, bool> OpenExternalUrl { get; init; }
    public required Action ClearSelectedGameContext { get; init; }
    public required Action<string> LogInfo { get; init; }
    public required Action<string> LogWarn { get; init; }
}

internal sealed class MainInstallCompletionSelectionRefreshContext
{
    public required MainInstallCompletionSelectionRefreshState State { get; init; }
    public required MainInstallCompletionSelectionRefreshServices Services { get; init; }
}

internal sealed class MainInstallCompletionSelectionRefreshState
{
    public required Func<GameCardViewModel?> ReadSelectedGame { get; init; }
    public required Func<GameCardViewModel, string> ReadSelectedGameId { get; init; }
    public required Func<string> ReadInstallButtonText { get; init; }
}

internal sealed class MainInstallCompletionSelectionRefreshServices
{
    public required Func<string, GameCardViewModel?> TryRefreshVisibleCard { get; init; }
    public required Func<GameCardViewModel?, CancellationToken, bool, bool, Task> SelectGameAsync { get; init; }
    public required Action<string> LogInfo { get; init; }
    public required Action<string> LogWarn { get; init; }
    public required Action<string, Exception> LogError { get; init; }
}
