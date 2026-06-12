using OptiClick.Wpf.Localization;
using OptiClick.Wpf.Models;

namespace OptiClick.Wpf.ViewModels.Ports.ShellInteractionContext.Details;

internal sealed record MainDetailsDialogContextInput
{
    public required Func<GameCardViewModel?> ReadSelectedGame { get; init; }
    public required Func<AppStrings> ReadStrings { get; init; }
    public required Action<AppDialogRequest> ShowDeferredDialog { get; init; }
}
