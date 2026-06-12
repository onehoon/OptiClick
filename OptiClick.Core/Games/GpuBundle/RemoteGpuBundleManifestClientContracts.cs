namespace OptiClick.Core.Games.GpuBundle;

public sealed class RemoteGpuBundleManifestFetchResult
{
    public bool IsSuccess { get; init; }
    public bool IsSkipped { get; init; }
    public string ErrorCode { get; init; } = "";
    public RemoteGpuBundleManifest Manifest { get; init; } = new();

    public static RemoteGpuBundleManifestFetchResult Success(RemoteGpuBundleManifest manifest)
    {
        return new RemoteGpuBundleManifestFetchResult
        {
            IsSuccess = true,
            Manifest = manifest ?? new RemoteGpuBundleManifest()
        };
    }

    public static RemoteGpuBundleManifestFetchResult Skipped()
    {
        return new RemoteGpuBundleManifestFetchResult
        {
            IsSuccess = false,
            IsSkipped = true
        };
    }

    public static RemoteGpuBundleManifestFetchResult Failure(string errorCode)
    {
        return new RemoteGpuBundleManifestFetchResult
        {
            IsSuccess = false,
            IsSkipped = false,
            ErrorCode = (errorCode ?? "").Trim()
        };
    }
}

public sealed class GpuBundleManifestFetchRequest
{
    public string Vendor { get; init; } = "";
    public string GpuRaw { get; init; } = "";
    public string DeviceManufacturer { get; init; } = "";
    public string DeviceModel { get; init; } = "";
    public string RequestSource { get; init; } = "app";
    public string AppVersion { get; init; } = "";
}

public interface IGpuBundleManifestRequestUriBuilder
{
    Uri? Build(string endpoint, GpuBundleManifestFetchRequest request);
}

public interface IRemoteGpuBundleManifestClient
{
    Task<RemoteGpuBundleManifestFetchResult> FetchAsync(
        string endpoint,
        GpuBundleManifestFetchRequest request,
        CancellationToken cancellationToken = default);
}
