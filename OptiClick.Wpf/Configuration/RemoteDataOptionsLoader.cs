using System.Text.Json;
using OptiClick.Core.Runtime;

namespace OptiClick.Wpf.Configuration;

public sealed class RemoteDataOptionsLoader
{
    public const string RuntimeDataUrlEnvName = "OPTICLICK_RUNTIME_DATA_URL";
    public const string GpuBundleManifestUrlEnvName = "OPTICLICK_GPU_BUNDLE_MANIFEST_URL";
    public const string GpuBundleUrlEnvName = "OPTICLICK_GPU_BUNDLE_URL";
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
            RuntimeDataUrl = "",
            GpuBundleManifestUrl = "",
            GpuBundleUrl = "",
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
        var channel = current.Channel;

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
            RuntimeDataUrl = runtimeDataUrl,
            GpuBundleManifestUrl = gpuBundleManifestUrl,
            GpuBundleUrl = gpuBundleUrl,
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
        var channel = current.Channel;

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
            RuntimeDataUrl = runtimeDataUrl,
            GpuBundleManifestUrl = gpuBundleManifestUrl,
            GpuBundleUrl = gpuBundleUrl,
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
        var manifestEndpoint = (input.ManifestEndpoint ?? "").Trim();
        var channel = (input.Channel ?? "").Trim();
        if (string.IsNullOrWhiteSpace(channel))
        {
            channel = DefaultChannel;
        }

        return new RemoteDataOptions
        {
            RuntimeDataUrl = runtimeDataUrl,
            GpuBundleManifestUrl = gpuBundleManifestUrl,
            GpuBundleUrl = gpuBundleUrl,
            ManifestEndpoint = manifestEndpoint,
            Channel = channel,
            AllowMockGpuManifestFallback = input.AllowMockGpuManifestFallback
        };
    }
}
