using System;
using System.Threading;
using System.Threading.Tasks;
using OptiClick.Wpf.Install.UiState;
using OptiClick.Wpf.Models;
using OptiClick.Wpf.Shell.Selection;

namespace OptiClick.Wpf.ViewModels;

internal sealed class MainSelectionInteractionController
{
    public async Task SelectGameCardAsync(
        MainSelectionInteractionContext context,
        GameCardViewModel? game,
        CancellationToken cancellationToken = default,
        bool navigateHome = true,
        bool showPendingPopups = true)
    {
        if (game is null || context.State.IsInteractionBlocked())
        {
            return;
        }

        var selectedGame = game;
        context.State.SetSelectedGame(selectedGame);
        if (navigateHome)
        {
            context.Services.NavigateHome();
        }

        if (!context.Services.CanSelect())
        {
            return;
        }

        var selectedIndex = context.State.ResolveSelectedIndex(selectedGame);
        var selectionVersion = context.State.IncrementSelectionVersion();
        var previousSelectionState = context.State.ReadSelectionState();

        context.Services.ApplyPrecheckRunningIntermediate();
        var updatedSelectionState = new ShellInstallSelectionState
        {
            SelectedIndex = selectedIndex >= 0 ? selectedIndex : null,
            SelectedGameId = selectedGame.ResolvedGameId,
            PopupConfirmed = false,
            PrecheckRunning = true,
            PrecheckOk = false,
            MultiGpuBlocked = context.State.IsMultiGpuBlocked(),
            GpuSelectionPending = context.State.IsGpuSelectionPending(),
            InstallButtonPresentation = new InstallButtonPresentation
            {
                IsEnabled = false,
                ReasonCode = InstallButtonReasonCodes.InstallPrecheckRunning,
                Text = ""
            }
        };
        context.Services.ApplySelectionState(updatedSelectionState);
        context.Services.ApplySelectionBridgeState(updatedSelectionState);

        var request = context.Services.BuildSelectionRequest(selectedGame, selectedIndex, previousSelectionState);
        var result = await context.Services.SelectAsync(request, cancellationToken);
        context.Services.DispatchFlowLogs(result);

        if (selectionVersion != context.State.ReadSelectionVersion())
        {
            return;
        }

        if (!result.DidRun || !result.IsSuccess || result.IsStaleIgnored)
        {
            return;
        }

        context.Services.ApplySelectionState(result.SelectionState);
        context.Services.ApplySelectionBridgeState(result.SelectionState);
        if (showPendingPopups)
        {
            var popupResult = await context.Popup.ShowPendingSelectionPopupRequestsAsync(
                result.SelectionState,
                selectionVersion,
                cancellationToken);
            if (popupResult.DidChange)
            {
                ApplySelectionPopupState(context, popupResult.SelectionState);
            }

            return;
        }

        var confirmResult = context.Popup.ConfirmAllPendingSelectionPopups(result.SelectionState, selectedGame);
        if (confirmResult.DidChange)
        {
            ApplySelectionPopupState(context, confirmResult.SelectionState);
        }
    }

    public void ConfirmNextPendingPopup(MainSelectionInteractionContext context)
    {
        var result = context.Popup.ConfirmNextPendingPopup(context.State.ReadSelectionState());
        ApplySelectionPopupState(context, result);
    }

    public Task ShowPendingSelectionPopupRequestsAsync(
        MainSelectionInteractionContext context,
        long selectionVersion,
        CancellationToken cancellationToken)
    {
        return ShowPendingSelectionPopupRequestsAsyncCore(context, selectionVersion, cancellationToken);
    }

    public void ConfirmAllPendingSelectionPopups(MainSelectionInteractionContext context)
    {
        var selectedGame = context.State.ReadSelectedGame();
        var result = context.Popup.ConfirmAllPendingSelectionPopups(context.State.ReadSelectionState(), selectedGame);
        if (result.DidChange)
        {
            ApplySelectionPopupState(context, result.SelectionState);
        }
    }

    private static void ApplySelectionPopupState(
        MainSelectionInteractionContext context,
        ShellInstallSelectionState selectionState)
    {
        context.Services.ApplySelectionState(selectionState);
        context.Services.ApplySelectionBridgeState(selectionState);
    }

    private static async Task ShowPendingSelectionPopupRequestsAsyncCore(
        MainSelectionInteractionContext context,
        long selectionVersion,
        CancellationToken cancellationToken)
    {
        var result = await context.Popup.ShowPendingSelectionPopupRequestsAsync(
            context.State.ReadSelectionState(),
            selectionVersion,
            cancellationToken);
        if (result.DidChange)
        {
            ApplySelectionPopupState(context, result.SelectionState);
        }
    }
}

internal sealed class MainSelectionInteractionContext
{
    public required MainSelectionInteractionState State { get; init; }
    public required MainSelectionInteractionServices Services { get; init; }
    public required MainSelectionPopupCallbacks Popup { get; init; }
}

internal sealed class MainSelectionInteractionState
{
    public required Func<bool> IsInteractionBlocked { get; init; }
    public required Action<GameCardViewModel?> SetSelectedGame { get; init; }
    public required Func<GameCardViewModel, int> ResolveSelectedIndex { get; init; }
    public required Func<long> IncrementSelectionVersion { get; init; }
    public required Func<ShellInstallSelectionState> ReadSelectionState { get; init; }
    public required Func<long> ReadSelectionVersion { get; init; }
    public required Func<bool> IsMultiGpuBlocked { get; init; }
    public required Func<bool> IsGpuSelectionPending { get; init; }
    public required Func<GameCardViewModel?> ReadSelectedGame { get; init; }
}

internal sealed class MainSelectionInteractionServices
{
    public required Func<bool> CanSelect { get; init; }
    public required Action ApplyPrecheckRunningIntermediate { get; init; }
    public required Action<ShellInstallSelectionState> ApplySelectionState { get; init; }
    public required Action<ShellInstallSelectionState> ApplySelectionBridgeState { get; init; }
    public required Func<GameCardViewModel, int, ShellInstallSelectionState, GameSelectionFlowRequest> BuildSelectionRequest { get; init; }
    public required Func<GameSelectionFlowRequest, CancellationToken, Task<GameSelectionFlowResult>> SelectAsync { get; init; }
    public required Action<GameSelectionFlowResult> DispatchFlowLogs { get; init; }
    public required Action NavigateHome { get; init; }
}

internal sealed class MainSelectionPopupCallbacks
{
    public required Func<ShellInstallSelectionState, long, CancellationToken, Task<SelectionPopupChainResult>> ShowPendingSelectionPopupRequestsAsync { get; init; }
    public required Func<ShellInstallSelectionState, GameCardViewModel?, SelectionPopupChainResult> ConfirmAllPendingSelectionPopups { get; init; }
    public required Func<ShellInstallSelectionState, ShellInstallSelectionState> ConfirmNextPendingPopup { get; init; }
}
