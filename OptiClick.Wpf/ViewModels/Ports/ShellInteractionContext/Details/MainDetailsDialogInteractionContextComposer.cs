namespace OptiClick.Wpf.ViewModels.Ports.ShellInteractionContext.Details;

internal sealed record MainDetailsDialogInteractionContextCompositionInput
{
    public required MainShellResolvedDependencies ShellDependencies { get; init; }
    public required IMainDetailsDialogInteractionAccess Access { get; init; }
}

internal static class MainDetailsDialogInteractionContextComposer
{
    public static MainDetailsDialogContextInput Compose(
        MainDetailsDialogInteractionContextCompositionInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var access = input.Access;

        return new MainDetailsDialogContextInput
        {
            ReadSelectedGame = () => access.SelectedGame,
            ReadStrings = () => access.Strings,
            ShowDeferredDialog = input.ShellDependencies.DialogPresenter.ShowDeferred
        };
    }
}
