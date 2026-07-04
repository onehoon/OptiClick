using System.Text.Json.Serialization;

namespace OptiClick.Wpf.Shell.RemoteV2;

public sealed record AuthV2SessionStartRequest
{
    [JsonPropertyName("protocol_version")]
    public int ProtocolVersion { get; init; } = 2;

    [JsonPropertyName("app_version")]
    public string AppVersion { get; init; } = "";

    [JsonPropertyName("app_start_id")]
    public string AppStartId { get; init; } = "";

    [JsonPropertyName("request_source")]
    public string RequestSource { get; init; } = "app";

    [JsonPropertyName("device")]
    public AuthV2DevicePayload Device { get; init; } = new();

    [JsonPropertyName("gpus")]
    public IReadOnlyList<AuthV2GpuPayload> Gpus { get; init; } = [];

    [JsonPropertyName("selected_gpu")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public AuthV2SelectedGpuPayload? SelectedGpu { get; init; }

    [JsonPropertyName("timestamp")]
    public long Timestamp { get; init; }

    [JsonPropertyName("nonce")]
    public string Nonce { get; init; } = "";
}

public sealed record AuthV2DevicePayload
{
    [JsonPropertyName("manufacturer")]
    public string Manufacturer { get; init; } = "";

    [JsonPropertyName("model")]
    public string Model { get; init; } = "";
}

public sealed record AuthV2GpuPayload
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = "";

    [JsonPropertyName("vendor_hint")]
    public string VendorHint { get; init; } = "";

    [JsonPropertyName("is_primary")]
    public bool IsPrimary { get; init; }

    [JsonPropertyName("source")]
    public string Source { get; init; } = "";
}

public sealed record AuthV2SelectedGpuPayload
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = "";

    [JsonPropertyName("vendor_hint")]
    public string VendorHint { get; init; } = "";
}

public sealed record AuthV2SessionStartResult
{
    public bool IsHttpSuccess { get; init; }
    public string ErrorCode { get; init; } = "";
    public string Status { get; init; } = "";
    public bool SessionReady { get; init; }
    public string RuntimeToken { get; init; } = "";
    public int RuntimeTokenExpiresIn { get; init; }
    public string ClientId { get; init; } = "";
    public string ClientSecret { get; init; } = "";
    public bool CredentialIssued { get; init; }
    public AuthV2ResolvedGpu ResolvedGpu { get; init; } = new();
    public IReadOnlyList<AuthV2ResolvedGpu> Candidates { get; init; } = [];

    public bool IsResolved =>
        IsHttpSuccess
        && SessionReady
        && string.Equals(Status, "resolved", StringComparison.OrdinalIgnoreCase)
        && !string.IsNullOrWhiteSpace(RuntimeToken)
        && ResolvedGpu.IsUsable;

    public bool IsUnsupported =>
        string.Equals(Status, "unsupported", StringComparison.OrdinalIgnoreCase)
        || string.Equals(ErrorCode, "gpu_unsupported", StringComparison.OrdinalIgnoreCase);

    public bool RequiresGpuSelection =>
        string.Equals(Status, "gpu_selection_required", StringComparison.OrdinalIgnoreCase);

    public bool IsInvalidSelectedGpu =>
        string.Equals(Status, "invalid_selected_gpu", StringComparison.OrdinalIgnoreCase);

    public bool IsMultiGpuUnsupported =>
        string.Equals(Status, "multi_gpu_unsupported", StringComparison.OrdinalIgnoreCase);

    public bool IsBusinessStatus =>
        IsHttpSuccess
        && !SessionReady
        && (IsUnsupported || RequiresGpuSelection || IsInvalidSelectedGpu || IsMultiGpuUnsupported);
}

public sealed record AuthV2ResolvedGpu
{
    public string CandidateId { get; init; } = "";
    public int SourceIndex { get; init; }
    public string Name { get; init; } = "";
    public string Vendor { get; init; } = "";
    public string BundleKey { get; init; } = "";
    public string GpuGroup { get; init; } = "";
    public string GpuRaw { get; init; } = "";
    public string GpuRawNormalized { get; init; } = "";
    public string ManifestVersion { get; init; } = "";

    public bool IsUsable =>
        !string.IsNullOrWhiteSpace(Vendor)
        && !string.IsNullOrWhiteSpace(BundleKey)
        && !string.IsNullOrWhiteSpace(GpuGroup);
}

public sealed record DataV2RuntimeDataResult
{
    public bool IsSuccess { get; init; }
    public string ErrorCode { get; init; } = "";
    public string RuntimeDataJson { get; init; } = "";
    public string GpuBundleToken { get; init; } = "";
    public int GpuBundleTokenExpiresIn { get; init; }
}

public sealed record DataV2GpuBundleResult
{
    public bool IsSuccess { get; init; }
    public string ErrorCode { get; init; } = "";
    public string GpuBundleJson { get; init; } = "";
}
