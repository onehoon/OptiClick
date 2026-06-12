using OptiClick.Core.Runtime;
using OptiClick.Wpf.Shell.RuntimeData;

namespace OptiClick.Wpf.ViewModels.Ports.ShellInteractionContext.StartupAnnouncement;

internal interface IMainStartupAnnouncementInteractionAccess
{
    AppLanguage SelectedLanguage { get; }
    RuntimeContext LatestRuntimeContext { get; }
    RemoteRuntimeData LatestRuntimeData { get; }
}
