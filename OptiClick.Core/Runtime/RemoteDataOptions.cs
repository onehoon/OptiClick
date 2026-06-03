namespace OptiClick.Core.Runtime;

public sealed record RemoteDataOptions
{
    public string RuntimeDataUrl { get; init; } = "";
    public string GpuBundleManifestUrl { get; init; } = "";
    public string GpuBundleUrl { get; init; } = "";
    public string ManifestEndpoint { get; init; } = "";
    public string Channel { get; init; } = "stable";
    public bool AllowMockGpuManifestFallback { get; init; }

    public bool HasRuntimeDataUrl => !string.IsNullOrWhiteSpace(RuntimeDataUrl);
    public bool HasManifestEndpoint => !string.IsNullOrWhiteSpace(ManifestEndpoint);

    public string GetEffectiveRuntimeDataUrl()
    {
        if (!string.IsNullOrWhiteSpace(RuntimeDataUrl))
        {
            return RuntimeDataUrl.Trim();
        }

        return (ManifestEndpoint ?? "").Trim();
    }
}
