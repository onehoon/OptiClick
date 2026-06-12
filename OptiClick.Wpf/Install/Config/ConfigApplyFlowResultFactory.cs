using OptiClick.Core.Install;
using OptiClick.Wpf.Install.Flow;

namespace OptiClick.Wpf.Install.Config;

internal static class ConfigApplyFlowResultFactory
{
    public const string ConfigProfileApplierMissingCode = ConfigApplyFailureCodes.ConfigProfileApplierMissing;
    public const string IniProfileEditorMissingCode = ConfigApplyFailureCodes.IniProfileEditorMissing;
    public const string ConfigApplyFailedCode = ConfigApplyFailureCodes.ConfigApplyFailed;
    public const string ConfigApplyExceptionCode = ConfigApplyFailureCodes.ConfigApplyException;

    public static ConfigApplyFlowResult Skipped()
    {
        return new ConfigApplyFlowResult
        {
            IsSuccess = true
        };
    }

    public static ConfigApplyFlowResult Success(
        int errorCount,
        IReadOnlyList<InstallFlowLogEntry> logs)
    {
        return new ConfigApplyFlowResult
        {
            IsSuccess = true,
            ErrorCount = errorCount,
            Logs = logs
        };
    }

    public static ConfigApplyFlowResult Failure(
        string message,
        string code,
        IReadOnlyList<InstallFlowLogEntry> logs,
        int errorCount)
    {
        return new ConfigApplyFlowResult
        {
            IsSuccess = false,
            FailureMessage = message,
            FailureCode = code,
            ErrorCount = errorCount,
            Logs = logs
        };
    }
}
