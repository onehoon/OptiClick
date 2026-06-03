using System.Text.Json;

namespace OptiClick.Wpf.Configuration;

public sealed class SupportedGamesWikiOptionsLoader
{
    public const string EndpointEnvName = "OPTICLICK_SUPPORTED_GAMES_WIKI_ENDPOINT";
    public const string EnabledEnvName = "OPTICLICK_SUPPORTED_GAMES_WIKI_ENABLED";

    private readonly Func<string, string?> _readEnvironmentVariable;
    private readonly Func<JsonDocument> _loadAppSettingsDocument;

    public SupportedGamesWikiOptionsLoader(
        Func<string, string?>? readEnvironmentVariable = null,
        Func<JsonDocument>? loadAppSettingsDocument = null)
    {
        _readEnvironmentVariable = readEnvironmentVariable ?? Environment.GetEnvironmentVariable;
        _loadAppSettingsDocument = loadAppSettingsDocument ?? EmbeddedAppSettingsReader.Load;
    }

    public SupportedGamesWikiOptions Load()
    {
        var options = new SupportedGamesWikiOptions();
        options = ApplyEmbeddedAppSettings(options);
        options = ApplyEnvironmentOverride(options);
        return Normalize(options);
    }

    private SupportedGamesWikiOptions ApplyEmbeddedAppSettings(SupportedGamesWikiOptions current)
    {
        using var document = _loadAppSettingsDocument();
        var section = TryResolveSection(document.RootElement);
        if (section is null)
        {
            return current;
        }

        var endpoint = current.Endpoint;
        var enabled = current.Enabled;
        if (section.Value.TryGetProperty("Endpoint", out var endpointElement)
            && endpointElement.ValueKind == JsonValueKind.String)
        {
            endpoint = endpointElement.GetString() ?? "";
        }

        if (section.Value.TryGetProperty("Enabled", out var enabledElement))
        {
            enabled = ReadBool(enabledElement, current.Enabled);
        }

        return new SupportedGamesWikiOptions
        {
            Endpoint = endpoint,
            Enabled = enabled
        };
    }

    private SupportedGamesWikiOptions ApplyEnvironmentOverride(SupportedGamesWikiOptions current)
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

        return new SupportedGamesWikiOptions
        {
            Endpoint = endpoint,
            Enabled = enabled
        };
    }

    private static SupportedGamesWikiOptions Normalize(SupportedGamesWikiOptions input)
    {
        return new SupportedGamesWikiOptions
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

    private static JsonElement? TryResolveSection(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        if (root.TryGetProperty("SupportedGamesWiki", out var section)
            && section.ValueKind == JsonValueKind.Object)
        {
            return section;
        }

        if (root.TryGetProperty("SupportedGamesWikiList", out var legacySection)
            && legacySection.ValueKind == JsonValueKind.Object)
        {
            return legacySection;
        }

        return null;
    }
}
