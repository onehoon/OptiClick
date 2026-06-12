using OptiClick.Wpf.Localization;

namespace OptiClick.Wpf.ViewModels.Ports.ShellInteractionContext.Details;

internal interface IMainDetailsDialogInteractionAccess
{
    AppStrings Strings { get; }
    GameCardViewModel? SelectedGame { get; }
}
