using System.Globalization;

namespace OptiClick.Wpf.Install.Config;

public static class ConfigApplyFlowLogFormatter
{
    public static string FormatAppliedItem(
        string? profileName,
        string targetKey,
        string valuePath,
        string oldValue,
        string newValue)
    {
        return
            $"{NormalizeStatusCode(profileName, "config_profile")} item status=applied key={Quote(LogTargetKey(targetKey, valuePath))} old={Quote(oldValue)} new={Quote(newValue)}";
    }

    public static string FormatSkippedItem(
        string? profileName,
        string reasonCode,
        string targetKey,
        string valuePath,
        string detail,
        string oldValue,
        string newValue)
    {
        return
            $"{NormalizeStatusCode(profileName, "config_profile")} item status=skipped reason={NormalizeStatusCode(reasonCode, "unknown")} key={Quote(LogTargetKey(targetKey, valuePath, detail))} old={Quote(oldValue)} new={Quote(newValue)} detail={Quote(detail)}";
    }

    public static string FormatErrorItem(
        string? profileName,
        string reasonCode,
        string targetKey,
        string valuePath,
        string detail,
        string oldValue,
        string newValue)
    {
        return
            $"{NormalizeStatusCode(profileName, "config_profile")} item status=error reason={NormalizeStatusCode(reasonCode, "unknown")} key={Quote(LogTargetKey(targetKey, valuePath, detail))} old={Quote(oldValue)} new={Quote(newValue)} detail={Quote(detail)}";
    }

    public static string FormatIncompleteSummary(string? profileName, string targetPathHint)
    {
        return $"{NormalizeStatusCode(profileName, "config_profile")} stage status=error reason=incomplete target={Quote(LogTarget(targetPathHint))}";
    }

    public static string FormatMissingConfigProfileApplier()
    {
        return "config apply failed reason=config_profile_applier_missing";
    }

    public static string FormatMissingIniProfileEditor()
    {
        return "config apply failed reason=ini_profile_editor_missing";
    }

    public static string FormatConfigApplyFailed(string targetPath)
    {
        return $"config apply failed target={Quote(LogTarget(targetPath))}";
    }

    public static string FormatConfigApplyFailedWithException()
    {
        return "config apply failed with exception";
    }

    public static string NormalizeStatusCode(string? value, string fallback)
    {
        var normalized = (value ?? "").Trim();
        return string.IsNullOrWhiteSpace(normalized) ? fallback : normalized;
    }

    public static string LogTarget(string value)
    {
        var normalized = (value ?? "").Trim();
        return string.IsNullOrWhiteSpace(normalized) ? "-" : normalized;
    }

    public static string Quote(string value)
    {
        var safeValue = value ?? "";
        var escaped = safeValue
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal)
            .Replace("\r", "\\r", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal);
        return $"\"{escaped}\"";
    }

    public static string LogTargetKey(string targetKey, string valuePath, string fallback = "")
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

    public static string Format(string template, params object[] args)
    {
        return string.Format(CultureInfo.CurrentCulture, template ?? "", args ?? []);
    }
}
