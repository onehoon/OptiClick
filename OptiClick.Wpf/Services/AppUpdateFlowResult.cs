using OptiClick.Wpf.Models;
using OptiClick.Wpf.Shell.Flow;

namespace OptiClick.Wpf.Services;

public sealed record AppUpdateFlowLogEntry : IFlowLogEntry
{
    public string Level { get; init; } = "info";
    public string Category { get; init; } = "app-update";
    public string Message { get; init; } = "";
    public Exception? Exception { get; init; }
}

public abstract record AppUpdateFlowResult
{
    public string StatusText { get; init; } = "";
    public IReadOnlyList<AppUpdateFlowLogEntry> Logs { get; init; } = [];
}

public sealed record AppUpdateCheckResult : AppUpdateFlowResult
{
    public bool IsUpdateAvailable { get; init; }
    public AppUpdateInfo? UpdateInfo { get; init; }
    public AppDialogRequest DialogRequest { get; init; } = new();
}

public sealed record AppUpdateExecutionFlowResult : AppUpdateFlowResult
{
    public bool IsSuccess { get; init; }
    public bool ShouldShutdown { get; init; }
    public AppDialogRequest? DialogRequest { get; init; }
}
