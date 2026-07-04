namespace OptiClick.Core.Runtime;

public sealed record RemoteDataOptions
{
    public RemoteDataProtocol Protocol { get; init; } = RemoteDataProtocol.V2;
    public string RuntimeDataUrl { get; init; } = "";
    public string GpuBundleManifestUrl { get; init; } = "";
    public string GpuBundleUrl { get; init; } = "";
    public string AuthV2BaseUrl { get; init; } = "";
    public string DataV2BaseUrl { get; init; } = "";
    public string ManifestEndpoint { get; init; } = "";
    public string Channel { get; init; } = "stable";
    public bool AllowMockGpuManifestFallback { get; init; }

    public bool HasRuntimeDataUrl => !string.IsNullOrWhiteSpace(RuntimeDataUrl);
    public bool HasManifestEndpoint => !string.IsNullOrWhiteSpace(ManifestEndpoint);
    public bool IsV2 => Protocol == RemoteDataProtocol.V2;
    public bool HasAuthV2BaseUrl => !string.IsNullOrWhiteSpace(AuthV2BaseUrl);
    public bool HasDataV2BaseUrl => !string.IsNullOrWhiteSpace(DataV2BaseUrl);

    public string GetEffectiveRuntimeDataUrl()
    {
        if (!string.IsNullOrWhiteSpace(RuntimeDataUrl))
        {
            return RuntimeDataUrl.Trim();
        }

        return (ManifestEndpoint ?? "").Trim();
    }
}
