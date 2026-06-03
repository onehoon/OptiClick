using System.Text.Json;
using System.Collections;

namespace OptiClick.Wpf.Install.Config;

public static class ConfigProfileNames
{
    public const string GameIniProfile = "game_ini_profile";
    public const string GameUnrealIniProfile = "game_unreal_ini_profile";
    public const string GameXmlProfile = "game_xml_profile";
    public const string GameJsonProfile = "game_json_profile";
    public const string EngineIniProfile = "engine_ini_profile";
    public const string RegistryProfile = "registry_profile";
}

public static class ConfigSkipReasons
{
    public const string MissingTargetFile = "missing_target_file";
    public const string MissingTargetDirectory = "missing_target_directory";
    public const string InvalidRow = "invalid_row";
    public const string UnsupportedOperation = "unsupported_operation";
    public const string MissingPathTarget = "missing_path_target";
    public const string UnsupportedHiveOrType = "unsupported_hive_or_type";
    public const string MissingRequiredField = "missing_required_field";
    public const string TargetFileNotFound = "target_file_not_found";
    public const string UnsupportedOp = "unsupported_op";
    public const string InvalidJsonPointer = "invalid_json_pointer";
    public const string JsonTargetMissing = "json_target_missing";
    public const string UnrealValuePathMissing = "unreal_value_path_missing";
    public const string UnrealTargetMissing = "unreal_target_missing";
    public const string XmlTargetMissing = "xml_target_missing";
    public const string ParentDirectoryMissing = "parent_directory_missing";
    public const string UnsupportedRegistryHive = "unsupported_registry_hive";
    public const string UnsupportedRegistryType = "unsupported_registry_type";
    public const string Unchanged = "unchanged";
}

public static class ConfigErrorReasons
{
    public const string ApplyFailed = "apply_failed";
    public const string RegistryWriteFailed = "registry_write_failed";
    public const string ApplyException = "apply_exception";
}

public sealed record ConfigProfileApplyEvent
{
    public string Stage { get; init; } = "";
    public string ProfileType { get; init; } = "";
    public string Path { get; init; } = "";
    public string Target { get; init; } = "";
    public string Action { get; init; } = ""; // applied / skipped / error
    public string ReasonCode { get; init; } = "";
}

public sealed record ConfigProfileAppliedRow
{
    public string ProfileName { get; init; } = "";
    public string TargetPath { get; init; } = "";
    public string TargetKey { get; init; } = "";
    public string ValuePath { get; init; } = "";
    public string OldValue { get; init; } = "";
    public string NewValue { get; init; } = "";
}

public sealed record ConfigProfileSkippedRow
{
    public string ProfileName { get; init; } = "";
    public string ReasonCode { get; init; } = "";
    public string Detail { get; init; } = "";
    public string TargetPath { get; init; } = "";
    public string TargetKey { get; init; } = "";
    public string ValuePath { get; init; } = "";
    public string OldValue { get; init; } = "";
    public string NewValue { get; init; } = "";
}

public sealed record ConfigProfileError
{
    public string ProfileName { get; init; } = "";
    public string ReasonCode { get; init; } = "";
    public string Detail { get; init; } = "";
    public string TargetPath { get; init; } = "";
    public string TargetKey { get; init; } = "";
    public string ValuePath { get; init; } = "";
    public string OldValue { get; init; } = "";
    public string NewValue { get; init; } = "";
}

public sealed record ConfigProfileApplySummary
{
    public string ProfileName { get; init; } = "";
    public bool Completed { get; init; } = true;
    public bool Changed { get; init; }
    public string TargetPathHint { get; init; } = "";
    public IReadOnlyList<ConfigProfileAppliedRow> Applied { get; init; } = Array.Empty<ConfigProfileAppliedRow>();
    public IReadOnlyList<ConfigProfileSkippedRow> Skipped { get; init; } = Array.Empty<ConfigProfileSkippedRow>();
    public IReadOnlyList<ConfigProfileError> Errors { get; init; } = Array.Empty<ConfigProfileError>();
    public int AppliedCount { get; init; }
    public int SkippedCount { get; init; }
    public int ErrorCount { get; init; }
    public IReadOnlyList<ConfigProfileApplyEvent> Events { get; init; } = Array.Empty<ConfigProfileApplyEvent>();
}

public sealed record ConfigProfileApplyResult
{
    public bool Changed { get; init; }
    public IReadOnlyList<ConfigProfileApplySummary> Summaries { get; init; } = Array.Empty<ConfigProfileApplySummary>();
    public IReadOnlyList<ConfigProfileAppliedRow> Applied { get; init; } = Array.Empty<ConfigProfileAppliedRow>();
    public IReadOnlyList<ConfigProfileSkippedRow> Skipped { get; init; } = Array.Empty<ConfigProfileSkippedRow>();
    public IReadOnlyList<ConfigProfileError> Errors { get; init; } = Array.Empty<ConfigProfileError>();
}

public sealed record ConfigProfileApplyContext
{
    public string TargetPath { get; init; } = "";
    public IReadOnlyDictionary<string, object?> GameData { get; init; } = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
}

public static class ConfigDataReader
{
    public static IReadOnlyList<IReadOnlyDictionary<string, object?>> ReadRows(
        IReadOnlyDictionary<string, object?> gameData,
        string key)
    {
        if (!gameData.TryGetValue(key, out var rawValue) || rawValue is null)
        {
            return Array.Empty<IReadOnlyDictionary<string, object?>>();
        }

        if (rawValue is JsonElement jsonElement)
        {
            return ReadRowsFromJsonElement(jsonElement);
        }

        if (rawValue is IEnumerable<object?> objectEnumerable)
        {
            return objectEnumerable
                .Select(ToDictionary)
                .Where(static item => item is not null)
                .Cast<IReadOnlyDictionary<string, object?>>()
                .ToArray();
        }

        if (rawValue is IEnumerable enumerable)
        {
            var rows = new List<IReadOnlyDictionary<string, object?>>();
            foreach (var item in enumerable)
            {
                var dictionary = ToDictionary(item);
                if (dictionary is not null)
                {
                    rows.Add(dictionary);
                }
            }

            return rows;
        }

        if (rawValue is IEnumerable<IReadOnlyDictionary<string, object?>> dictEnumerable)
        {
            return dictEnumerable.ToArray();
        }

        return Array.Empty<IReadOnlyDictionary<string, object?>>();
    }

    public static string ReadString(IReadOnlyDictionary<string, object?> row, string key)
    {
        if (!row.TryGetValue(key, out var value) || value is null)
        {
            return "";
        }

        return value switch
        {
            string text => text.Trim(),
            JsonElement json => JsonToString(json),
            _ => value.ToString()?.Trim() ?? ""
        };
    }

    public static object? ReadValue(IReadOnlyDictionary<string, object?> row, string key)
    {
        if (!row.TryGetValue(key, out var value))
        {
            return null;
        }

        if (value is JsonElement json)
        {
            return JsonToObject(json);
        }

        return value;
    }

    private static IReadOnlyList<IReadOnlyDictionary<string, object?>> ReadRowsFromJsonElement(JsonElement jsonElement)
    {
        if (jsonElement.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<IReadOnlyDictionary<string, object?>>();
        }

        var rows = new List<IReadOnlyDictionary<string, object?>>();
        foreach (var item in jsonElement.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var property in item.EnumerateObject())
            {
                row[property.Name] = JsonToObject(property.Value);
            }

            rows.Add(row);
        }

        return rows;
    }

    private static IReadOnlyDictionary<string, object?>? ToDictionary(object? value)
    {
        if (value is IReadOnlyDictionary<string, object?> readonlyDictionary)
        {
            return readonlyDictionary;
        }

        if (value is IDictionary<string, object?> mutableDictionary)
        {
            return new Dictionary<string, object?>(mutableDictionary, StringComparer.OrdinalIgnoreCase);
        }

        if (value is IDictionary<string, object> mutableObjectDictionary)
        {
            return mutableObjectDictionary.ToDictionary(
                static pair => pair.Key,
                static pair => (object?)pair.Value,
                StringComparer.OrdinalIgnoreCase);
        }

        if (value is JsonElement json && json.ValueKind == JsonValueKind.Object)
        {
            var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var property in json.EnumerateObject())
            {
                row[property.Name] = JsonToObject(property.Value);
            }

            return row;
        }

        return null;
    }

    private static string JsonToString(JsonElement json)
    {
        return json.ValueKind switch
        {
            JsonValueKind.String => json.GetString()?.Trim() ?? "",
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => "",
            _ => json.GetRawText().Trim()
        };
    }

    private static object? JsonToObject(JsonElement json)
    {
        return json.ValueKind switch
        {
            JsonValueKind.String => json.GetString(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Number when json.TryGetInt64(out var longValue) => longValue,
            JsonValueKind.Number when json.TryGetDouble(out var doubleValue) => doubleValue,
            JsonValueKind.Null => null,
            JsonValueKind.Array or JsonValueKind.Object => json.GetRawText(),
            _ => json.ToString()
        };
    }
}
