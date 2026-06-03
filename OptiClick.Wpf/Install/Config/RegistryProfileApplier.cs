namespace OptiClick.Wpf.Install.Config;

public interface IRegistryWriter
{
    object? GetValue(string hiveName, string keyPath, string valueName);
    void SetValue(string hiveName, string keyPath, string valueName, string valueTypeName, object value);
}

public sealed class RegistryProfileApplier
{
    private static readonly IReadOnlyDictionary<string, string> RegistryHiveMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["hkcu"] = "HKEY_CURRENT_USER",
        ["hkey_current_user"] = "HKEY_CURRENT_USER",
        ["hklm"] = "HKEY_LOCAL_MACHINE",
        ["hkey_local_machine"] = "HKEY_LOCAL_MACHINE",
        ["hkcr"] = "HKEY_CLASSES_ROOT",
        ["hkey_classes_root"] = "HKEY_CLASSES_ROOT",
        ["hku"] = "HKEY_USERS",
        ["hkey_users"] = "HKEY_USERS"
    };

    private static readonly IReadOnlyDictionary<string, string> RegistryTypeMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["reg_sz"] = "REG_SZ",
        ["reg_expand_sz"] = "REG_EXPAND_SZ",
        ["reg_multi_sz"] = "REG_MULTI_SZ",
        ["reg_dword"] = "REG_DWORD",
        ["reg_qword"] = "REG_QWORD"
    };

    private readonly IRegistryWriter _registryWriter;

    public RegistryProfileApplier(IRegistryWriter registryWriter)
    {
        _registryWriter = registryWriter;
    }

    public ConfigProfileApplySummary Apply(IReadOnlyDictionary<string, object?> gameData)
    {
        var profileName = ConfigProfileNames.RegistryProfile;
        var applied = new List<ConfigProfileAppliedRow>();
        var skipped = new List<ConfigProfileSkippedRow>();
        var errors = new List<ConfigProfileError>();

        var rows = DedupeRows(ConfigDataReader.ReadRows(gameData, ConfigProfileNames.RegistryProfile));
        foreach (var row in rows)
        {
            var hive = ConfigDataReader.ReadString(row, "hive").ToLowerInvariant();
            var keyPath = ConfigDataReader.ReadString(row, "key_path");
            var valueName = ConfigDataReader.ReadString(row, "value_name");
            var valueType = ConfigDataReader.ReadString(row, "value_type").ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(hive)
                || string.IsNullOrWhiteSpace(keyPath)
                || string.IsNullOrWhiteSpace(valueName)
                || string.IsNullOrWhiteSpace(valueType))
            {
                skipped.Add(new ConfigProfileSkippedRow
                {
                    ProfileName = profileName,
                    ReasonCode = ConfigSkipReasons.InvalidRow,
                    Detail = "hive/key_path/value_name/value_type",
                    TargetPath = $"{hive}\\{keyPath}",
                    TargetKey = valueName
                });
                continue;
            }

            if (!RegistryHiveMap.TryGetValue(hive, out var resolvedHive)
                || !RegistryTypeMap.TryGetValue(valueType, out var resolvedType))
            {
                skipped.Add(new ConfigProfileSkippedRow
                {
                    ProfileName = profileName,
                    ReasonCode = ConfigSkipReasons.UnsupportedHiveOrType,
                    Detail = $"{hive}:{valueType}",
                    TargetPath = $"{hive}\\{keyPath}",
                    TargetKey = valueName
                });
                continue;
            }

            object coercedValue;
            try
            {
                coercedValue = CoerceRegistryValue(ConfigDataReader.ReadValue(row, "value"), valueType);
            }
            catch (Exception ex)
            {
                errors.Add(new ConfigProfileError
                {
                    ProfileName = profileName,
                    ReasonCode = ConfigErrorReasons.ApplyFailed,
                    Detail = $"{keyPath}:{valueName} ({ex.Message})",
                    TargetPath = $"{resolvedHive}\\{keyPath}",
                    TargetKey = valueName
                });
                continue;
            }

            object? oldValue = null;
            var oldValueRead = false;
            try
            {
                oldValue = _registryWriter.GetValue(resolvedHive, keyPath, valueName);
                oldValueRead = true;
                if (RegistryValuesEqual(oldValue, coercedValue, valueType))
                {
                    skipped.Add(new ConfigProfileSkippedRow
                    {
                        ProfileName = profileName,
                        ReasonCode = ConfigSkipReasons.Unchanged,
                        Detail = $"{keyPath}:{valueName}",
                        TargetPath = $"{resolvedHive}\\{keyPath}",
                        TargetKey = valueName,
                        OldValue = FormatRegistryValue(oldValue, missingFallback: "<missing>"),
                        NewValue = FormatRegistryValue(coercedValue)
                    });
                    continue;
                }

                _registryWriter.SetValue(resolvedHive, keyPath, valueName, resolvedType, coercedValue);
                applied.Add(new ConfigProfileAppliedRow
                {
                    ProfileName = profileName,
                    TargetPath = $"{resolvedHive}\\{keyPath}",
                    TargetKey = valueName,
                    OldValue = FormatRegistryValue(oldValue, missingFallback: "<missing>"),
                    NewValue = FormatRegistryValue(coercedValue)
                });
            }
            catch (Exception ex)
            {
                errors.Add(new ConfigProfileError
                {
                    ProfileName = profileName,
                    ReasonCode = ConfigErrorReasons.ApplyFailed,
                    Detail = $"{keyPath}:{valueName} ({ex.Message})",
                    TargetPath = $"{resolvedHive}\\{keyPath}",
                    TargetKey = valueName,
                    OldValue = oldValueRead ? FormatRegistryValue(oldValue, missingFallback: "<missing>") : "<unread>",
                    NewValue = FormatRegistryValue(coercedValue)
                });
            }
        }

        return new ConfigProfileApplySummary
        {
            ProfileName = profileName,
            Completed = true,
            Changed = applied.Count > 0,
            Applied = applied,
            Skipped = skipped,
            Errors = errors
        };
    }

    public static object CoerceRegistryValue(object? value, string valueType)
    {
        var normalizedType = (valueType ?? "").Trim().ToLowerInvariant();
        if (normalizedType == "reg_dword")
        {
            return Convert.ToInt32(value);
        }

        if (normalizedType == "reg_qword")
        {
            return Convert.ToInt64(value);
        }

        if (normalizedType == "reg_multi_sz")
        {
            if (value is IEnumerable<object?> objectValues && value is not string)
            {
                return objectValues.Select(static item => item?.ToString() ?? "").ToArray();
            }

            return (value?.ToString() ?? "")
                .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }

        return value?.ToString() ?? "";
    }

    private static string FormatRegistryValue(object? value, string missingFallback = "")
    {
        if (value is null)
        {
            return missingFallback;
        }

        if (value is string[] stringArray)
        {
            return string.Join("|", stringArray);
        }

        if (value is IEnumerable<object?> objectValues && value is not string)
        {
            return string.Join("|", objectValues.Select(static item => item?.ToString() ?? ""));
        }

        return value.ToString() ?? "";
    }

    private static bool RegistryValuesEqual(object? oldValue, object newValue, string valueType)
    {
        if (oldValue is null)
        {
            return false;
        }

        var normalizedType = (valueType ?? "").Trim().ToLowerInvariant();
        try
        {
            return normalizedType switch
            {
                "reg_dword" => Convert.ToInt32(oldValue) == Convert.ToInt32(newValue),
                "reg_qword" => Convert.ToInt64(oldValue) == Convert.ToInt64(newValue),
                "reg_multi_sz" => RegistryMultiStringValuesEqual(oldValue, newValue),
                _ => string.Equals(oldValue.ToString() ?? "", newValue.ToString() ?? "", StringComparison.Ordinal)
            };
        }
        catch
        {
            return false;
        }
    }

    private static bool RegistryMultiStringValuesEqual(object oldValue, object newValue)
    {
        var oldItems = NormalizeRegistryMultiString(oldValue);
        var newItems = NormalizeRegistryMultiString(newValue);
        return oldItems.SequenceEqual(newItems, StringComparer.Ordinal);
    }

    private static string[] NormalizeRegistryMultiString(object value)
    {
        if (value is string[] stringArray)
        {
            return stringArray;
        }

        if (value is IEnumerable<object?> objectValues && value is not string)
        {
            return objectValues.Select(static item => item?.ToString() ?? "").ToArray();
        }

        return (value.ToString() ?? "")
            .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static IReadOnlyList<IReadOnlyDictionary<string, object?>> DedupeRows(IReadOnlyList<IReadOnlyDictionary<string, object?>> rows)
    {
        var deduped = new Dictionary<string, IReadOnlyDictionary<string, object?>>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows)
        {
            var hive = ConfigDataReader.ReadString(row, "hive").ToLowerInvariant();
            var keyPath = ConfigDataReader.ReadString(row, "key_path");
            var valueName = ConfigDataReader.ReadString(row, "value_name");
            if (string.IsNullOrWhiteSpace(hive) || string.IsNullOrWhiteSpace(keyPath) || string.IsNullOrWhiteSpace(valueName))
            {
                continue;
            }

            var key = $"{hive}|{keyPath}|{valueName}";
            deduped[key] = row;
        }

        return deduped.Values.ToArray();
    }
}

