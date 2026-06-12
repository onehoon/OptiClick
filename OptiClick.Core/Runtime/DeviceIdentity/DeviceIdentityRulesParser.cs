using System.Text.Json;
namespace OptiClick.Core.Runtime.DeviceIdentity;

public sealed class DeviceIdentityRulesParser : IDeviceIdentityRulesParser
{
    public DeviceIdentityRulesParseResult Parse(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return DeviceIdentityRulesParseResult.Failure("empty_input");
        }

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json);
        }
        catch (JsonException)
        {
            return DeviceIdentityRulesParseResult.Failure("invalid_json");
        }

        using (document)
        {
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return DeviceIdentityRulesParseResult.Failure("invalid_root");
            }

            var manufacturerAliases = ReadAliasMap(document.RootElement, "manufacturer_aliases");
            var modelAliases = ReadAliasMap(document.RootElement, "model_aliases");

            if (manufacturerAliases.Count == 0 && modelAliases.Count == 0)
            {
                return DeviceIdentityRulesParseResult.Failure("rules_empty");
            }

            return DeviceIdentityRulesParseResult.Success(
                new DeviceIdentityRules
                {
                    ManufacturerAliases = manufacturerAliases,
                    ModelAliases = modelAliases
                });
        }
    }

    private static IReadOnlyDictionary<string, string> ReadAliasMap(JsonElement root, string propertyName)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!root.TryGetProperty(propertyName, out var aliasesElement)
            || aliasesElement.ValueKind != JsonValueKind.Object)
        {
            return map;
        }

        foreach (var property in aliasesElement.EnumerateObject())
        {
            if (property.Value.ValueKind != JsonValueKind.String)
            {
                continue;
            }

            var key = (property.Name ?? "").Trim();
            var value = (property.Value.GetString() ?? "").Trim();
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            map[key] = value;
        }

        return map;
    }
}
