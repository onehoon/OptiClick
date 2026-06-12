using OptiClick.Core.Install;

namespace OptiClick.Infrastructure.Install.Config;

public static class ConfigProfileGameDataBuilder
{
    public static IReadOnlyDictionary<string, object?> BuildGameIniData(ConfigApplyProfileRows profileRows)
    {
        ArgumentNullException.ThrowIfNull(profileRows);
        return BuildSingleProfileData(
            ConfigProfileNames.GameIniProfile,
            ConfigApplyProfileKind.GameIni,
            profileRows.GameIniProfileRows);
    }

    public static IReadOnlyDictionary<string, object?> BuildEngineIniData(ConfigApplyProfileRows profileRows)
    {
        ArgumentNullException.ThrowIfNull(profileRows);
        return BuildSingleProfileData(
            ConfigProfileNames.EngineIniProfile,
            ConfigApplyProfileKind.EngineIni,
            profileRows.EngineIniProfileRows);
    }

    public static IReadOnlyDictionary<string, object?> BuildGameUnrealIniData(ConfigApplyProfileRows profileRows)
    {
        ArgumentNullException.ThrowIfNull(profileRows);
        return BuildSingleProfileData(
            ConfigProfileNames.GameUnrealIniProfile,
            ConfigApplyProfileKind.GameUnrealIni,
            profileRows.GameUnrealIniProfileRows);
    }

    public static IReadOnlyDictionary<string, object?> BuildGameXmlData(ConfigApplyProfileRows profileRows)
    {
        ArgumentNullException.ThrowIfNull(profileRows);
        return BuildSingleProfileData(
            ConfigProfileNames.GameXmlProfile,
            ConfigApplyProfileKind.GameXml,
            profileRows.GameXmlProfileRows);
    }

    public static IReadOnlyDictionary<string, object?> BuildGameJsonData(ConfigApplyProfileRows profileRows)
    {
        ArgumentNullException.ThrowIfNull(profileRows);
        return BuildSingleProfileData(
            ConfigProfileNames.GameJsonProfile,
            ConfigApplyProfileKind.GameJson,
            profileRows.GameJsonProfileRows);
    }

    public static IReadOnlyDictionary<string, object?> BuildRegistryData(ConfigApplyProfileRows profileRows)
    {
        ArgumentNullException.ThrowIfNull(profileRows);
        return BuildSingleProfileData(
            ConfigProfileNames.RegistryProfile,
            ConfigApplyProfileKind.Registry,
            profileRows.RegistryProfileRows);
    }

    public static IReadOnlyDictionary<string, object?> BuildFromProfileRows(ConfigApplyProfileRows profileRows)
    {
        ArgumentNullException.ThrowIfNull(profileRows);

        return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            [ConfigProfileNames.GameIniProfile] = ToRawRows(ConfigApplyProfileKind.GameIni, profileRows.GameIniProfileRows),
            [ConfigProfileNames.GameUnrealIniProfile] = ToRawRows(ConfigApplyProfileKind.GameUnrealIni, profileRows.GameUnrealIniProfileRows),
            [ConfigProfileNames.GameXmlProfile] = ToRawRows(ConfigApplyProfileKind.GameXml, profileRows.GameXmlProfileRows),
            [ConfigProfileNames.GameJsonProfile] = ToRawRows(ConfigApplyProfileKind.GameJson, profileRows.GameJsonProfileRows),
            [ConfigProfileNames.EngineIniProfile] = ToRawRows(ConfigApplyProfileKind.EngineIni, profileRows.EngineIniProfileRows),
            [ConfigProfileNames.RegistryProfile] = ToRawRows(ConfigApplyProfileKind.Registry, profileRows.RegistryProfileRows)
        };
    }

    private static IReadOnlyDictionary<string, object?> BuildSingleProfileData(
        string profileName,
        ConfigApplyProfileKind kind,
        IReadOnlyList<ConfigApplyProfileRow> rows)
    {
        return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            [profileName] = ToRawRows(kind, rows)
        };
    }

    private static IReadOnlyList<IReadOnlyDictionary<string, object?>> ToRawRows(
        ConfigApplyProfileKind kind,
        IReadOnlyList<ConfigApplyProfileRow> rows)
    {
        return rows.Select(row => ToRawRow(
            row.Values,
            row.Edit.Kind == ConfigApplyProfileKind.Unknown
                ? row.Edit with { Kind = kind }
                : row.Edit)).ToArray();
    }

    private static IReadOnlyDictionary<string, object?> ToRawRow(
        ConfigApplyProfileValueSet originalValues,
        ConfigApplyProfileEdit edit)
    {
        var row = new Dictionary<string, object?>(originalValues.ToDictionary(), StringComparer.OrdinalIgnoreCase);
        switch (edit.Kind)
        {
            case ConfigApplyProfileKind.GameIni:
            case ConfigApplyProfileKind.EngineIni:
                AddText(row, "path", edit.TargetPathHint);
                AddText(row, "section", edit.Section);
                AddText(row, "key", edit.Key);
                AddValue(row, "value", edit.Value);
                AddText(row, "value_type", edit.ValueType);
                break;
            case ConfigApplyProfileKind.GameUnrealIni:
                AddText(row, "path", edit.TargetPathHint);
                AddText(row, "section", edit.Section);
                AddText(row, "key", edit.Key);
                AddText(row, "value_path", edit.ValuePath);
                AddValue(row, "value", edit.Value);
                AddText(row, "value_type", edit.ValueType);
                break;
            case ConfigApplyProfileKind.GameXml:
                AddText(row, "path", edit.TargetPathHint);
                AddText(row, "xml_path", edit.ValuePath);
                AddValue(row, "value", edit.Value);
                break;
            case ConfigApplyProfileKind.GameJson:
                AddText(row, "path", edit.TargetPathHint);
                AddText(row, "json_path", edit.ValuePath);
                AddText(row, "op", edit.Operation);
                AddValue(row, "value", edit.Value);
                AddText(row, "value_type", edit.ValueType);
                break;
            case ConfigApplyProfileKind.Registry:
                AddText(row, "hive", edit.RegistryHive);
                AddText(row, "key_path", edit.RegistryKeyPath);
                AddText(row, "value_name", edit.RegistryValueName);
                AddText(row, "value_type", edit.ValueType);
                AddValue(row, "value", edit.Value);
                break;
            default:
                break;
        }

        return row;
    }

    private static void AddText(IDictionary<string, object?> row, string key, string value)
    {
        if (row.ContainsKey(key))
        {
            return;
        }

        var normalized = (value ?? "").Trim();
        if (!string.IsNullOrWhiteSpace(normalized))
        {
            row[key] = normalized;
        }
    }

    private static void AddValue(IDictionary<string, object?> row, string key, ConfigApplyValue value)
    {
        if (!row.ContainsKey(key) && value.HasValue)
        {
            row[key] = value.RawValue;
        }
    }
}
