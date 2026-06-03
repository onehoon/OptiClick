using OptiClick.Wpf.Models;

namespace OptiClick.Wpf.Shell.Support;

public sealed record SupportActionResult
{
    public bool IsSuccess { get; init; }
    public string StatusText { get; init; } = "";
    public string LogCategory { get; init; } = "";
    public string LogMessage { get; init; } = "";
    public AppDialogRequest? DialogRequest { get; init; }
}
