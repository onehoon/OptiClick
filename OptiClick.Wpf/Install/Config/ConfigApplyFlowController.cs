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
            return Failure(request.Strings.InstallFailedConfigApply, "config_profile_applier_missing", logs);
        }

        var optiScalerIniSettings = NormalizeIniSettings(request.OptiScalerIniSettings);
        if (optiScalerIniSettings.Count > 0 && _optiScalerIniBaseApplier is null)
        {
            logs.Add(Error("config-apply", "config apply failed reason=ini_profile_editor_missing"));
            return Failure(request.Strings.InstallFailedConfigApply, "ini_profile_editor_missing", logs);
        }

        try
        {
            ConfigProfileApplySummary? baseSummary = null;
            if (optiScalerIniSettings.Count > 0)
            {
                logs.Add(Info("config", "OptiScaler.ini base apply start"));
                baseSummary = _optiScalerIniBaseApplier!.ApplyBase(
                    request.Plan.TargetFolder,
                    "OptiScaler.ini",
                    optiScalerIniSettings);
                AddConfigItemLogs(logs, baseSummary);
                logs.Add(Info(
                    "config",
                    $"OptiScaler.ini base apply completed completed={baseSummary.Completed} applied={CountApplied(baseSummary)} skipped={CountSkipped(baseSummary)} errors={CountErrors(baseSummary)}"));
            }

            var profileRowCounts = CountProfileRowsByName(request.Plan.ProfileRows);
            logs.Add(Info("config", $"profile apply start rows={profileRowCounts.Values.Sum()}"));
            var applyContext = new ConfigProfileApplyContext
            {
                TargetPath = request.Plan.TargetFolder,
                GameData = ConfigProfileGameDataBuilder.BuildFromProfileRows(request.Plan.ProfileRows)
            };

            var profileResult = _configProfileApplier.Apply(applyContext);
            foreach (var summary in profileResult.Summaries)
            {
                var rowCount = GetProfileRowCount(profileRowCounts, summary.ProfileName);
                if (IsProfileNotApplicable(summary, rowCount))
                {
                    logs.Add(Info(
                        "config",
                        $"{summary.ProfileName} not_applicable reason=no_profile_rows applied=0 skipped=0 errors=0"));
                    continue;
                }

                AddConfigItemLogs(logs, summary);
                logs.Add(Info(
                    "config",
                    $"{summary.ProfileName} completed row_count={rowCount} completed={summary.Completed} applied={CountApplied(summary)} skipped={CountSkipped(summary)} errors={CountErrors(summary)}"));
            }

            logs.Add(Info(
                "config",
                $"profile apply completed stages={profileResult.Summaries.Count} errors={profileResult.Errors.Count}"));

            var hasBaseFailure = baseSummary is not null
                                 && (!baseSummary.Completed
                                     || baseSummary.Errors.Count > 0
                                     || CountErrors(baseSummary) > 0);
            var hasProfileFailure = profileResult.Errors.Count > 0
                                    || profileResult.Summaries.Any(static summary => !summary.Completed)
                                    || profileResult.Summaries.Any(static summary => CountErrors(summary) > 0);
            if (hasBaseFailure || hasProfileFailure)
            {
                logs.Add(Error("config-apply", $"config apply failed target={request.Plan.TargetFolder}"));
                return Failure(request.Strings.InstallFailedConfigApply, "config_apply_failed", logs);
            }

            return new ConfigApplyFlowResult
            {
                IsSuccess = true,
                Logs = logs
            };
        }
        catch (Exception ex)
        {
            logs.Add(Error("config-apply", "config apply failed with exception", ex));
            return Failure(request.Strings.InstallFailedConfigApply, "config_apply_exception", logs);
        }
    }

    private static ConfigApplyFlowResult Failure(
        string message,
        string code,
        IReadOnlyList<InstallFlowLogEntry> logs)
    {
        return new ConfigApplyFlowResult
        {
            IsSuccess = false,
            FailureMessage = message,
            FailureCode = code,
            Logs = logs
        };
    }

    private static InstallFlowLogEntry Info(string category, string message)
    {
        return new InstallFlowLogEntry
        {
            Level = "info",
            Category = category,
            Message = message
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

    private static Dictionary<string, int> CountProfileRowsByName(AttachedRuntimeProfileRows rows)
    {
        if (rows is null)
        {
            return new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        }

        return new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            [ConfigProfileNames.GameIniProfile] = rows.GameIniProfileRows.Count,
            [ConfigProfileNames.GameUnrealIniProfile] = rows.GameUnrealIniProfileRows.Count,
            [ConfigProfileNames.GameXmlProfile] = rows.GameXmlProfileRows.Count,
            [ConfigProfileNames.GameJsonProfile] = rows.GameJsonProfileRows.Count,
            [ConfigProfileNames.EngineIniProfile] = rows.EngineIniProfileRows.Count,
            [ConfigProfileNames.RegistryProfile] = rows.RegistryProfileRows.Count
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

    private static int GetProfileRowCount(IReadOnlyDictionary<string, int> profileRowCounts, string profileName)
    {
        if (profileRowCounts.TryGetValue(profileName, out var rowCount))
        {
            return rowCount;
        }

        return 0;
    }

    private static bool IsProfileNotApplicable(ConfigProfileApplySummary summary, int rowCount)
    {
        return rowCount == 0
               && CountApplied(summary) == 0
               && CountSkipped(summary) == 0
               && CountErrors(summary) == 0;
    }

    private static void AddConfigItemLogs(List<InstallFlowLogEntry> logs, ConfigProfileApplySummary summary)
    {
        foreach (var applied in summary.Applied)
        {
            logs.Add(Info(
                "config",
                $"{summary.ProfileName} item status=applied target={Quote(LogTarget(applied.TargetPath))} key={Quote(LogTargetKey(applied.TargetKey, applied.ValuePath))} old={Quote(applied.OldValue)} new={Quote(applied.NewValue)}"));
        }

        foreach (var skipped in summary.Skipped)
        {
            logs.Add(Info(
                "config",
                $"{summary.ProfileName} item status=skipped reason={NormalizeStatusCode(skipped.ReasonCode, "unknown")} target={Quote(LogTarget(skipped.TargetPath))} key={Quote(LogTargetKey(skipped.TargetKey, skipped.ValuePath, skipped.Detail))} old={Quote(skipped.OldValue)} new={Quote(skipped.NewValue)} detail={Quote(skipped.Detail)}"));
        }

        foreach (var error in summary.Errors)
        {
            logs.Add(Error(
                "config",
                $"{summary.ProfileName} item status=error reason={NormalizeStatusCode(error.ReasonCode, "unknown")} target={Quote(LogTarget(error.TargetPath))} key={Quote(LogTargetKey(error.TargetKey, error.ValuePath, error.Detail))} old={Quote(error.OldValue)} new={Quote(error.NewValue)} detail={Quote(error.Detail)}"));
        }
    }

    private static int CountApplied(ConfigProfileApplySummary summary)
    {
        return Math.Max(summary.AppliedCount, summary.Applied.Count);
    }

    private static int CountSkipped(ConfigProfileApplySummary summary)
    {
        return Math.Max(summary.SkippedCount, summary.Skipped.Count);
    }

    private static int CountErrors(ConfigProfileApplySummary summary)
    {
        return Math.Max(summary.ErrorCount, summary.Errors.Count);
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
