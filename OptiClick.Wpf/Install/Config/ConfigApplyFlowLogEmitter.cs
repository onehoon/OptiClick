using OptiClick.Core.Install;
using OptiClick.Wpf.Install.Flow;

namespace OptiClick.Wpf.Install.Config;

internal static class ConfigApplyFlowLogEmitter
{
    public static void AddMissingConfigProfileApplier(ICollection<InstallFlowLogEntry> logs)
    {
        logs.Add(InstallFlowLogEntryFactory.Error(
            "config-apply",
            ConfigApplyFlowLogFormatter.FormatMissingConfigProfileApplier()));
    }

    public static void AddMissingIniProfileEditor(ICollection<InstallFlowLogEntry> logs)
    {
        logs.Add(InstallFlowLogEntryFactory.Error(
            "config-apply",
            ConfigApplyFlowLogFormatter.FormatMissingIniProfileEditor()));
    }

    public static void AddConfigApplyFailed(ICollection<InstallFlowLogEntry> logs, string targetPath)
    {
        logs.Add(InstallFlowLogEntryFactory.Error(
            "config-apply",
            ConfigApplyFlowLogFormatter.FormatConfigApplyFailed(targetPath)));
    }

    public static void AddConfigApplyFailedWithException(
        ICollection<InstallFlowLogEntry> logs,
        Exception exception)
    {
        logs.Add(InstallFlowLogEntryFactory.Error(
            "config-apply",
            ConfigApplyFlowLogFormatter.FormatConfigApplyFailedWithException(),
            exception));
    }

    public static void AddAppliedEventLog(
        ICollection<InstallFlowLogEntry> logs,
        ConfigApplyEvent configEvent)
    {
        logs.Add(InstallFlowLogEntryFactory.Info(
            "config",
            ConfigApplyFlowLogFormatter.FormatAppliedItem(
                configEvent.ProfileName,
                configEvent.TargetKey,
                configEvent.ValuePath,
                configEvent.OldValue,
                configEvent.NewValue)));
    }

    public static void AddSkippedEventLog(
        ICollection<InstallFlowLogEntry> logs,
        ConfigApplyEvent configEvent)
    {
        logs.Add(InstallFlowLogEntryFactory.Info(
            "config",
            ConfigApplyFlowLogFormatter.FormatSkippedItem(
                configEvent.ProfileName,
                configEvent.ReasonCode,
                configEvent.TargetKey,
                configEvent.ValuePath,
                configEvent.Detail,
                configEvent.OldValue,
                configEvent.NewValue)));
    }

    public static void AddIncompleteEventLog(
        ICollection<InstallFlowLogEntry> logs,
        ConfigApplyEvent configEvent)
    {
        if (!string.Equals(
                configEvent.Action,
                ConfigApplyEventActions.StageError,
                StringComparison.Ordinal))
        {
            return;
        }

        logs.Add(InstallFlowLogEntryFactory.Error(
            "config",
            ConfigApplyFlowLogFormatter.FormatIncompleteSummary(configEvent.ProfileName, configEvent.TargetPath)));
    }

    public static void AddConfigIssueLogs(
        ICollection<InstallFlowLogEntry> logs,
        IEnumerable<ConfigApplyIssue> issues,
        ISet<string> loggedErrorKeys)
    {
        foreach (var issue in issues)
        {
            if (!loggedErrorKeys.Add(ConfigApplyIssueKeyPolicy.Build(issue)))
            {
                continue;
            }

            logs.Add(InstallFlowLogEntryFactory.Error(
                "config",
                ConfigApplyFlowLogFormatter.FormatErrorItem(
                    issue.ProfileName,
                    issue.ReasonCode,
                    issue.TargetKey,
                    issue.ValuePath,
                    issue.Detail,
                    issue.OldValue,
                    issue.NewValue)));
        }
    }
}
