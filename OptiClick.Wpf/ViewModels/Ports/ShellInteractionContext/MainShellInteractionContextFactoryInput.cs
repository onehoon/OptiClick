using OptiClick.Wpf.ViewModels.Ports.ShellInteractionContext.AppUpdate;
using OptiClick.Wpf.ViewModels.Ports.ShellInteractionContext.Details;
using OptiClick.Wpf.ViewModels.Ports.ShellInteractionContext.ShellCommand;
using OptiClick.Wpf.ViewModels.Ports.ShellInteractionContext.StartupAnnouncement;
using OptiClick.Wpf.ViewModels.Ports.ShellInteractionContext.UserSettings;

namespace OptiClick.Wpf.ViewModels.Ports.ShellInteractionContext;

internal sealed record MainShellInteractionContextFactoryInput
{
    public required MainShellCommandInteractionContextInput ShellCommand { get; init; }
    public required MainStartupAnnouncementInteractionContextInput StartupAnnouncement { get; init; }
    public required MainUserSettingsInteractionContextInput UserSettings { get; init; }
    public required MainAppUpdateInteractionContextInput AppUpdate { get; init; }
    public required MainDetailsDialogContextInput Details { get; init; }
}
