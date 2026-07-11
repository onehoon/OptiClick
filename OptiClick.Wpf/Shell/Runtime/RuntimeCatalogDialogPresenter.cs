using OptiClick.Wpf.Models;

namespace OptiClick.Wpf.Shell.Runtime;

public sealed class RuntimeCatalogDialogPresenter
{
    public AppDialogRequest BuildUnexpectedErrorDialog(
        RuntimeCatalogFlowText text)
    {
        return BuildWarning(
            "remote_catalog_unexpected_error",
            text.RuntimeCatalogUnexpectedErrorTitle,
            text.RuntimeCatalogUnexpectedErrorSummary,
            text.RuntimeCatalogUnexpectedErrorBullet1,
            text.RuntimeCatalogUnexpectedErrorBullet2) with
        {
            DisplayErrorCode = "remote_catalog_failed"
        };
    }

    public AppDialogRequest BuildSkippedDialog(
        string errorCode,
        RuntimeCatalogFlowText text)
    {
        var normalizedCode = NormalizeStatusCode(errorCode, "runtime_data_skipped");
        return BuildWarning(
            normalizedCode,
            text.RuntimeCatalogSkippedTitle,
            text.RuntimeCatalogSkippedSummary,
            text.RuntimeCatalogSkippedBullet1,
            text.RuntimeCatalogSkippedBullet2) with
        {
            DisplayErrorCode = ResolveDisplayErrorCode(normalizedCode)
        };
    }

    public AppDialogRequest BuildFailedDialog(
        string errorCode,
        RuntimeCatalogFlowText text)
    {
        var normalizedCode = NormalizeStatusCode(errorCode, "runtime_data_failed");
        return BuildWarning(
            normalizedCode,
            text.RuntimeCatalogFailedTitle,
            text.RuntimeCatalogFailedSummary,
            text.RuntimeCatalogFailedBullet1,
            text.RuntimeCatalogFailedBullet2) with
        {
            DisplayErrorCode = ResolveDisplayErrorCode(normalizedCode)
        };
    }

    public AppDialogRequest BuildUnsupportedGpuDialog(RuntimeCatalogFlowText text)
    {
        return new AppDialogRequest
        {
            Kind = AppDialogKind.Info,
            Severity = DialogSeverity.Warning,
            Title = text.RuntimeCatalogUnsupportedGpuTitle,
            Summary = text.RuntimeCatalogUnsupportedGpuSummary,
            PrimaryButtonText = text.DialogButtonOk,
            PrimaryResult = AppDialogResult.Ok
        };
    }

    public AppDialogRequest BuildAuthV2GpuSelectionRequiredDialog(
        RuntimeCatalogFlowText text,
        IReadOnlyList<string> candidateLabels)
    {
        return BuildAuthV2BusinessStatusWarning(
            "gpu_selection_required",
            text.RuntimeCatalogGpuSelectionRequiredTitle,
            text.RuntimeCatalogGpuSelectionRequiredSummary,
            text,
            candidateLabels is { Count: > 0 }
                ? candidateLabels
                : [text.RuntimeCatalogGpuSelectionRequiredBullet]);
    }

    public AppDialogRequest BuildAuthV2InvalidSelectedGpuDialog(
        RuntimeCatalogFlowText text,
        IReadOnlyList<string> candidateLabels)
    {
        return BuildAuthV2BusinessStatusWarning(
            "invalid_selected_gpu",
            text.RuntimeCatalogInvalidSelectedGpuTitle,
            text.RuntimeCatalogInvalidSelectedGpuSummary,
            text,
            candidateLabels is { Count: > 0 }
                ? candidateLabels
                : [text.RuntimeCatalogInvalidSelectedGpuBullet]);
    }

    public AppDialogRequest BuildAuthV2MultiGpuUnsupportedDialog(RuntimeCatalogFlowText text)
    {
        return new AppDialogRequest
        {
            Kind = AppDialogKind.Warning,
            Severity = DialogSeverity.Warning,
            Title = text.RuntimeCatalogMultiGpuUnsupportedTitle,
            Summary = text.RuntimeCatalogMultiGpuUnsupportedSummary,
            ErrorCode = "multi_gpu_unsupported",
            PrimaryButtonText = text.DialogButtonOk,
            PrimaryResult = AppDialogResult.Ok
        };
    }

    public AppDialogRequest BuildGpuDetectionFailedDialog(
        RuntimeCatalogFlowText text,
        bool retryFailed = false)
    {
        return new AppDialogRequest
        {
            Kind = AppDialogKind.Warning,
            Severity = DialogSeverity.Warning,
            Title = text.RuntimeCatalogGpuDetectionFailedTitle,
            Summary = retryFailed
                ? text.RuntimeCatalogGpuDetectionRetryFailedSummary
                : text.RuntimeCatalogGpuDetectionFailedSummary,
            ErrorCode = "gpu_detection_failed",
            PrimaryButtonText = text.DialogButtonRetryDetection,
            SecondaryButtonText = text.DialogButtonCancel,
            PrimaryResult = AppDialogResult.Retry,
            SecondaryResult = AppDialogResult.Close,
            CanClose = true,
            CloseOnOverlayClick = false
        };
    }

    public AppDialogRequest BuildEmptyCatalogDialog(RuntimeCatalogFlowText text)
    {
        return BuildWarning(
            "empty_catalog",
            text.RuntimeCatalogEmptyTitle,
            text.RuntimeCatalogEmptySummary,
            text.RuntimeCatalogEmptyBullet1,
            text.RuntimeCatalogEmptyBullet2);
    }

    public AppDialogRequest BuildPipelineMissingDialog(RuntimeCatalogFlowText text)
    {
        return BuildWarning(
            "gpu_bundle_pipeline_missing",
            text.RuntimeCatalogPipelineMissingTitle,
            text.RuntimeCatalogPipelineMissingSummary,
            text.RuntimeCatalogPipelineMissingBullet1);
    }

    private static AppDialogRequest BuildWarning(
        string errorCode,
        string title,
        string summary,
        IReadOnlyList<string> bulletItems)
    {
        return new AppDialogRequest
        {
            Kind = AppDialogKind.Warning,
            Severity = DialogSeverity.Warning,
            Title = title,
            Summary = summary,
            BulletItems = bulletItems ?? [],
            ErrorCode = errorCode
        };
    }

    private static AppDialogRequest BuildAuthV2BusinessStatusWarning(
        string errorCode,
        string title,
        string summary,
        RuntimeCatalogFlowText text,
        IReadOnlyList<string> bulletItems)
    {
        return BuildWarning(errorCode, title, summary, bulletItems) with
        {
            PrimaryButtonText = text.DialogButtonOk,
            PrimaryResult = AppDialogResult.Ok
        };
    }

    private static AppDialogRequest BuildWarning(
        string errorCode,
        string title,
        string summary,
        params string[] bulletItems)
    {
        return BuildWarning(errorCode, title, summary, (IReadOnlyList<string>)(bulletItems ?? []));
    }

    private static string NormalizeStatusCode(string? value, string fallback)
    {
        var normalized = (value ?? "").Trim();
        return string.IsNullOrWhiteSpace(normalized) ? fallback : normalized;
    }

    private static string ResolveDisplayErrorCode(string diagnosticErrorCode)
    {
        var normalized = (diagnosticErrorCode ?? "").Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return "remote_catalog_failed";
        }

        if (string.Equals(normalized, "bootstrap_diversity_exceeded", StringComparison.OrdinalIgnoreCase))
        {
            return "authentication_temporarily_unavailable";
        }

        if (MatchesAny(
                normalized,
                "client_not_found",
                "invalid_signature",
                "invalid_client_record",
                "invalid_request"))
        {
            return "authentication_failed";
        }

        if (MatchesAny(
                normalized,
                "runtime_token_replay",
                "gpu_bundle_token_replay",
                "invalid_runtime_token",
                "invalid_gpu_bundle_token",
                "runtime_token_expired",
                "gpu_bundle_token_expired"))
        {
            return "authentication_session_expired";
        }

        if (MatchesAny(
                normalized,
                "auth_v2_kv_unavailable",
                "data_v2_kv_unavailable",
                "resolve_v2_unavailable",
                "runtime_data_unavailable",
                "gpu_bundle_unavailable"))
        {
            return "service_temporarily_unavailable";
        }

        if (MatchesAny(
                normalized,
                "resolve_v2_invalid_contract",
                "auth_v2_invalid_json",
                "data_v2_invalid_json",
                "invalid_runtime_data",
                "invalid_gpu_bundle",
                "runtime_data_parse_failed",
                "gpu_bundle_parse_failed"))
        {
            return "service_response_invalid";
        }

        return "remote_catalog_failed";
    }

    private static bool MatchesAny(string value, params string[] candidates)
    {
        return candidates.Any(candidate =>
            string.Equals(value, candidate, StringComparison.OrdinalIgnoreCase));
    }
}
