using OptiClick.Core.Install;
using OptiClick.Wpf.Install.Flow;
using OptiClick.Wpf.Shell.RuntimeData;

namespace OptiClick.Wpf.Install.Config;

public sealed class ConfigApplyFlowController
{
    private readonly IConfigProfileApplier? _configProfileApplier;
    private readonly IOptiScalerIniBaseApplier? _optiScalerIniBaseApplier;

    public ConfigApplyFlowController(
        IConfigProfileApplier? configProfileApplier,
        IOptiScalerIniBaseApplier? optiScalerIniBaseApplier)
    {
        _configProfileApplier = configProfileApplier;
        _optiScalerIniBaseApplier = optiScalerIniBaseApplier;
    }

    public ConfigApplyFlowResult Apply(ConfigApplyFlowRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Strings);

        if (!request.InstallSucceeded)
        {
            return new ConfigApplyFlowResult
            {
                IsSuccess = true
            };
        }

        var logs = new List<InstallFlowLogEntry>();
        if (_configProfileApplier is null)
        {
            logs.Add(Error("config-apply", "config apply failed reason=config_profile_applier_missing"));
            return Failure(request.Strings.InstallFailedConfigApply, "config_profile_applier_missing", logs, errorCount: 1);
        }

        var optiScalerIniSettings = NormalizeIniSettings(request.OptiScalerIniSettings);
        var commonOptiScalerIniSettings = NormalizeIniSettings(request.CommonOptiScalerIniSettings);
        if ((optiScalerIniSettings.Count > 0 || commonOptiScalerIniSettings.Count > 0)
            && _optiScalerIniBaseApplier is null)
        {
            logs.Add(Error("config-apply", "config apply failed reason=ini_profile_editor_missing"));
            return Failure(request.Strings.InstallFailedConfigApply, "ini_profile_editor_missing", logs, errorCount: 1);
        }

        try
        {
            var loggedErrorKeys = new HashSet<string>(StringComparer.Ordinal);
            ConfigProfileApplySummary? baseSummary = null;
            ConfigProfileApplySummary? commonSummary = null;
            if (optiScalerIniSettings.Count > 0)
            {
                baseSummary = _optiScalerIniBaseApplier!.ApplyBase(
                    request.Plan.TargetFolder,
                    "OptiScaler.ini",
                    optiScalerIniSettings);
                AddConfigErrorLogs(logs, baseSummary, loggedErrorKeys);
                AddIncompleteSummaryLog(logs, baseSummary);
            }

            if (commonOptiScalerIniSettings.Count > 0)
            {
                commonSummary = _optiScalerIniBaseApplier!.ApplyBase(
                    request.Plan.TargetFolder,
                    "OptiScaler.ini",
                    commonOptiScalerIniSettings,
                    "optiscaler_ini_common");
                AddConfigErrorLogs(logs, commonSummary, loggedErrorKeys);
                AddIncompleteSummaryLog(logs, commonSummary);
            }

            var applyContext = new ConfigProfileApplyContext
            {
                TargetPath = request.Plan.TargetFolder,
                GameData = ConfigProfileGameDataBuilder.BuildFromProfileRows(request.Plan.ProfileRows)
            };

            var profileResult = _configProfileApplier.Apply(applyContext);
            foreach (var summary in profileResult.Summaries)
            {
                AddConfigErrorLogs(logs, summary, loggedErrorKeys);
                AddIncompleteSummaryLog(logs, summary);
            }
            AddConfigErrorLogs(logs, profileResult.Errors, loggedErrorKeys);

            var hasBaseFailure = HasSummaryFailure(baseSummary) || HasSummaryFailure(commonSummary);
            var hasProfileFailure = profileResult.Errors.Count > 0
                                    || profileResult.Summaries.Any(static summary => !summary.Completed)
                                    || profileResult.Summaries.Any(static summary => CountErrors(summary) > 0);
            var totalErrorCount = CountTotalErrors([baseSummary, commonSummary], profileResult);
            if (hasBaseFailure || hasProfileFailure)
            {
                logs.Add(Error("config-apply", $"config apply failed target={request.Plan.TargetFolder}"));
                return Failure(
                    request.Strings.InstallFailedConfigApply,
                    "config_apply_failed",
                    logs,
                    Math.Max(totalErrorCount, 1));
            }

            return new ConfigApplyFlowResult
            {
                IsSuccess = true,
                ErrorCount = totalErrorCount,
                Logs = logs
            };
        }
        catch (Exception ex)
        {
            logs.Add(Error("config-apply", "config apply failed with exception", ex));
            return Failure(request.Strings.InstallFailedConfigApply, "config_apply_exception", logs, errorCount: 1);
        }
    }

    private static ConfigApplyFlowResult Failure(
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

    private static InstallFlowLogEntry Error(string category, string message, Exception? exception = null)
    {
        return new InstallFlowLogEntry
        {
            Level = "error",
            Category = category,
            Message = message,
            Exception = exception
        };
    }

    private static IReadOnlyDictionary<string, string> NormalizeIniSettings(IReadOnlyDictionary<string, string>? settings)
    {
        if (settings is null || settings.Count == 0)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        var normalized = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in settings)
        {
            var normalizedKey = (key ?? "").Trim();
            if (string.IsNullOrWhiteSpace(normalizedKey))
            {
                continue;
            }

            normalized[normalizedKey] = value ?? "";
        }

        return normalized;
    }

    private static void AddConfigErrorLogs(
        List<InstallFlowLogEntry> logs,
        ConfigProfileApplySummary summary,
        ISet<string> loggedErrorKeys)
    {
        AddConfigErrorLogs(logs, summary.Errors, loggedErrorKeys);
    }

    private static void AddIncompleteSummaryLog(List<InstallFlowLogEntry> logs, ConfigProfileApplySummary summary)
    {
        if (summary.Completed)
        {
            return;
        }

        logs.Add(Error(
            "config",
            $"{NormalizeStatusCode(summary.ProfileName, "config_profile")} stage status=error reason=incomplete target={Quote(LogTarget(summary.TargetPathHint))}"));
    }

    private static void AddConfigErrorLogs(
        List<InstallFlowLogEntry> logs,
        IEnumerable<ConfigProfileError> errors,
        ISet<string> loggedErrorKeys)
    {
        foreach (var error in errors)
        {
            if (!loggedErrorKeys.Add(BuildErrorKey(error)))
            {
                continue;
            }

            logs.Add(Error(
                "config",
                $"{NormalizeStatusCode(error.ProfileName, "config_profile")} item status=error reason={NormalizeStatusCode(error.ReasonCode, "unknown")} target={Quote(LogTarget(error.TargetPath))} key={Quote(LogTargetKey(error.TargetKey, error.ValuePath, error.Detail))} old={Quote(error.OldValue)} new={Quote(error.NewValue)} detail={Quote(error.Detail)}"));
        }
    }

    private static int CountErrors(ConfigProfileApplySummary summary)
    {
        return Math.Max(summary.ErrorCount, summary.Errors.Count);
    }

    private static bool HasSummaryFailure(ConfigProfileApplySummary? summary)
    {
        return summary is not null
               && (!summary.Completed
                   || summary.Errors.Count > 0
                   || CountErrors(summary) > 0);
    }

    private static int CountTotalErrors(
        IEnumerable<ConfigProfileApplySummary?> optiScalerIniSummaries,
        ConfigProfileApplyResult profileResult)
    {
        var total = optiScalerIniSummaries.Sum(static summary => summary is null ? 0 : CountErrors(summary));
        var profileErrorKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var summary in profileResult.Summaries)
        {
            foreach (var error in summary.Errors)
            {
                profileErrorKeys.Add(BuildErrorKey(error));
            }

            var unlistedErrors = Math.Max(0, summary.ErrorCount - summary.Errors.Count);
            total += unlistedErrors;
        }

        foreach (var error in profileResult.Errors)
        {
            profileErrorKeys.Add(BuildErrorKey(error));
        }

        total += profileErrorKeys.Count;
        return total;
    }

    private static string BuildErrorKey(ConfigProfileError error)
    {
        return string.Join(
            "\u001F",
            error.ProfileName ?? "",
            error.ReasonCode ?? "",
            error.TargetPath ?? "",
            error.TargetKey ?? "",
            error.ValuePath ?? "",
            error.Detail ?? "",
            error.OldValue ?? "",
            error.NewValue ?? "");
    }

    private static string LogTarget(string value)
    {
        var normalized = (value ?? "").Trim();
        return string.IsNullOrWhiteSpace(normalized) ? "-" : normalized;
    }

    private static string NormalizeStatusCode(string? value, string fallback)
    {
        var normalized = (value ?? "").Trim();
        return string.IsNullOrWhiteSpace(normalized) ? fallback : normalized;
    }

    private static string LogTargetKey(string targetKey, string valuePath, string fallback = "")
    {
        var key = (targetKey ?? "").Trim();
        var path = (valuePath ?? "").Trim();
        if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(path))
        {
            return $"{key}:{path}";
        }

        if (!string.IsNullOrWhiteSpace(key))
        {
            return key;
        }

        if (!string.IsNullOrWhiteSpace(path))
        {
            return path;
        }

        var detail = (fallback ?? "").Trim();
        return string.IsNullOrWhiteSpace(detail) ? "-" : detail;
    }

    private static string Quote(string value)
    {
        var safeValue = value ?? "";
        var escaped = safeValue
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal)
            .Replace("\r", "\\r", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal);
        return $"\"{escaped}\"";
    }
}
