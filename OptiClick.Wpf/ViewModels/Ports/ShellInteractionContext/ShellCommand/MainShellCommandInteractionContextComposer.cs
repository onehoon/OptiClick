using OptiClick.Wpf.ViewModels.Ports.ShellInteractionContext;

namespace OptiClick.Wpf.ViewModels.Ports.ShellInteractionContext.ShellCommand;

internal sealed record MainShellCommandInteractionContextCompositionInput
{
    public required MainAppResolvedDependencies AppDependencies { get; init; }
    public required MainShellResolvedDependencies ShellDependencies { get; init; }
    public required IMainShellCommandInteractionAccess Access { get; init; }
}

internal static class MainShellCommandInteractionContextComposer
{
    public static MainShellCommandInteractionContextInput Compose(
        MainShellCommandInteractionContextCompositionInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var access = input.Access;

        return new MainShellCommandInteractionContextInput
        {
            ResultApplier = input.ShellDependencies.ResultApplier,
            ReadStrings = () => access.Strings,
            ReadSelectedLanguage = () => access.SelectedLanguage,
            ReadLatestRuntimeContext = () => access.LatestRuntimeContext,
            ReadCurrentAppVersion = () => MainShellInteractionContextUtilities.NormalizeAppVersion(
                input.AppDependencies.AppVersionProvider.GetCurrentVersion()),
            ReadLogDirectory = () => input.AppDependencies.AppLogger.LogDirectory,
            ShowDialogAsync = input.ShellDependencies.DialogPresenter.ShowSafelyAsync,
            ShowDeferredDialog = input.ShellDependencies.DialogPresenter.ShowDeferred,
            ApplyStateUpdate = access.ApplyStateUpdate,
            ApplyDeferredStateUpdate = access.ApplyDeferredStateUpdate,
            ApplyAppLog = (shouldWrite, asWarning, category, message) =>
                MainShellInteractionContextUtilities.ApplyAppLog(
                    input.AppDependencies.AppLogger,
                    shouldWrite,
                    asWarning,
                    category,
                    message)
        };
    }
}
