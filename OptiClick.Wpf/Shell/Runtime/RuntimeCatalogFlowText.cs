using OptiClick.Wpf.Localization;

namespace OptiClick.Wpf.Shell.Runtime;

public sealed record RuntimeCatalogFlowText
{
    public required string RuntimeRemoteCatalogFailed { get; init; }
    public required string RuntimeRemoteCatalogSkipped { get; init; }
    public required string RuntimeRemoteCatalogLoadedScanHint { get; init; }
    public required string RuntimeCatalogUnexpectedErrorTitle { get; init; }
    public required string RuntimeCatalogUnexpectedErrorSummary { get; init; }
    public required string RuntimeCatalogUnexpectedErrorBullet1 { get; init; }
    public required string RuntimeCatalogUnexpectedErrorBullet2 { get; init; }
    public required string RuntimeCatalogSkippedTitle { get; init; }
    public required string RuntimeCatalogSkippedSummary { get; init; }
    public required string RuntimeCatalogSkippedBullet1 { get; init; }
    public required string RuntimeCatalogSkippedBullet2 { get; init; }
    public required string RuntimeCatalogFailedTitle { get; init; }
    public required string RuntimeCatalogFailedSummary { get; init; }
    public required string RuntimeCatalogFailedBullet1 { get; init; }
    public required string RuntimeCatalogFailedBullet2 { get; init; }
    public required string RuntimeCatalogUnsupportedGpuTitle { get; init; }
    public required string RuntimeCatalogUnsupportedGpuSummary { get; init; }
    public required string RuntimeCatalogEmptyTitle { get; init; }
    public required string RuntimeCatalogEmptySummary { get; init; }
    public required string RuntimeCatalogEmptyBullet1 { get; init; }
    public required string RuntimeCatalogEmptyBullet2 { get; init; }
    public required string RuntimeCatalogPipelineMissingTitle { get; init; }
    public required string RuntimeCatalogPipelineMissingSummary { get; init; }
    public required string RuntimeCatalogPipelineMissingBullet1 { get; init; }
    public required string DialogButtonOk { get; init; }
    public required string StatusUnknown { get; init; }

    public static RuntimeCatalogFlowText FromAppStrings(AppStrings strings)
    {
        ArgumentNullException.ThrowIfNull(strings);
        return new RuntimeCatalogFlowText
        {
            RuntimeRemoteCatalogFailed = strings.RuntimeRemoteCatalogFailed,
            RuntimeRemoteCatalogSkipped = strings.RuntimeRemoteCatalogSkipped,
            RuntimeRemoteCatalogLoadedScanHint = strings.RuntimeRemoteCatalogLoadedScanHint,
            RuntimeCatalogUnexpectedErrorTitle = strings.RuntimeCatalogUnexpectedErrorTitle,
            RuntimeCatalogUnexpectedErrorSummary = strings.RuntimeCatalogUnexpectedErrorSummary,
            RuntimeCatalogUnexpectedErrorBullet1 = strings.RuntimeCatalogUnexpectedErrorBullet1,
            RuntimeCatalogUnexpectedErrorBullet2 = strings.RuntimeCatalogUnexpectedErrorBullet2,
            RuntimeCatalogSkippedTitle = strings.RuntimeCatalogSkippedTitle,
            RuntimeCatalogSkippedSummary = strings.RuntimeCatalogSkippedSummary,
            RuntimeCatalogSkippedBullet1 = strings.RuntimeCatalogSkippedBullet1,
            RuntimeCatalogSkippedBullet2 = strings.RuntimeCatalogSkippedBullet2,
            RuntimeCatalogFailedTitle = strings.RuntimeCatalogFailedTitle,
            RuntimeCatalogFailedSummary = strings.RuntimeCatalogFailedSummary,
            RuntimeCatalogFailedBullet1 = strings.RuntimeCatalogFailedBullet1,
            RuntimeCatalogFailedBullet2 = strings.RuntimeCatalogFailedBullet2,
            RuntimeCatalogUnsupportedGpuTitle = strings.RuntimeCatalogUnsupportedGpuTitle,
            RuntimeCatalogUnsupportedGpuSummary = strings.RuntimeCatalogUnsupportedGpuSummary,
            RuntimeCatalogEmptyTitle = strings.RuntimeCatalogEmptyTitle,
            RuntimeCatalogEmptySummary = strings.RuntimeCatalogEmptySummary,
            RuntimeCatalogEmptyBullet1 = strings.RuntimeCatalogEmptyBullet1,
            RuntimeCatalogEmptyBullet2 = strings.RuntimeCatalogEmptyBullet2,
            RuntimeCatalogPipelineMissingTitle = strings.RuntimeCatalogPipelineMissingTitle,
            RuntimeCatalogPipelineMissingSummary = strings.RuntimeCatalogPipelineMissingSummary,
            RuntimeCatalogPipelineMissingBullet1 = strings.RuntimeCatalogPipelineMissingBullet1,
            DialogButtonOk = strings.DialogButtonOk,
            StatusUnknown = strings.StatusUnknown
        };
    }
}
