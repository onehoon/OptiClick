using OptiClick.Wpf.Localization;
using OptiClick.Wpf.Models;

namespace OptiClick.Wpf.ViewModels.Ports.ShellInteractionContext.Details;

internal sealed record MainDetailsDialogContext
{
    public required GameCardViewModel? SelectedGame { get; init; }
    public required AppStrings Strings { get; init; }
    public required Action<AppDialogRequest> ShowDeferredDialog { get; init; }
}
