using OptiClick.Core.OptiScaler;

namespace OptiClick.Core.Install;

public static class ConfigApplyFailureCodes
{
    public const string ConfigProfileApplierMissing = "config_profile_applier_missing";
    public const string IniProfileEditorMissing = "ini_profile_editor_missing";
    public const string ConfigApplyFailed = "config_apply_failed";
    public const string ConfigApplyException = "config_apply_exception";
}

public sealed record ConfigApplyProfileRows
{
    public static ConfigApplyProfileRows Empty { get; } = new();

    public IReadOnlyList<ConfigApplyProfileRow> GameIniProfileRows { get; init; } = [];
    public IReadOnlyList<ConfigApplyProfileRow> GameUnrealIniProfileRows { get; init; } = [];
    public IReadOnlyList<ConfigApplyProfileRow> EngineIniProfileRows { get; init; } = [];
    public IReadOnlyList<ConfigApplyProfileRow> GameXmlProfileRows { get; init; } = [];
    public IReadOnlyList<ConfigApplyProfileRow> RegistryProfileRows { get; init; } = [];
    public IReadOnlyList<ConfigApplyProfileRow> GameJsonProfileRows { get; init; } = [];

    public bool HasAnyRows =>
        GameIniProfileRows.Count > 0
        || GameUnrealIniProfileRows.Count > 0
        || EngineIniProfileRows.Count > 0
        || GameXmlProfileRows.Count > 0
        || RegistryProfileRows.Count > 0
        || GameJsonProfileRows.Count > 0;
}

public sealed record ConfigApplyProfileRow
{
    private static readonly ConfigApplyProfileValueSet EmptyValues = ConfigApplyProfileValueSet.Empty;

    public ConfigApplyProfileValueSet Values { get; init; } = EmptyValues;
    public ConfigApplyProfileEdit Edit { get; init; } = ConfigApplyProfileEdit.Empty;

    public static ConfigApplyProfileRow FromValues(IReadOnlyDictionary<string, object?>? values)
    {
        return FromValues(ConfigApplyProfileKind.Unknown, values);
    }

    public static ConfigApplyProfileRow FromValues(
        ConfigApplyProfileKind kind,
        IReadOnlyDictionary<string, object?>? values)
    {
        return new ConfigApplyProfileRow
        {
            Values = ConfigApplyProfileValueSet.FromValues(values),
            Edit = ConfigApplyProfileEdit.FromValues(kind, values)
        };
    }
}

public enum ConfigApplyProfileKind
{
    Unknown = 0,
    GameIni,
    GameUnrealIni,
    EngineIni,
    GameXml,
    GameJson,
    Registry
}

public sealed record ConfigApplyProfileEdit
{
    public static ConfigApplyProfileEdit Empty { get; } = new();

    public ConfigApplyProfileKind Kind { get; init; } = ConfigApplyProfileKind.Unknown;
    public string TargetPathHint { get; init; } = "";
    public string Section { get; init; } = "";
    public string Key { get; init; } = "";
    public string ValuePath { get; init; } = "";
    public ConfigApplyValue Value { get; init; } = ConfigApplyValue.Empty;
    public string ValueType { get; init; } = "";
    public string Operation { get; init; } = "";
    public string RegistryHive { get; init; } = "";
    public string RegistryKeyPath { get; init; } = "";
    public string RegistryValueName { get; init; } = "";

    public static ConfigApplyProfileEdit FromValues(
        ConfigApplyProfileKind kind,
        IReadOnlyDictionary<string, object?>? values)
    {
        var valueSet = ConfigApplyProfileValueSet.FromValues(values);
        if (ReferenceEquals(valueSet, ConfigApplyProfileValueSet.Empty))
        {
            return Empty with { Kind = kind };
        }

        return new ConfigApplyProfileEdit
        {
            Kind = kind,
            TargetPathHint = ReadString(valueSet, "path"),
            Section = ReadString(valueSet, "section"),
            Key = ReadString(valueSet, "key"),
            ValuePath = ReadFirstString(valueSet, "value_path", "json_path", "xml_path"),
            Value = valueSet.TryGetValue("value", out var value)
                ? ConfigApplyValue.FromRaw(value)
                : ConfigApplyValue.Empty,
            ValueType = ReadString(valueSet, "value_type"),
            Operation = ReadString(valueSet, "op"),
            RegistryHive = ReadString(valueSet, "hive"),
            RegistryKeyPath = ReadString(valueSet, "key_path"),
            RegistryValueName = ReadString(valueSet, "value_name")
        };
    }

    private static string ReadFirstString(ConfigApplyProfileValueSet values, params string[] keys)
    {
        foreach (var key in keys)
        {
            var value = ReadString(values, key);
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return "";
    }

    private static string ReadString(ConfigApplyProfileValueSet values, string key)
    {
        if (!values.TryGetValue(key, out var value) || value is null)
        {
            return "";
        }

        return value switch
        {
            string text => text.Trim(),
            _ => value.ToString()?.Trim() ?? ""
        };
    }
}

public sealed record ConfigApplyValue
{
    public static ConfigApplyValue Empty { get; } = new();

    public bool HasValue { get; init; }
    public object? RawValue { get; init; }

    public static ConfigApplyValue FromRaw(object? value)
    {
        return new ConfigApplyValue
        {
            HasValue = true,
            RawValue = value
        };
    }
}

public sealed record ConfigApplyProfileValueSet
{
    public static ConfigApplyProfileValueSet Empty { get; } = new();

    public IReadOnlyList<ConfigApplyProfileValue> Entries { get; init; } = [];

    public object? this[string key]
    {
        get
        {
            if (TryGetValue(key, out var value))
            {
                return value;
            }

            throw new KeyNotFoundException(key);
        }
    }

    public static ConfigApplyProfileValueSet FromValues(IReadOnlyDictionary<string, object?>? values)
    {
        if (values is null || values.Count == 0)
        {
            return Empty;
        }

        var normalized = new Dictionary<string, ConfigApplyProfileValue>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in values)
        {
            var key = (pair.Key ?? "").Trim();
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            normalized[key] = new ConfigApplyProfileValue(key, pair.Value);
        }

        if (normalized.Count == 0)
        {
            return Empty;
        }

        return new ConfigApplyProfileValueSet
        {
            Entries = normalized.Values.ToArray()
        };
    }

    public bool TryGetValue(string key, out object? value)
    {
        var normalizedKey = (key ?? "").Trim();
        if (string.IsNullOrWhiteSpace(normalizedKey))
        {
            value = null;
            return false;
        }

        foreach (var entry in Entries)
        {
            if (string.Equals(entry.Key, normalizedKey, StringComparison.OrdinalIgnoreCase))
            {
                value = entry.Value;
                return true;
            }
        }

        value = null;
        return false;
    }

    public IReadOnlyDictionary<string, object?> ToDictionary()
    {
        return Entries.ToDictionary(
            static entry => entry.Key,
            static entry => entry.Value,
            StringComparer.OrdinalIgnoreCase);
    }
}

public sealed record ConfigApplyProfileValue(string Key, object? Value);

public sealed record ConfigApplyApplicationRequest
{
    public required string TargetFolder { get; init; }
    public ConfigApplyProfileRows ProfileRows { get; init; } = ConfigApplyProfileRows.Empty;
    public OptiScalerIniApplyContext OptiScalerIniApplyContext { get; init; } = new();
}

public sealed record ConfigApplyStageResult
{
    public bool HasFailure { get; init; }
    public int ErrorCount { get; init; }
    public IReadOnlyList<ConfigApplyEvent> Events { get; init; } = [];
    public IReadOnlyList<ConfigApplyIssue> Issues { get; init; } = [];
}

public interface IConfigApplyOptiScalerIniStageRunner
{
    ConfigApplyStageResult Apply(string targetFolder, OptiScalerIniApplyPlan plan);
}

public interface IConfigApplyProfileStageRunner
{
    ConfigApplyStageResult Apply(string targetPath, ConfigApplyProfileRows profileRows);
}
