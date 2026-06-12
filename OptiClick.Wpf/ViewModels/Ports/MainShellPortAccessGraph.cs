using OptiClick.Wpf.ViewModels.Ports.App;
using OptiClick.Wpf.ViewModels.Ports.Install;
using OptiClick.Wpf.ViewModels.Ports.Localization;
using OptiClick.Wpf.ViewModels.Ports.Runtime;
using OptiClick.Wpf.ViewModels.Ports.Selection;
using OptiClick.Wpf.ViewModels.Ports.ShellInteractionContext;
using OptiClick.Wpf.ViewModels.Ports.Startup;
using OptiClick.Wpf.ViewModels.Ports.Ui;

namespace OptiClick.Wpf.ViewModels.Ports;

internal sealed record MainShellPortAccessGraph
{
    public required IMainShellAppPortAccess App { get; init; }
    public required IMainShellRuntimePortAccess Runtime { get; init; }
    public required IMainShellStartupPortAccess Startup { get; init; }
    public required IMainShellSelectionPortAccess Selection { get; init; }
    public required IMainShellInstallPortAccess Install { get; init; }
    public required IMainShellUiPortAccess Ui { get; init; }
    public required IMainShellLocalizationPortAccess Localization { get; init; }
    public required MainShellInteractionContextAccesses ShellInteractionContext { get; init; }
}
