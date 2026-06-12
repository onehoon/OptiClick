using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

using OptiClick.Core.Install;

namespace OptiClick.Infrastructure.Install.Config;

public sealed class JsonProfileEditor
{
    private readonly IProfilePathResolver _pathResolver;

    public JsonProfileEditor(IProfilePathResolver pathResolver)
    {
        _pathResolver = pathResolver;
    }

    public ConfigProfileApplySummary Apply(string targetPath, ConfigApplyProfileRows profileRows)
    {
        var skipped = new List<ConfigProfileSkippedRow>();
        var targets = CollectTargets(
            targetPath,
            (profileRows ?? ConfigApplyProfileRows.Empty).GameJsonProfileRows,
            skipped);
        return ApplyTargets(targets, skipped);
    }

    public ConfigProfileApplySummary Apply(string targetPath, IReadOnlyDictionary<string, object?> gameData)
    {
        var skipped = new List<ConfigProfileSkippedRow>();
        var targets = CollectTargets(targetPath, gameData, skipped);
        return ApplyTargets(targets, skipped);
    }

    private static ConfigProfileApplySummary ApplyTargets(
        IReadOnlyDictionary<string, List<JsonProfileRow>> targets,
        List<ConfigProfileSkippedRow> skipped)
    {
        var profileName = ConfigProfileNames.GameJsonProfile;
        var applied = new List<ConfigProfileAppliedRow>();
        var errors = new List<ConfigProfileError>();
        var changedAny = false;

        foreach (var (filePath, rows) in targets)
        {
            var fileApplied = new List<ConfigProfileAppliedRow>();
            var fileSkipped = new List<ConfigProfileSkippedRow>();
            try
            {
                OptionalFileEditRunner.ApplyExistingFileSettings(
                    filePath,
                    () =>
                    {
                        var changed = ApplyRows(filePath, rows, fileApplied, fileSkipped);
                        changedAny = changedAny || changed;
                    },
                    restoreOriginalReadonly: true);
                applied.AddRange(fileApplied);
                skipped.AddRange(fileSkipped);
            }
            catch (Exception ex)
            {
                errors.Add(new ConfigProfileError
                {
                    ProfileName = profileName,
                    ReasonCode = ConfigErrorReasons.ApplyFailed,
                    Detail = $"{Path.GetFileName(filePath)}: {ex.Message}",
                    TargetPath = filePath
                });
            }
        }

        return new ConfigProfileApplySummary
        {
            ProfileName = profileName,
            Changed = changedAny,
            Applied = applied,
            Skipped = skipped,
            Errors = errors,
            Completed = true
        };
    }

    private Dictionary<string, List<JsonProfileRow>> CollectTargets(
        string targetPath,
        IReadOnlyDictionary<string, object?> gameData,
        List<ConfigProfileSkippedRow> skipped)
    {
        var grouped = new Dictionary<string, List<JsonProfileRow>>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in ConfigDataReader.ReadRows(gameData, ConfigProfileNames.GameJsonProfile))
        {
            var profilePath = ConfigDataReader.ReadString(row, "path");
            var jsonPath = ConfigDataReader.ReadString(row, "json_path");
            var op = ConfigDataReader.ReadString(row, "op");
            op = string.IsNullOrWhiteSpace(op) ? "set" : op.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(profilePath) || string.IsNullOrWhiteSpace(jsonPath))
            {
                skipped.Add(new ConfigProfileSkippedRow
                {
                    ProfileName = ConfigProfileNames.GameJsonProfile,
                    ReasonCode = ConfigSkipReasons.InvalidRow,
                    Detail = "path/json_path"
                });
                continue;
            }

            if (!string.Equals(op, "set", StringComparison.Ordinal))
            {
                skipped.Add(new ConfigProfileSkippedRow
                {
                    ProfileName = ConfigProfileNames.GameJsonProfile,
                    ReasonCode = ConfigSkipReasons.UnsupportedOperation,
                    Detail = op
                });
                continue;
            }

            var resolvedPath = _pathResolver.Resolve(targetPath, profilePath, requireExisting: true);
            if (resolvedPath is null)
            {
                skipped.Add(new ConfigProfileSkippedRow
                {
                    ProfileName = ConfigProfileNames.GameJsonProfile,
                    ReasonCode = ConfigSkipReasons.MissingTargetFile,
                    Detail = profilePath
                });
                continue;
            }

            if (!grouped.TryGetValue(resolvedPath, out var list))
            {
                list = new List<JsonProfileRow>();
                grouped[resolvedPath] = list;
            }

            list.Add(new JsonProfileRow
            {
                JsonPath = jsonPath,
                Value = ConfigDataReader.ReadValue(row, "value"),
                ValueType = ConfigDataReader.ReadString(row, "value_type")
            });
        }

        return grouped;
    }

    private Dictionary<string, List<JsonProfileRow>> CollectTargets(
        string targetPath,
        IReadOnlyList<ConfigApplyProfileRow> profileRows,
        List<ConfigProfileSkippedRow> skipped)
    {
        var grouped = new Dictionary<string, List<JsonProfileRow>>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in profileRows ?? [])
        {
            var profilePath = ConfigApplyProfileRowReader.ReadTargetPathHint(row);
            var jsonPath = ConfigApplyProfileRowReader.ReadValuePath(row, "json_path");
            var op = ConfigApplyProfileRowReader.ReadOperation(row);
            op = string.IsNullOrWhiteSpace(op) ? "set" : op.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(profilePath) || string.IsNullOrWhiteSpace(jsonPath))
            {
                skipped.Add(new ConfigProfileSkippedRow
                {
                    ProfileName = ConfigProfileNames.GameJsonProfile,
                    ReasonCode = ConfigSkipReasons.InvalidRow,
                    Detail = "path/json_path"
                });
                continue;
            }

            if (!string.Equals(op, "set", StringComparison.Ordinal))
            {
                skipped.Add(new ConfigProfileSkippedRow
                {
                    ProfileName = ConfigProfileNames.GameJsonProfile,
                    ReasonCode = ConfigSkipReasons.UnsupportedOperation,
                    Detail = op
                });
                continue;
            }

            var resolvedPath = _pathResolver.Resolve(targetPath, profilePath, requireExisting: true);
            if (resolvedPath is null)
            {
                skipped.Add(new ConfigProfileSkippedRow
                {
                    ProfileName = ConfigProfileNames.GameJsonProfile,
                    ReasonCode = ConfigSkipReasons.MissingTargetFile,
                    Detail = profilePath
                });
                continue;
            }

            if (!grouped.TryGetValue(resolvedPath, out var list))
            {
                list = new List<JsonProfileRow>();
                grouped[resolvedPath] = list;
            }

            list.Add(new JsonProfileRow
            {
                JsonPath = jsonPath,
                Value = ConfigApplyProfileRowReader.ReadValue(row),
                ValueType = ConfigApplyProfileRowReader.ReadValueType(row)
            });
        }

        return grouped;
    }

    private static bool ApplyRows(
        string filePath,
        IReadOnlyList<JsonProfileRow> rows,
        List<ConfigProfileAppliedRow> applied,
        List<ConfigProfileSkippedRow> skipped)
    {
        var readResult = JsonTextCodec.ReadWithFallback(filePath);
        var rootNode = JsonNode.Parse(readResult.Text);
        if (rootNode is null)
        {
            return false;
        }

        var changed = false;
        foreach (var row in rows)
        {
            IReadOnlyList<string> tokens;
            try
            {
                tokens = ParseJsonPointer(row.JsonPath);
            }
            catch
            {
                skipped.Add(new ConfigProfileSkippedRow
                {
                    ProfileName = ConfigProfileNames.GameJsonProfile,
                    ReasonCode = ConfigSkipReasons.InvalidRow,
                    Detail = row.JsonPath
                });
                continue;
            }

            if (!TryResolveExistingTarget(rootNode, tokens, out var containerNode, out var targetToken))
            {
                skipped.Add(new ConfigProfileSkippedRow
                {
                    ProfileName = ConfigProfileNames.GameJsonProfile,
                    ReasonCode = ConfigSkipReasons.MissingPathTarget,
                    Detail = row.JsonPath
                });
                continue;
            }

            var coercedValue = CoerceJsonValue(row.Value, row.ValueType);
            if (!TryGetExistingJsonValue(containerNode!, targetToken, out var oldNode))
            {
                skipped.Add(new ConfigProfileSkippedRow
                {
                    ProfileName = ConfigProfileNames.GameJsonProfile,
                    ReasonCode = ConfigSkipReasons.MissingPathTarget,
                    Detail = row.JsonPath,
                    TargetPath = filePath,
                    TargetKey = row.JsonPath,
                    OldValue = "<missing>",
                    NewValue = FormatJsonValueForLog(coercedValue)
                });
                continue;
            }

            var oldValueText = FormatJsonNodeForLog(oldNode);
            var newValueText = FormatJsonValueForLog(coercedValue);
            if (string.Equals(
                    FormatJsonNodeForCompare(oldNode),
                    FormatJsonValueForCompare(coercedValue),
                    StringComparison.Ordinal))
            {
                skipped.Add(new ConfigProfileSkippedRow
                {
                    ProfileName = ConfigProfileNames.GameJsonProfile,
                    ReasonCode = ConfigSkipReasons.Unchanged,
                    Detail = row.JsonPath,
                    TargetPath = filePath,
                    TargetKey = row.JsonPath,
                    OldValue = oldValueText,
                    NewValue = newValueText
                });
                continue;
            }

            if (!TrySetExistingJsonValue(containerNode!, targetToken, coercedValue))
            {
                skipped.Add(new ConfigProfileSkippedRow
                {
                    ProfileName = ConfigProfileNames.GameJsonProfile,
                    ReasonCode = ConfigSkipReasons.MissingPathTarget,
                    Detail = row.JsonPath,
                    TargetPath = filePath,
                    TargetKey = row.JsonPath,
                    OldValue = oldValueText,
                    NewValue = newValueText
                });
                continue;
            }

            changed = true;
            applied.Add(new ConfigProfileAppliedRow
            {
                ProfileName = ConfigProfileNames.GameJsonProfile,
                TargetPath = filePath,
                TargetKey = row.JsonPath,
                OldValue = oldValueText,
                NewValue = newValueText
            });
        }

        if (!changed)
        {
            return false;
        }

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
        var rebuilt = rootNode.ToJsonString(options) + "\n";
        JsonTextCodec.WriteWithOriginalEncoding(filePath, rebuilt, readResult.EncodingInfo);
        return true;
    }

    public static IReadOnlyList<string> ParseJsonPointer(string jsonPath)
    {
        var normalized = (jsonPath ?? "").Trim();
        if (string.IsNullOrWhiteSpace(normalized) || !normalized.StartsWith("/", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("json_path must start with '/'.");
        }

        var tokens = normalized.Split('/').Skip(1)
            .Select(static token => token.Replace("~1", "/").Replace("~0", "~"))
            .ToArray();
        return tokens;
    }

    public static object? CoerceJsonValue(object? value, string valueType)
    {
        var normalizedType = (valueType ?? "").Trim().ToLowerInvariant();
        if (normalizedType is "string" or "str" or "")
        {
            return value is null ? "" : value.ToString();
        }

        if (normalizedType is "int" or "integer")
        {
            return Convert.ToInt32(value);
        }

        if (normalizedType is "float" or "double")
        {
            return Convert.ToDouble(value, System.Globalization.CultureInfo.InvariantCulture);
        }

        if (normalizedType is "bool" or "boolean")
        {
            if (value is bool boolValue)
            {
                return boolValue;
            }

            var normalizedValue = (value?.ToString() ?? "").Trim().ToLowerInvariant();
            if (normalizedValue is "1" or "true" or "yes" or "on")
            {
                return true;
            }

            if (normalizedValue is "0" or "false" or "no" or "off")
            {
                return false;
            }

            throw new InvalidOperationException($"Unsupported bool value: {value}");
        }

        if (normalizedType == "null")
        {
            return null;
        }

        if (normalizedType == "json")
        {
            if (value is string jsonText)
            {
                return JsonNode.Parse(jsonText);
            }

            return JsonSerializer.SerializeToNode(value);
        }

        return value;
    }

    private static bool TryResolveExistingTarget(JsonNode rootNode, IReadOnlyList<string> tokens, out JsonNode? containerNode, out string targetToken)
    {
        containerNode = null;
        targetToken = "";
        if (tokens.Count == 0)
        {
            return false;
        }

        JsonNode? current = rootNode;
        foreach (var token in tokens.Take(tokens.Count - 1))
        {
            switch (current)
            {
                case JsonObject jsonObject:
                    if (!jsonObject.TryGetPropertyValue(token, out current) || current is null)
                    {
                        return false;
                    }

                    break;
                case JsonArray jsonArray:
                    if (!int.TryParse(token, out var index) || index < 0 || index >= jsonArray.Count)
                    {
                        return false;
                    }

                    current = jsonArray[index];
                    if (current is null)
                    {
                        return false;
                    }

                    break;
                default:
                    return false;
            }
        }

        containerNode = current;
        targetToken = tokens[^1];
        return containerNode is not null;
    }

    private static bool TrySetExistingJsonValue(JsonNode containerNode, string token, object? value)
    {
        var replacementNode = value as JsonNode ?? JsonSerializer.SerializeToNode(value);
        switch (containerNode)
        {
            case JsonObject jsonObject:
                if (!jsonObject.ContainsKey(token))
                {
                    return false;
                }

                jsonObject[token] = replacementNode;
                return true;

            case JsonArray jsonArray:
                if (!int.TryParse(token, out var index) || index < 0 || index >= jsonArray.Count)
                {
                    return false;
                }

                jsonArray[index] = replacementNode;
                return true;

            default:
                return false;
        }
    }

    private static bool TryGetExistingJsonValue(JsonNode containerNode, string token, out JsonNode? value)
    {
        value = null;
        switch (containerNode)
        {
            case JsonObject jsonObject:
                if (!jsonObject.ContainsKey(token))
                {
                    return false;
                }

                value = jsonObject[token];
                return true;

            case JsonArray jsonArray:
                if (!int.TryParse(token, out var index) || index < 0 || index >= jsonArray.Count)
                {
                    return false;
                }

                value = jsonArray[index];
                return true;

            default:
                return false;
        }
    }

    private static string FormatJsonValueForLog(object? value)
    {
        return FormatJsonNodeForLog(value as JsonNode ?? JsonSerializer.SerializeToNode(value));
    }

    private static string FormatJsonValueForCompare(object? value)
    {
        return FormatJsonNodeForCompare(value as JsonNode ?? JsonSerializer.SerializeToNode(value));
    }

    private static string FormatJsonNodeForCompare(JsonNode? node)
    {
        return node?.ToJsonString(new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        }) ?? "null";
    }

    private static string FormatJsonNodeForLog(JsonNode? node)
    {
        if (node is null)
        {
            return "null";
        }

        if (node is JsonValue jsonValue && jsonValue.TryGetValue<string>(out var stringValue))
        {
            return stringValue;
        }

        return node.ToJsonString(new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });
    }

    private sealed record JsonProfileRow
    {
        public string JsonPath { get; init; } = "";
        public object? Value { get; init; }
        public string ValueType { get; init; } = "";
    }
}
