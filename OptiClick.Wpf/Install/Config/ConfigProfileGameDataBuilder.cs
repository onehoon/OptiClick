using System.Text.Json;
using OptiClick.Wpf.Shell.RuntimeData;

namespace OptiClick.Wpf.Install.Config;

public static class ConfigProfileGameDataBuilder
{
    public static IReadOnlyDictionary<string, object?> BuildFromProfileRows(AttachedRuntimeProfileRows profileRows)
    {
        return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            [ConfigProfileNames.GameIniProfile] = ConvertRows(profileRows.GameIniProfileRows),
            [ConfigProfileNames.GameUnrealIniProfile] = ConvertRows(profileRows.GameUnrealIniProfileRows),
            [ConfigProfileNames.GameXmlProfile] = ConvertRows(profileRows.GameXmlProfileRows),
            [ConfigProfileNames.GameJsonProfile] = ConvertRows(profileRows.GameJsonProfileRows),
            [ConfigProfileNames.EngineIniProfile] = ConvertRows(profileRows.EngineIniProfileRows),
            [ConfigProfileNames.RegistryProfile] = ConvertRows(profileRows.RegistryProfileRows)
        };
    }

    private static IReadOnlyList<IReadOnlyDictionary<string, object?>> ConvertRows(IReadOnlyList<RuntimeDataRawRow> rows)
    {
        if (rows is null || rows.Count == 0)
        {
            return Array.Empty<IReadOnlyDictionary<string, object?>>();
        }

        return rows.Select(ConvertRow).ToArray();
    }

    private static IReadOnlyDictionary<string, object?> ConvertRow(RuntimeDataRawRow row)
    {
        var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in row.Values)
        {
            values[key] = ConvertJsonElement(value);
        }

        return values;
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
