namespace OptiClick.Wpf.Install.Archives;

internal static class ArchivePreparationStageStatuses
{
    public const string Ok = "ok";
    public const string Cached = "cached";
    public const string Skipped = "skipped";
    public const string Pending = "pending";
    public const string NotConfigured = "not_configured";

    private const string SourceCache = "cache";
    private const string SourceDownload = "download";
    private const string SourceFallback = "fallback";
    private const string SourceMissingMetadata = "missing_metadata";
    private const string SourceUnknown = "unknown";
    private const string FolderMissing = "missing";
    private const string GithubDigestUnavailable = "github_digest_unavailable";

    public static ArchivePreparationStageStatus CachedStatus()
    {
        return Create(SourceCache, Cached, Cached, Cached, Ok);
    }

    public static ArchivePreparationStageStatus MissingMetadata()
    {
        return Create(SourceMissingMetadata, Skipped, Skipped, FolderMissing, Skipped);
    }

    public static ArchivePreparationStageStatus DownloadFailed(string errorCode)
    {
        return Create(SourceDownload, Failed(errorCode), Skipped, Skipped, Skipped);
    }

    public static ArchivePreparationStageStatus DownloadSucceeded(ArchiveDownloadResult? download)
    {
        return Create(SourceDownload, Ok, ResolveShaStatus(download), Pending, Pending);
    }

    public static ArchivePreparationStageStatus WithFolderFailure(
        ArchivePreparationStageStatus status,
        string errorCode)
    {
        var safeStatus = status ?? ArchivePreparationStageStatus.Unknown;
        return safeStatus with { Folder = Failed(errorCode), Json = Skipped };
    }

    public static ArchivePreparationStageStatus WithFolderOk(ArchivePreparationStageStatus status)
    {
        var safeStatus = status ?? ArchivePreparationStageStatus.Unknown;
        return safeStatus with { Folder = Ok };
    }

    public static ArchivePreparationStageStatus WithJsonOk(ArchivePreparationStageStatus status)
    {
        var safeStatus = status ?? ArchivePreparationStageStatus.Unknown;
        return safeStatus with { Json = Ok };
    }

    public static ArchivePreparationStageStatus Fallback(ArchivePreparationStageStatus status)
    {
        var safeStatus = status ?? ArchivePreparationStageStatus.Unknown;
        return safeStatus with
        {
            Source = SourceFallback,
            Folder = string.IsNullOrWhiteSpace(safeStatus.Folder)
                     || string.Equals(safeStatus.Folder, Skipped, StringComparison.OrdinalIgnoreCase)
                ? SourceFallback
                : safeStatus.Folder,
            Json = string.IsNullOrWhiteSpace(safeStatus.Json) ? Skipped : safeStatus.Json
        };
    }

    public static ArchivePreparationStageStatus EnsureFailure(
        ArchivePreparationStageStatus status,
        string errorCode)
    {
        var safeStatus = status ?? ArchivePreparationStageStatus.Unknown;
        if (string.IsNullOrWhiteSpace(safeStatus.Source))
        {
            return Create(SourceUnknown, Skipped, Skipped, Failed(errorCode), Skipped);
        }

        if (string.Equals(safeStatus.Json, Pending, StringComparison.OrdinalIgnoreCase))
        {
            return safeStatus with { Json = Failed(errorCode) };
        }

        if (string.Equals(safeStatus.Folder, Pending, StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(safeStatus.Folder))
        {
            return safeStatus with
            {
                Folder = Failed(errorCode),
                Json = string.IsNullOrWhiteSpace(safeStatus.Json) ? Skipped : safeStatus.Json
            };
        }

        return safeStatus;
    }

    public static ArchivePreparationStageStatus Create(
        string source,
        string download,
        string sha,
        string folder,
        string json)
    {
        return new ArchivePreparationStageStatus
        {
            Source = source,
            Download = download,
            Sha = sha,
            Folder = folder,
            Json = json
        };
    }

    public static string Failed(string code)
    {
        return string.IsNullOrWhiteSpace(code) ? "failed" : $"failed:{code.Trim()}";
    }

    public static string ResolveShaStatus(ArchiveDownloadResult? download)
    {
        if (download is null || string.IsNullOrWhiteSpace(download.VerificationSource))
        {
            return Ok;
        }

        return string.Equals(download.VerificationSource, NotConfigured, StringComparison.OrdinalIgnoreCase)
               || string.Equals(download.VerificationSource, GithubDigestUnavailable, StringComparison.OrdinalIgnoreCase)
            ? NotConfigured
            : Ok;
    }
}
