using System.Text.Json;
using OptiClick.Core.Runtime;

namespace OptiClick.Wpf.Configuration;

public sealed class RemoteDataOptionsLoader
{
    public const string RuntimeDataUrlEnvName = "OPTICLICK_RUNTIME_DATA_URL";
    public const string GpuBundleManifestUrlEnvName = "OPTICLICK_GPU_BUNDLE_MANIFEST_URL";
    public const string GpuBundleUrlEnvName = "OPTICLICK_GPU_BUNDLE_URL";
    public const string ProtocolEnvName = "OPTICLICK_REMOTE_DATA_PROTOCOL";
    public const string AuthV2BaseUrlEnvName = "OPTICLICK_AUTH_V2_BASE_URL";
    public const string DataV2BaseUrlEnvName = "OPTICLICK_DATA_V2_BASE_URL";
    public const string ManifestEndpointEnvName = "OPTICLICK_REMOTE_MANIFEST_ENDPOINT";
    public const string ChannelEnvName = "OPTICLICK_REMOTE_CHANNEL";

    private const string DefaultChannel = "stable";
    private readonly Func<string, string?> _readEnvironmentVariable;
    private readonly Func<JsonDocument> _loadAppSettingsDocument;

    public RemoteDataOptionsLoader(
        Func<string, string?>? readEnvironmentVariable = null,
        Func<JsonDocument>? loadAppSettingsDocument = null)
    {
        _readEnvironmentVariable = readEnvironmentVariable ?? Environment.GetEnvironmentVariable;
        _loadAppSettingsDocument = loadAppSettingsDocument ?? EmbeddedAppSettingsReader.Load;
    }

    public RemoteDataOptions Load()
    {
        var options = new RemoteDataOptions
        {
            Protocol = RemoteDataProtocol.V2,
            RuntimeDataUrl = "",
            GpuBundleManifestUrl = "",
            GpuBundleUrl = "",
            AuthV2BaseUrl = "",
            DataV2BaseUrl = "",
            ManifestEndpoint = "",
            Channel = DefaultChannel,
            AllowMockGpuManifestFallback = false
        };

        options = ApplyEmbeddedAppSettings(options);
        options = ApplyEnvironmentOverrides(options);
        return Normalize(options);
    }

    private RemoteDataOptions ApplyEmbeddedAppSettings(RemoteDataOptions current)
    {
        using var document = _loadAppSettingsDocument();
        if (!document.RootElement.TryGetProperty("RemoteData", out var remoteData)
            || remoteData.ValueKind != JsonValueKind.Object)
        {
            return current;
        }

        var manifestEndpoint = current.ManifestEndpoint;
        var runtimeDataUrl = current.RuntimeDataUrl;
        var gpuBundleManifestUrl = current.GpuBundleManifestUrl;
        var gpuBundleUrl = current.GpuBundleUrl;
        var protocol = current.Protocol;
        var authV2BaseUrl = current.AuthV2BaseUrl;
        var dataV2BaseUrl = current.DataV2BaseUrl;
        var channel = current.Channel;

        if (remoteData.TryGetProperty("Protocol", out var protocolElement)
            && protocolElement.ValueKind == JsonValueKind.String)
        {
            protocol = ParseProtocol(protocolElement.GetString(), protocol);
        }

        if (remoteData.TryGetProperty("RemoteDataProtocol", out var legacyProtocolElement)
            && legacyProtocolElement.ValueKind == JsonValueKind.String)
        {
            protocol = ParseProtocol(legacyProtocolElement.GetString(), protocol);
        }

        if (remoteData.TryGetProperty("RuntimeDataUrl", out var runtimeDataElement)
            && runtimeDataElement.ValueKind == JsonValueKind.String)
        {
            runtimeDataUrl = runtimeDataElement.GetString() ?? "";
        }

        if (remoteData.TryGetProperty("GpuBundleManifestUrl", out var gpuBundleManifestElement)
            && gpuBundleManifestElement.ValueKind == JsonValueKind.String)
        {
            gpuBundleManifestUrl = gpuBundleManifestElement.GetString() ?? "";
        }

        if (remoteData.TryGetProperty("GpuBundleUrl", out var gpuBundleElement)
            && gpuBundleElement.ValueKind == JsonValueKind.String)
        {
            gpuBundleUrl = gpuBundleElement.GetString() ?? "";
        }

        if (remoteData.TryGetProperty("AuthV2BaseUrl", out var authV2Element)
            && authV2Element.ValueKind == JsonValueKind.String)
        {
            authV2BaseUrl = authV2Element.GetString() ?? "";
        }

        if (remoteData.TryGetProperty("DataV2BaseUrl", out var dataV2Element)
            && dataV2Element.ValueKind == JsonValueKind.String)
        {
            dataV2BaseUrl = dataV2Element.GetString() ?? "";
        }

        if (remoteData.TryGetProperty("ManifestEndpoint", out var endpointElement)
            && endpointElement.ValueKind == JsonValueKind.String)
        {
            manifestEndpoint = endpointElement.GetString() ?? "";
        }

        if (remoteData.TryGetProperty("Channel", out var channelElement)
            && channelElement.ValueKind == JsonValueKind.String)
        {
            channel = channelElement.GetString() ?? "";
        }

        return new RemoteDataOptions
        {
            Protocol = protocol,
            RuntimeDataUrl = runtimeDataUrl,
            GpuBundleManifestUrl = gpuBundleManifestUrl,
            GpuBundleUrl = gpuBundleUrl,
            AuthV2BaseUrl = authV2BaseUrl,
            DataV2BaseUrl = dataV2BaseUrl,
            ManifestEndpoint = manifestEndpoint,
            Channel = channel,
            AllowMockGpuManifestFallback = current.AllowMockGpuManifestFallback
        };
    }

    private RemoteDataOptions ApplyEnvironmentOverrides(RemoteDataOptions current)
    {
        var runtimeDataUrl = current.RuntimeDataUrl;
        var gpuBundleManifestUrl = current.GpuBundleManifestUrl;
        var gpuBundleUrl = current.GpuBundleUrl;
        var manifestEndpoint = current.ManifestEndpoint;
        var protocol = current.Protocol;
        var authV2BaseUrl = current.AuthV2BaseUrl;
        var dataV2BaseUrl = current.DataV2BaseUrl;
        var channel = current.Channel;

        var protocolEnv = _readEnvironmentVariable(ProtocolEnvName);
        if (!string.IsNullOrWhiteSpace(protocolEnv))
        {
            protocol = ParseProtocol(protocolEnv, protocol);
        }

        var runtimeDataEnv = _readEnvironmentVariable(RuntimeDataUrlEnvName);
        if (!string.IsNullOrWhiteSpace(runtimeDataEnv))
        {
            runtimeDataUrl = runtimeDataEnv;
        }

        var gpuBundleManifestEnv = _readEnvironmentVariable(GpuBundleManifestUrlEnvName);
        if (!string.IsNullOrWhiteSpace(gpuBundleManifestEnv))
        {
            gpuBundleManifestUrl = gpuBundleManifestEnv;
        }

        var gpuBundleEnv = _readEnvironmentVariable(GpuBundleUrlEnvName);
        if (!string.IsNullOrWhiteSpace(gpuBundleEnv))
        {
            gpuBundleUrl = gpuBundleEnv;
        }

        var authV2Env = _readEnvironmentVariable(AuthV2BaseUrlEnvName);
        if (!string.IsNullOrWhiteSpace(authV2Env))
        {
            authV2BaseUrl = authV2Env;
        }

        var dataV2Env = _readEnvironmentVariable(DataV2BaseUrlEnvName);
        if (!string.IsNullOrWhiteSpace(dataV2Env))
        {
            dataV2BaseUrl = dataV2Env;
        }

        var endpointEnv = _readEnvironmentVariable(ManifestEndpointEnvName);
        if (!string.IsNullOrWhiteSpace(endpointEnv) && string.IsNullOrWhiteSpace(runtimeDataUrl))
        {
            manifestEndpoint = endpointEnv;
        }

        var channelEnv = _readEnvironmentVariable(ChannelEnvName);
        if (!string.IsNullOrWhiteSpace(channelEnv))
        {
            channel = channelEnv;
        }

        return new RemoteDataOptions
        {
            Protocol = protocol,
            RuntimeDataUrl = runtimeDataUrl,
            GpuBundleManifestUrl = gpuBundleManifestUrl,
            GpuBundleUrl = gpuBundleUrl,
            AuthV2BaseUrl = authV2BaseUrl,
            DataV2BaseUrl = dataV2BaseUrl,
            ManifestEndpoint = manifestEndpoint,
            Channel = channel,
            AllowMockGpuManifestFallback = current.AllowMockGpuManifestFallback
        };
    }

    private static RemoteDataOptions Normalize(RemoteDataOptions input)
    {
        var runtimeDataUrl = (input.RuntimeDataUrl ?? "").Trim();
        var gpuBundleManifestUrl = (input.GpuBundleManifestUrl ?? "").Trim();
        var gpuBundleUrl = (input.GpuBundleUrl ?? "").Trim();
        var authV2BaseUrl = (input.AuthV2BaseUrl ?? "").Trim().TrimEnd('/');
        var dataV2BaseUrl = (input.DataV2BaseUrl ?? "").Trim().TrimEnd('/');
        var manifestEndpoint = (input.ManifestEndpoint ?? "").Trim();
        var channel = (input.Channel ?? "").Trim();
        if (string.IsNullOrWhiteSpace(channel))
        {
            channel = DefaultChannel;
        }

        return new RemoteDataOptions
        {
            // V2 is currently enforced intentionally.
            // V1 code paths remain in the tree only for rollback/reference and are not selectable by configuration.
            Protocol = RemoteDataProtocol.V2,
            RuntimeDataUrl = runtimeDataUrl,
            GpuBundleManifestUrl = gpuBundleManifestUrl,
            GpuBundleUrl = gpuBundleUrl,
            AuthV2BaseUrl = authV2BaseUrl,
            DataV2BaseUrl = dataV2BaseUrl,
            ManifestEndpoint = manifestEndpoint,
            Channel = channel,
            AllowMockGpuManifestFallback = input.AllowMockGpuManifestFallback
        };
    }

    private static RemoteDataProtocol ParseProtocol(string? value, RemoteDataProtocol fallback)
    {
        var normalized = (value ?? "").Trim();
        if (string.Equals(normalized, "v2", StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalized, "2", StringComparison.OrdinalIgnoreCase))
        {
            return RemoteDataProtocol.V2;
        }

        if (string.Equals(normalized, "v1", StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalized, "1", StringComparison.OrdinalIgnoreCase))
        {
            return RemoteDataProtocol.V1;
        }

        return fallback;
    }
}
