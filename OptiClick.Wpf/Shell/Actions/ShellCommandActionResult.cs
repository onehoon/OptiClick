using OptiClick.Wpf.Models;
using OptiClick.Wpf.Shell.Support;

namespace OptiClick.Wpf.Shell.Actions;

public sealed record ShellCommandActionResult
{
    public string? SettingsStatusText { get; init; }
    public AppDialogRequest? DialogRequest { get; init; }
    public SupportActionResult? SupportActionResult { get; init; }
    public bool ShouldQueuePendingStartupNotice { get; init; }
    public bool ShouldWriteLog { get; init; }
    public bool LogAsWarning { get; init; }
    public string LogCategory { get; init; } = "";
    public string LogMessage { get; init; } = "";
}
