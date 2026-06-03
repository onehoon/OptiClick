using System.Text.Json;

namespace OptiClick.Wpf.Configuration;

public sealed class DeviceIdentityRulesOptionsLoader
{
    public const string EndpointEnvName = "OPTICLICK_DEVICE_IDENTITY_RULES_ENDPOINT";
    public const string EnabledEnvName = "OPTICLICK_DEVICE_IDENTITY_RULES_ENABLED";

    private readonly Func<string, string?> _readEnvironmentVariable;
    private readonly Func<JsonDocument> _loadAppSettingsDocument;

    public DeviceIdentityRulesOptionsLoader(
        string baseDirectory,
        Func<string, string?>? readEnvironmentVariable = null,
        Func<JsonDocument>? loadAppSettingsDocument = null)
    {
        _ = baseDirectory;
        _readEnvironmentVariable = readEnvironmentVariable ?? Environment.GetEnvironmentVariable;
        _loadAppSettingsDocument = loadAppSettingsDocument ?? EmbeddedAppSettingsReader.Load;
    }

    public DeviceIdentityRulesOptions Load()
    {
        var options = new DeviceIdentityRulesOptions();
        options = ApplyEmbeddedAppSettings(options);
        options = ApplyEnvironmentOverride(options);
        return Normalize(options);
    }

    private DeviceIdentityRulesOptions ApplyEmbeddedAppSettings(DeviceIdentityRulesOptions current)
    {
        using var document = _loadAppSettingsDocument();
        if (!document.RootElement.TryGetProperty("DeviceIdentityRules", out var section)
            || section.ValueKind != JsonValueKind.Object)
        {
            return current;
        }

        var endpoint = current.Endpoint;
        var enabled = current.Enabled;
        if (section.TryGetProperty("Endpoint", out var endpointElement)
            && endpointElement.ValueKind == JsonValueKind.String)
        {
            endpoint = endpointElement.GetString() ?? "";
        }

        if (section.TryGetProperty("Enabled", out var enabledElement))
        {
            enabled = ReadBool(enabledElement, current.Enabled);
        }

        return new DeviceIdentityRulesOptions
        {
            Endpoint = endpoint,
            Enabled = enabled
        };
    }

    private DeviceIdentityRulesOptions ApplyEnvironmentOverride(DeviceIdentityRulesOptions current)
    {
        var endpoint = current.Endpoint;
        var enabled = current.Enabled;
        var endpointEnv = _readEnvironmentVariable(EndpointEnvName);
        if (!string.IsNullOrWhiteSpace(endpointEnv))
        {
            endpoint = endpointEnv;
        }

        var enabledEnv = _readEnvironmentVariable(EnabledEnvName);
        if (!string.IsNullOrWhiteSpace(enabledEnv)
            && bool.TryParse(enabledEnv, out var parsedEnabled))
        {
            enabled = parsedEnabled;
        }

        return new DeviceIdentityRulesOptions
        {
            Endpoint = endpoint,
            Enabled = enabled
        };
    }

    private static DeviceIdentityRulesOptions Normalize(DeviceIdentityRulesOptions input)
    {
        return new DeviceIdentityRulesOptions
        {
            Endpoint = (input.Endpoint ?? "").Trim(),
            Enabled = input.Enabled
        };
    }

    private static bool ReadBool(JsonElement element, bool fallback)
    {
        if (element.ValueKind == JsonValueKind.True)
        {
            return true;
        }

        if (element.ValueKind == JsonValueKind.False)
        {
            return false;
        }

        if (element.ValueKind == JsonValueKind.String
            && bool.TryParse(element.GetString(), out var parsed))
        {
            return parsed;
        }

        return fallback;
    }
}
