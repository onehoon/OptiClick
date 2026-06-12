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
            text.RuntimeCatalogUnexpectedErrorBullet2,
            "Error code: remote_catalog_unexpected_error");
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
            text.RuntimeCatalogSkippedBullet2,
            $"Error code: {normalizedCode}");
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
            text.RuntimeCatalogFailedBullet2,
            $"Error code: {normalizedCode}");
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
}
