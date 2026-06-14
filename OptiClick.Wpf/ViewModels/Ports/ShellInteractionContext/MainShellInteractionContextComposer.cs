using OptiClick.Wpf.ViewModels.Ports.ShellInteractionContext.AppUpdate;
using OptiClick.Wpf.ViewModels.Ports.ShellInteractionContext.ShellCommand;
using OptiClick.Wpf.ViewModels.Ports.ShellInteractionContext.StartupAnnouncement;
using OptiClick.Wpf.ViewModels.Ports.ShellInteractionContext.UserSettings;

namespace OptiClick.Wpf.ViewModels.Ports.ShellInteractionContext;

internal sealed record MainShellInteractionContextCompositionInput
{
    public required MainViewModelCompositionDependencies Dependencies { get; init; }
    public required MainShellInteractionContextAccesses Accesses { get; init; }
    public required MainOptiScalerSettingsController OptiScalerSettingsController { get; init; }
}

internal static class MainShellInteractionContextComposer
{
    public static MainShellInteractionContextFactoryInput Compose(
        MainShellInteractionContextCompositionInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new MainShellInteractionContextFactoryInput
        {
            ShellCommand = MainShellCommandInteractionContextComposer.Compose(
                new MainShellCommandInteractionContextCompositionInput
                {
                    AppDependencies = input.Dependencies.App,
                    ShellDependencies = input.Dependencies.Shell,
                    Access = input.Accesses.ShellCommand
                }),
            StartupAnnouncement = MainStartupAnnouncementInteractionContextComposer.Compose(
                new MainStartupAnnouncementInteractionContextCompositionInput
                {
                    ShellDependencies = input.Dependencies.Shell,
                    Access = input.Accesses.StartupAnnouncement
                }),
            UserSettings = MainUserSettingsInteractionContextComposer.Compose(
                new MainUserSettingsInteractionContextCompositionInput
                {
                    UserSettingsAccess = input.Accesses.UserSettings,
                    LanguageAccess = input.Accesses.Language,
                    OptiScalerAccess = input.Accesses.OptiScaler,
                    OptiScalerSettingsController = input.OptiScalerSettingsController
                }),
            AppUpdate = MainAppUpdateInteractionContextComposer.Compose(
                new MainAppUpdateInteractionContextCompositionInput
                {
                    AppDependencies = input.Dependencies.App,
                    ShellDependencies = input.Dependencies.Shell,
                    UpdateDependencies = input.Dependencies.Features.Update,
                    Access = input.Accesses.AppUpdate
                })
        };
    }
}
