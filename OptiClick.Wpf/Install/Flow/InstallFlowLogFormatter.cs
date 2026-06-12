using System.Globalization;
using System.Linq;
using OptiClick.Wpf.Install.Execution;
using CoreComponentInstallOperation = OptiClick.Core.Install.Components.ComponentInstallOperation;

namespace OptiClick.Wpf.Install.Flow;

public static class InstallFlowLogFormatter
{
    public static string FormatGateBlocked(string? reasonCode, string? stage)
    {
        return $"blocked reason={NormalizeStatusCode(reasonCode, "unknown")} stage={NormalizeStatusCode(stage, "none")}";
    }

    public static string FormatInstallCompletionLog(
        string gameId,
        ComponentInstallContext context,
        ComponentInstallResult installResult,
        InstallResultApplyResult applyResult,
        long durationMs)
    {
        var steps = installResult?.Steps ?? Array.Empty<ComponentInstallStepResult>();
        var failedStep = installResult?.FailedStep
            ?? steps.FirstOrDefault(static step => step.Status == ComponentInstallStatus.Failed);
        var failureCode = !string.IsNullOrWhiteSpace(applyResult.ConfigFailureCode)
            ? applyResult.ConfigFailureCode
            : NormalizeStatusCode(failedStep?.ErrorCode, "none");
        var baseMessage =
            $"completed success={FormatBool(applyResult.FinalSuccess)} game_id={NormalizeStatusCode(gameId, "missing")} variant={NormalizeStatusCode(context.OptiScalerVariant, "missing")} version={NormalizeStatusCode(context.OptiScalerDisplayVersion, NormalizeStatusCode(context.OptiScalerVersion, "missing"))} final_dll={NormalizeStatusCode(context.FinalDllName, "missing")} components={FormatComponentCounts(steps)} config_errors={applyResult.ConfigErrorCount} duration_ms={durationMs}";

        if (applyResult.FinalSuccess)
        {
            return baseMessage;
        }

        var failedComponent = failedStep is not null
            ? failedStep.Component.ToString()
            : !string.IsNullOrWhiteSpace(applyResult.ConfigFailureCode)
                ? "config"
                : "none";
        var failureMessage = failedStep is not null
            ? failedStep.Message
            : !string.IsNullOrWhiteSpace(applyResult.ConfigFailureCode)
                ? "config apply failed"
                : "-";

        return $"{baseMessage} failed_component={NormalizeStatusCode(failedComponent, "none")} code={NormalizeStatusCode(failureCode, "unknown_error")} message={Quote(NormalizeStatusCode(failureMessage, "-"))}";
    }

    public static string FormatComponentStep(ComponentInstallStepResult step)
    {
        return
            $"component status={FormatComponentStatus(step.Status)} component={NormalizeStatusCode(step.Component.ToString(), "unknown")} operations={FormatOperationCounts(step.Operations)} code={NormalizeStatusCode(step.ErrorCode, "none")} message={Quote(NormalizeStatusCode(step.Message, "-"))}";
    }

    public static string NormalizeStatusCode(string? value, string fallback)
    {
        var normalized = (value ?? "").Trim();
        return string.IsNullOrWhiteSpace(normalized) ? fallback : normalized;
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

    public static string Format(string template, params object[] args)
    {
        return string.Format(CultureInfo.CurrentCulture, template ?? "", args ?? []);
    }

    private static string FormatBool(bool value)
    {
        return value.ToString().ToLowerInvariant();
    }

    private static string FormatComponentStatus(ComponentInstallStatus status)
    {
        return status switch
        {
            ComponentInstallStatus.Success => "success",
            ComponentInstallStatus.Skipped => "skipped",
            ComponentInstallStatus.Failed => "failed",
            _ => NormalizeStatusCode(status.ToString(), "unknown").ToLowerInvariant()
        };
    }

    private static string FormatOperationCounts(IReadOnlyList<CoreComponentInstallOperation> operations)
    {
        if (operations.Count == 0)
        {
            return "none";
        }

        return string.Join(
            ",",
            operations
                .GroupBy(static operation => NormalizeStatusCode(operation.Kind, "unknown"), StringComparer.OrdinalIgnoreCase)
                .OrderBy(static group => group.Key, StringComparer.OrdinalIgnoreCase)
                .Select(static group => $"{group.Key}:{group.Count()}"));
    }

    private static string FormatComponentCounts(IReadOnlyList<ComponentInstallStepResult> steps)
    {
        if (steps.Count == 0)
        {
            return "none";
        }

        var success = steps.Count(static step => step.Status == ComponentInstallStatus.Success);
        var skipped = steps.Count(static step => step.Status == ComponentInstallStatus.Skipped);
        var failed = steps.Count(static step => step.Status == ComponentInstallStatus.Failed);
        return $"success:{success},skipped:{skipped},failed:{failed}";
    }
}
