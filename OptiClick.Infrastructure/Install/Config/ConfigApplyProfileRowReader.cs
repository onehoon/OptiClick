using System.Text.Json;
using OptiClick.Core.Install;

namespace OptiClick.Infrastructure.Install.Config;

internal static class ConfigApplyProfileRowReader
{
    public static string ReadTargetPathHint(ConfigApplyProfileRow row)
    {
        return ReadString(row, "path", row.Edit.TargetPathHint);
    }

    public static string ReadSection(ConfigApplyProfileRow row)
    {
        return ReadString(row, "section", row.Edit.Section);
    }

    public static string ReadKey(ConfigApplyProfileRow row)
    {
        return ReadString(row, "key", row.Edit.Key);
    }

    public static string ReadValuePath(ConfigApplyProfileRow row, params string[] fieldNames)
    {
        foreach (var fieldName in fieldNames)
        {
            var value = ReadString(row, fieldName, "");
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return (row.Edit.ValuePath ?? "").Trim();
    }

    public static string ReadValueType(ConfigApplyProfileRow row)
    {
        return ReadString(row, "value_type", row.Edit.ValueType);
    }

    public static string ReadOperation(ConfigApplyProfileRow row)
    {
        return ReadString(row, "op", row.Edit.Operation);
    }

    public static string ReadRegistryHive(ConfigApplyProfileRow row)
    {
        return ReadString(row, "hive", row.Edit.RegistryHive);
    }

    public static string ReadRegistryKeyPath(ConfigApplyProfileRow row)
    {
        return ReadString(row, "key_path", row.Edit.RegistryKeyPath);
    }

    public static string ReadRegistryValueName(ConfigApplyProfileRow row)
    {
        return ReadString(row, "value_name", row.Edit.RegistryValueName);
    }

    public static object? ReadValue(ConfigApplyProfileRow row)
    {
        if (row.Values.TryGetValue("value", out var value))
        {
            return value;
        }

        return row.Edit.Value.HasValue ? row.Edit.Value.RawValue : null;
    }

    public static object? ReadRawValue(ConfigApplyProfileRow row, string key, object? fallback = null)
    {
        return row.Values.TryGetValue(key, out var value) ? value : fallback;
    }

    private static string ReadString(ConfigApplyProfileRow row, string key, string fallback)
    {
        if (row.Values.TryGetValue(key, out var value) && value is not null)
        {
            return ToText(value);
        }

        return (fallback ?? "").Trim();
    }

    private static string ToText(object value)
    {
        return value switch
        {
            string text => text.Trim(),
            JsonElement json => JsonToString(json),
            _ => value.ToString()?.Trim() ?? ""
        };
    }

    private static string JsonToString(JsonElement json)
    {
        return json.ValueKind switch
        {
            JsonValueKind.String => json.GetString()?.Trim() ?? "",
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null or JsonValueKind.Undefined => "",
            _ => json.GetRawText().Trim()
        };
    }
}
