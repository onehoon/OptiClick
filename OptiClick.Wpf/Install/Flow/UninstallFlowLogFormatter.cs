namespace OptiClick.Wpf.Install.Flow;

public static class UninstallFlowLogFormatter
{
    public static string FormatPlanBuildStart(string? gameId, string targetPath)
    {
        return $"uninstall plan build start game_id={NormalizeStatusCode(gameId, "none")} target={NormalizeStatusCode(targetPath, "none")}";
    }

    public static string FormatPlanBuildResult(
        string status,
        int candidatesCount,
        int engineIniCleanupCount,
        int skippedCount,
        string? errorCode)
    {
        return
            $"uninstall plan build result status={NormalizeStatusCode(status, "none")} candidates={candidatesCount} engine_ini_cleanup={engineIniCleanupCount} skipped={skippedCount} error={NormalizeStatusCode(errorCode, "none")}";
    }

    public static string FormatPlanRejected(string status, string? errorCode)
    {
        return $"uninstall plan rejected status={NormalizeStatusCode(status, "none")} error={NormalizeStatusCode(errorCode, "none")}";
    }

    public static string FormatConfirmationResult(bool confirmed, string? dialogResult)
    {
        return $"uninstall confirmation result confirmed={confirmed} dialog_result={NormalizeStatusCode(dialogResult, "none")}";
    }

    public static string FormatExecuteResult(
        string status,
        int deletedCount,
        int failedCount,
        int skippedCount,
        int engineIniCleanedCount,
        int engineIniFailedCount,
        int engineIniSkippedCount,
        string? errorCode)
    {
        return
            $"uninstall execute result status={NormalizeStatusCode(status, "none")} deleted={deletedCount} failed={failedCount} skipped={skippedCount} engine_ini_cleaned={engineIniCleanedCount} engine_ini_failed={engineIniFailedCount} engine_ini_skipped={engineIniSkippedCount} error={NormalizeStatusCode(errorCode, "none")}";
    }

    public static string NormalizeStatusCode(string? value, string fallback)
    {
        var normalized = (value ?? "").Trim();
        return string.IsNullOrWhiteSpace(normalized) ? fallback : normalized;
    }
}
