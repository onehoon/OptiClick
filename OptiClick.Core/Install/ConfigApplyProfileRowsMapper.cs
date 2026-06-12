using System.Text.Json;
using OptiClick.Core.RuntimeData;

namespace OptiClick.Core.Install;

public static class ConfigApplyProfileRowsMapper
{
    public static ConfigApplyProfileRows FromAttachedRuntimeProfileRows(AttachedRuntimeProfileRows? profileRows)
    {
        if (profileRows is null)
        {
            return ConfigApplyProfileRows.Empty;
        }

        return new ConfigApplyProfileRows
        {
            GameIniProfileRows = ConvertRows(ConfigApplyProfileKind.GameIni, profileRows.GameIniProfileRows),
            GameUnrealIniProfileRows = ConvertRows(ConfigApplyProfileKind.GameUnrealIni, profileRows.GameUnrealIniProfileRows),
            GameXmlProfileRows = ConvertRows(ConfigApplyProfileKind.GameXml, profileRows.GameXmlProfileRows),
            GameJsonProfileRows = ConvertRows(ConfigApplyProfileKind.GameJson, profileRows.GameJsonProfileRows),
            EngineIniProfileRows = ConvertRows(ConfigApplyProfileKind.EngineIni, profileRows.EngineIniProfileRows),
            RegistryProfileRows = ConvertRows(ConfigApplyProfileKind.Registry, profileRows.RegistryProfileRows)
        };
    }

    private static IReadOnlyList<ConfigApplyProfileRow> ConvertRows(
        ConfigApplyProfileKind kind,
        IReadOnlyList<RuntimeDataRawRow> rows)
    {
        if (rows is null || rows.Count == 0)
        {
            return [];
        }

        return rows.Select(row => ConvertRow(kind, row)).ToArray();
    }

    private static ConfigApplyProfileRow ConvertRow(ConfigApplyProfileKind kind, RuntimeDataRawRow row)
    {
        var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in row.Values)
        {
            values[key] = ConvertJsonElement(value);
        }

        return ConfigApplyProfileRow.FromValues(kind, values);
    }

    private static object? ConvertJsonElement(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null or JsonValueKind.Undefined => null,
            JsonValueKind.Number when value.TryGetInt64(out var longValue) => longValue,
            JsonValueKind.Number when value.TryGetDouble(out var doubleValue) => doubleValue,
            JsonValueKind.Array => value.EnumerateArray().Select(ConvertJsonElement).ToArray(),
            JsonValueKind.Object => value.EnumerateObject().ToDictionary(
                static property => property.Name,
                static property => ConvertJsonElement(property.Value),
                StringComparer.OrdinalIgnoreCase),
            _ => value.GetRawText()
        };
    }
}
