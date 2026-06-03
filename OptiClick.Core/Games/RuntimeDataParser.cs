using System.Text.Json;
using OptiClick.Core.Models;

namespace OptiClick.Core.Games;

public sealed class RuntimeDataParser
{
    private static readonly string[] RequiredDataKeys =
    {
        "engine_ini_profile",
        "game_ini_profile",
        "game_json_profile",
        "game_master",
        "game_unreal_ini_profile",
        "game_xml_profile",
        "message_binding",
        "message_center",
        "registry_profile",
        "resource_master"
    };

    public RuntimeDataParseResult Parse(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        if (!root.TryGetProperty("schema_version", out var schemaElement) || schemaElement.GetInt32() != 1)
        {
            throw new InvalidOperationException("runtime-data schema_version must be 1.");
        }

        if (!root.TryGetProperty("data", out var dataElement) || dataElement.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException("runtime-data data object is required.");
        }

        foreach (var key in RequiredDataKeys)
        {
            if (!dataElement.TryGetProperty(key, out var listElement) || listElement.ValueKind != JsonValueKind.Array)
            {
                throw new InvalidOperationException($"runtime-data required list is missing: {key}.");
            }
        }

        var games = new List<GameEntry>();
        foreach (var rowElement in dataElement.GetProperty("game_master").EnumerateArray())
        {
            if (GameEntryMapper.TryMap(ToRow(rowElement), out var game))
            {
                games.Add(game);
            }
        }

        var resources = new List<ResourceEntry>();
        foreach (var rowElement in dataElement.GetProperty("resource_master").EnumerateArray())
        {
            resources.Add(ResourceEntryMapper.Map(ToRow(rowElement)));
        }

        return new RuntimeDataParseResult
        {
            SchemaVersion = 1,
            Games = games,
            Resources = resources
        };
    }

    private static IReadOnlyDictionary<string, string> ToRow(JsonElement element)
    {
        var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (element.ValueKind != JsonValueKind.Object)
        {
            return row;
        }

        foreach (var property in element.EnumerateObject())
        {
            row[property.Name] = property.Value.ValueKind switch
            {
                JsonValueKind.String => property.Value.GetString() ?? "",
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                JsonValueKind.Number => property.Value.GetRawText(),
                _ => property.Value.GetRawText()
            };
        }
        return row;
    }
}
