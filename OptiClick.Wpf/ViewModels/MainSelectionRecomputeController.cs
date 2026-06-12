using System;
using System.Threading;
using System.Threading.Tasks;
using OptiClick.Wpf.Models;

namespace OptiClick.Wpf.ViewModels;

internal sealed class MainSelectionRecomputeController
{
    public Task RecomputeAsync(
        MainSelectionRecomputeContext context,
        CancellationToken cancellationToken,
        bool navigateHome)
    {
        ArgumentNullException.ThrowIfNull(context);
        var selectedGame = context.State.ReadSelectedGame();
        if (selectedGame is null)
        {
            return Task.CompletedTask;
        }

        return context.Services.RecomputeSelectionAsync(
            selectedGame,
            cancellationToken,
            navigateHome,
            context.State.ShouldShowPopupAfterSelection);
    }
}

internal sealed class MainSelectionRecomputeContext
{
    public required MainSelectionRecomputeState State { get; init; }
    public required MainSelectionRecomputeServices Services { get; init; }
}

internal sealed class MainSelectionRecomputeState
{
    public required Func<GameCardViewModel?> ReadSelectedGame { get; init; }
    public required bool ShouldShowPopupAfterSelection { get; init; }
}

internal sealed class MainSelectionRecomputeServices
{
    public required Func<GameCardViewModel, CancellationToken, bool, bool, Task> RecomputeSelectionAsync { get; init; }
}
