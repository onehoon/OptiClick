using System;
using System.Collections.Generic;
using System.Linq;
using OptiClick.Wpf.Install.Execution;

namespace OptiClick.Wpf.Install.Flow;

internal static class InstallFlowLogEmitter
{
    public static void AddDependenciesMissingLog(ICollection<InstallFlowLogEntry> logs, string message)
    {
        logs.Add(InstallFlowLogEntryFactory.Error(
            "install",
            string.IsNullOrWhiteSpace(message) ? "install requested but execution dependencies are missing" : message));
    }

    public static void AddGateBlockedLog(
        ICollection<InstallFlowLogEntry> logs,
        string? reasonCode,
        string? stage)
    {
        logs.Add(InstallFlowLogEntryFactory.Warning(
            "install",
            InstallFlowLogFormatter.FormatGateBlocked(reasonCode, stage)));
    }

    public static void AddExecutionExceptionLog(
        ICollection<InstallFlowLogEntry> logs,
        string? message,
        Exception exception)
    {
        logs.Add(InstallFlowLogEntryFactory.Error(
            "install",
            message ?? "execution failed with exception",
            exception));
    }

    public static void AddComponentStepLogs(
        ICollection<InstallFlowLogEntry> logs,
        ComponentInstallResult installResult)
    {
        var loggedStepKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var step in installResult.Steps)
        {
            AddComponentStepLog(logs, step);
            loggedStepKeys.Add(BuildComponentStepKey(step));
        }

        if (installResult.FailedStep is not null
            && loggedStepKeys.Add(BuildComponentStepKey(installResult.FailedStep)))
        {
            AddComponentStepLog(logs, installResult.FailedStep);
        }
    }

    public static void AddCompletionLog(
        ICollection<InstallFlowLogEntry> logs,
        string gameId,
        ComponentInstallContext context,
        ComponentInstallResult installResult,
        InstallResultApplyResult applyResult,
        long durationMs)
    {
        var message = InstallFlowLogFormatter.FormatInstallCompletionLog(
            gameId,
            context,
            installResult,
            applyResult,
            durationMs);

        logs.Add(applyResult.FinalSuccess
            ? InstallFlowLogEntryFactory.Info("install", message)
            : InstallFlowLogEntryFactory.Error("install", message));
    }

    private static void AddComponentStepLog(
        ICollection<InstallFlowLogEntry> logs,
        ComponentInstallStepResult step)
    {
        var message = InstallFlowLogFormatter.FormatComponentStep(step);
        if (step.Status == ComponentInstallStatus.Failed)
        {
            logs.Add(InstallFlowLogEntryFactory.Error("install", message));
            return;
        }

        logs.Add(InstallFlowLogEntryFactory.Info("install", message));
    }

    private static string BuildComponentStepKey(ComponentInstallStepResult step)
    {
        return string.Join(
            "\u001F",
            step.Component.ToString(),
            step.Status.ToString(),
            step.ErrorCode ?? "",
            step.Message ?? "",
            string.Join(
                "\u001E",
                step.Operations.Select(static operation => string.Join(
                    "\u001D",
                    operation.Kind ?? "",
                    operation.Source ?? "",
                    operation.Destination ?? ""))));
    }
}
