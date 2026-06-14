using OptiClick.Wpf.ViewModels.Ports.ShellInteractionContext.AppUpdate;
using OptiClick.Wpf.ViewModels.Ports.ShellInteractionContext.Language;
using OptiClick.Wpf.ViewModels.Ports.ShellInteractionContext.OptiScaler;
using OptiClick.Wpf.ViewModels.Ports.ShellInteractionContext.ShellCommand;
using OptiClick.Wpf.ViewModels.Ports.ShellInteractionContext.StartupAnnouncement;
using OptiClick.Wpf.ViewModels.Ports.ShellInteractionContext.UserSettings;

namespace OptiClick.Wpf.ViewModels.Ports.ShellInteractionContext;

internal sealed record MainShellInteractionContextAccesses
{
    public required IMainShellCommandInteractionAccess ShellCommand { get; init; }
    public required IMainStartupAnnouncementInteractionAccess StartupAnnouncement { get; init; }
    public required IMainAppUpdateInteractionAccess AppUpdate { get; init; }
    public required IMainUserSettingsInteractionAccess UserSettings { get; init; }
    public required IMainLanguagePreferenceInteractionAccess Language { get; init; }
    public required IMainOptiScalerSettingsInteractionAccess OptiScaler { get; init; }
}
