namespace OptiClick.Core.Games.GpuBundle;

public sealed class RemoteGpuBundleManifest
{
    public IReadOnlyList<RemoteGpuBundleManifestRule> Rules { get; init; } = [];
    public RemoteGpuBundleManifestFallback? Fallback { get; init; }
    public string ManifestVersion { get; init; } = "";
}

public sealed class RemoteGpuBundleManifestRule
{
    public bool Enabled { get; init; } = true;
    public string Vendor { get; init; } = "";
    public string MatchMode { get; init; } = "";
    public string MatchValue { get; init; } = "";
    public string BundleKey { get; init; } = "";
    public string GpuGroup { get; init; } = "";
    public int Priority { get; init; } = 100;
    public int SourceIndex { get; init; }
    public Fsr4ManifestPolicy Fsr4 { get; init; } = Fsr4ManifestPolicy.Disabled;
}

public sealed record Fsr4ManifestPolicy
{
    public static readonly Fsr4ManifestPolicy Disabled = new();

    public bool Enabled { get; init; }
    public string Variant { get; init; } = "";
}

public sealed class RemoteGpuBundleManifestFallback
{
    public bool Enabled { get; init; } = true;
    public string BundleKey { get; init; } = "";
    public string GpuGroup { get; init; } = "";
}

public sealed class RemoteGpuBundleManifestParseResult
{
    public bool IsSuccess { get; init; }
    public string ErrorCode { get; init; } = "";
    public RemoteGpuBundleManifest Manifest { get; init; } = new();

    public static RemoteGpuBundleManifestParseResult Success(RemoteGpuBundleManifest manifest)
    {
        return new RemoteGpuBundleManifestParseResult
        {
            IsSuccess = true,
            Manifest = manifest ?? new RemoteGpuBundleManifest()
        };
    }

    public static RemoteGpuBundleManifestParseResult Failure(string errorCode)
    {
        return new RemoteGpuBundleManifestParseResult
        {
            IsSuccess = false,
            ErrorCode = (errorCode ?? "").Trim(),
            Manifest = new RemoteGpuBundleManifest()
        };
    }
}

public interface IRemoteGpuBundleManifestParser
{
    RemoteGpuBundleManifestParseResult Parse(string json);
}
