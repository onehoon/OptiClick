using OptiClick.Wpf.Localization;
using OptiClick.Wpf.Models;

namespace OptiClick.Wpf.Shell.Runtime;

public sealed class RuntimeCatalogDialogPresenter
{
    public AppDialogRequest BuildUnexpectedErrorDialog(
        AppStrings strings,
        RemoteServiceHealthSnapshot? serviceHealth)
    {
        return BuildWarning(
            "remote_catalog_unexpected_error",
            strings.RuntimeCatalogUnexpectedErrorTitle,
            strings.RuntimeCatalogUnexpectedErrorSummary,
            BuildBullets(
                strings.RuntimeCatalogUnexpectedErrorBullet1,
                strings.RuntimeCatalogUnexpectedErrorBullet2,
                strings,
                serviceHealth));
    }

    public AppDialogRequest BuildUnexpectedErrorDialog(AppStrings strings)
    {
        return BuildUnexpectedErrorDialog(strings, serviceHealth: null);
    }

    public AppDialogRequest BuildSkippedDialog(
        string errorCode,
        AppStrings strings,
        RemoteServiceHealthSnapshot? serviceHealth)
    {
        var normalizedCode = NormalizeStatusCode(errorCode, "runtime_data_skipped");
        return BuildWarning(
            normalizedCode,
            strings.RuntimeCatalogSkippedTitle,
            strings.RuntimeCatalogSkippedSummary,
            BuildBullets(
                strings.RuntimeCatalogSkippedBullet1,
                strings.RuntimeCatalogSkippedBullet2,
                $"Error code: {normalizedCode}",
                strings,
                serviceHealth));
    }

    public AppDialogRequest BuildSkippedDialog(string errorCode, AppStrings strings)
    {
        return BuildSkippedDialog(errorCode, strings, serviceHealth: null);
    }

    public AppDialogRequest BuildFailedDialog(
        string errorCode,
        AppStrings strings,
        RemoteServiceHealthSnapshot? serviceHealth)
    {
        var normalizedCode = NormalizeStatusCode(errorCode, "runtime_data_failed");
        return BuildWarning(
            normalizedCode,
            strings.RuntimeCatalogFailedTitle,
            strings.RuntimeCatalogFailedSummary,
            BuildBullets(
                strings.RuntimeCatalogFailedBullet1,
                strings.RuntimeCatalogFailedBullet2,
                $"Error code: {normalizedCode}",
                strings,
                serviceHealth));
    }

    public AppDialogRequest BuildFailedDialog(string errorCode, AppStrings strings)
    {
        return BuildFailedDialog(errorCode, strings, serviceHealth: null);
    }

    public AppDialogRequest BuildEmptyCatalogDialog(AppStrings strings)
    {
        return BuildWarning(
            "empty_catalog",
            strings.RuntimeCatalogEmptyTitle,
            strings.RuntimeCatalogEmptySummary,
            strings.RuntimeCatalogEmptyBullet1,
            strings.RuntimeCatalogEmptyBullet2);
    }

    public AppDialogRequest BuildPipelineMissingDialog(AppStrings strings)
    {
        return BuildWarning(
            "gpu_bundle_pipeline_missing",
            strings.RuntimeCatalogPipelineMissingTitle,
            strings.RuntimeCatalogPipelineMissingSummary,
            strings.RuntimeCatalogPipelineMissingBullet1);
    }

    private static IReadOnlyList<string> BuildBullets(
        string primary,
        string secondary,
        AppStrings strings,
        RemoteServiceHealthSnapshot? serviceHealth)
    {
        return BuildBullets(primary, secondary, extra: "", strings, serviceHealth);
    }

    private static IReadOnlyList<string> BuildBullets(
        string primary,
        string secondary,
        string extra,
        AppStrings strings,
        RemoteServiceHealthSnapshot? serviceHealth)
    {
        var bullets = new List<string>
        {
            primary,
            secondary
        };

        if (!string.IsNullOrWhiteSpace(extra))
        {
            bullets.Add(extra);
        }

        if (serviceHealth is not null)
        {
            bullets.Add(string.Format(
                strings.RuntimeCatalogCloudflareStatusFormat,
                FormatServiceStatus(serviceHealth.Cloudflare, strings)));
            bullets.Add(string.Format(
                strings.RuntimeCatalogGitHubStatusFormat,
                FormatServiceStatus(serviceHealth.GitHub, strings)));
        }

        return bullets;
    }

    private static string FormatServiceStatus(RemoteServiceHealthStatus status, AppStrings strings)
    {
        var indicator = NormalizeStatusCode(status?.Indicator, strings.StatusUnknown);
        var description = (status?.Description ?? "").Trim();
        if (string.IsNullOrWhiteSpace(description))
        {
            return indicator;
        }

        return $"{indicator} ({description})";
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
