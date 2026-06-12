using OptiClick.Core.Install;
using OptiClick.Wpf.Install.Execution;
using OptiClick.Wpf.Shell.RuntimeData;
using InfrastructureUninstall = OptiClick.Infrastructure.Install.Uninstall;

namespace OptiClick.Wpf.Install.Flow;

internal sealed record UninstallFlowExecutionRequest
{
    public required InstallExecutionDescriptor ExecutionDescriptor { get; init; }
    public required string SelectedGameId { get; init; }
    public required string TargetPath { get; init; }
    public required UninstallFlowSelectionSnapshot SelectionSnapshot { get; init; }
    public required IReadOnlyList<RuntimeDataRawRow> EngineIniProfileRows { get; init; }
}

internal sealed record UninstallFlowPlanResult
{
    public required InfrastructureUninstall.UninstallPlan Plan { get; init; }
    public bool CanExecute { get; init; }
    public UninstallFlowDialogKind DialogKind { get; init; } = UninstallFlowDialogKind.None;
    public IReadOnlyList<UninstallFlowLogEntry> Logs { get; init; } = [];
}

internal sealed record UninstallFlowExecutionResult
{
    public required InfrastructureUninstall.UninstallExecutionResult ExecutionResult { get; init; }
    public UninstallFlowDialogKind DialogKind { get; init; } = UninstallFlowDialogKind.None;
    public bool ShouldRefreshSelection { get; init; }
    public IReadOnlyList<UninstallFlowLogEntry> Logs { get; init; } = [];
}

internal enum UninstallFlowDialogKind
{
    None,
    NoRemovableItems,
    ValidationFailed,
    Confirmation,
    Completion
}

internal sealed record UninstallFlowLogEntry
{
    public UninstallFlowLogLevel Level { get; init; } = UninstallFlowLogLevel.Info;
    public string Message { get; init; } = "";

    public static UninstallFlowLogEntry Info(string message)
    {
        return new UninstallFlowLogEntry
        {
            Level = UninstallFlowLogLevel.Info,
            Message = message ?? ""
        };
    }

    public static UninstallFlowLogEntry Warning(string message)
    {
        return new UninstallFlowLogEntry
        {
            Level = UninstallFlowLogLevel.Warning,
            Message = message ?? ""
        };
    }
}

internal enum UninstallFlowLogLevel
{
    Info,
    Warning
}
