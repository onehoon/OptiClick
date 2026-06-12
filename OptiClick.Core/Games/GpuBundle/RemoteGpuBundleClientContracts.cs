namespace OptiClick.Core.Games.GpuBundle;

public sealed class RemoteGpuBundleFetchResult
{
    public bool IsSuccess { get; init; }
    public bool IsSkipped { get; init; }
    public string ErrorCode { get; init; } = "";
    public string Content { get; init; } = "";

    public static RemoteGpuBundleFetchResult Success(string content)
    {
        return new RemoteGpuBundleFetchResult
        {
            IsSuccess = true,
            Content = content ?? ""
        };
    }

    public static RemoteGpuBundleFetchResult Skipped()
    {
        return new RemoteGpuBundleFetchResult
        {
            IsSuccess = false,
            IsSkipped = true
        };
    }

    public static RemoteGpuBundleFetchResult Failure(string errorCode)
    {
        return new RemoteGpuBundleFetchResult
        {
            IsSuccess = false,
            IsSkipped = false,
            ErrorCode = (errorCode ?? "").Trim()
        };
    }
}

public sealed class GpuBundleFetchRequest
{
    public string Vendor { get; init; } = "";
    public string BundleKey { get; init; } = "";
    public string GpuRaw { get; init; } = "";
    public string RequestSource { get; init; } = "";
    public string DeviceManufacturer { get; init; } = "";
    public string DeviceModel { get; init; } = "";
    public string AppVersion { get; init; } = "";
    public string ManifestVersion { get; init; } = "";
}

public sealed class GpuBundleUnsupportedReportRequest
{
    public string Vendor { get; init; } = "";
    public string GpuRaw { get; init; } = "";
    public string RequestSource { get; init; } = "app";
    public string DeviceManufacturer { get; init; } = "";
    public string DeviceModel { get; init; } = "";
    public string AppVersion { get; init; } = "";
    public string ManifestVersion { get; init; } = "";
    public string Reason { get; init; } = "manifest_no_match";
}

public interface IGpuBundleRequestUriBuilder
{
    Uri? Build(string endpoint, GpuBundleFetchRequest request);
}

public interface IRemoteGpuBundleClient
{
    Task<RemoteGpuBundleFetchResult> FetchAsync(
        string endpoint,
        GpuBundleFetchRequest request,
        CancellationToken cancellationToken = default);

    Task<RemoteGpuBundleFetchResult> ReportUnsupportedAsync(
        string endpoint,
        GpuBundleUnsupportedReportRequest request,
        CancellationToken cancellationToken = default);
}
