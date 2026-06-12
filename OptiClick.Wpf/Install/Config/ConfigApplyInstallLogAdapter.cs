using OptiClick.Core.Install;
using OptiClick.Wpf.Install.Flow;

namespace OptiClick.Wpf.Install.Config;

public sealed class ConfigApplyInstallLogAdapter
{
    public IReadOnlyList<InstallFlowLogEntry> Convert(
        ConfigApplyApplicationResult result,
        string targetPath)
    {
        ArgumentNullException.ThrowIfNull(result);

        var logs = new List<InstallFlowLogEntry>();
        if (string.Equals(
                result.FailureCode,
                ConfigApplyFlowResultFactory.ConfigProfileApplierMissingCode,
                StringComparison.Ordinal))
        {
            ConfigApplyFlowLogEmitter.AddMissingConfigProfileApplier(logs);
            return logs;
        }

        if (string.Equals(
                result.FailureCode,
                ConfigApplyFlowResultFactory.IniProfileEditorMissingCode,
                StringComparison.Ordinal))
        {
            ConfigApplyFlowLogEmitter.AddMissingIniProfileEditor(logs);
            return logs;
        }

        var loggedErrorKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var configEvent in result.Events)
        {
            if (string.Equals(
                    configEvent.Action,
                    ConfigApplyEventActions.Applied,
                    StringComparison.Ordinal))
            {
                ConfigApplyFlowLogEmitter.AddAppliedEventLog(logs, configEvent);
                continue;
            }

            if (string.Equals(
                    configEvent.Action,
                    ConfigApplyEventActions.Skipped,
                    StringComparison.Ordinal))
            {
                ConfigApplyFlowLogEmitter.AddSkippedEventLog(logs, configEvent);
                continue;
            }

            ConfigApplyFlowLogEmitter.AddIncompleteEventLog(logs, configEvent);
        }

        ConfigApplyFlowLogEmitter.AddConfigIssueLogs(logs, result.Issues, loggedErrorKeys);

        if (string.Equals(
                result.FailureCode,
                ConfigApplyFlowResultFactory.ConfigApplyFailedCode,
                StringComparison.Ordinal))
        {
            ConfigApplyFlowLogEmitter.AddConfigApplyFailed(logs, targetPath);
        }

        if (result.Exception is not null)
        {
            ConfigApplyFlowLogEmitter.AddConfigApplyFailedWithException(logs, result.Exception);
        }

        return logs;
    }
}
