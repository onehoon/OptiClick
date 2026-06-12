namespace OptiClick.Infrastructure.Install.Config;

using OptiClick.Core.Install;

public interface IConfigProfileApplier
{
    ConfigProfileApplyResult Apply(ConfigProfileApplyContext context);
}

public sealed class ConfigProfileApplier : IConfigProfileApplier
{
    private readonly IniProfileEditor _iniProfileEditor;
    private readonly UnrealIniProfileEditor _unrealIniProfileEditor;
    private readonly XmlProfileEditor _xmlProfileEditor;
    private readonly JsonProfileEditor _jsonProfileEditor;
    private readonly RegistryProfileApplier _registryProfileApplier;

    public ConfigProfileApplier(
        IniProfileEditor iniProfileEditor,
        UnrealIniProfileEditor unrealIniProfileEditor,
        XmlProfileEditor xmlProfileEditor,
        JsonProfileEditor jsonProfileEditor,
        RegistryProfileApplier registryProfileApplier)
    {
        _iniProfileEditor = iniProfileEditor;
        _unrealIniProfileEditor = unrealIniProfileEditor;
        _xmlProfileEditor = xmlProfileEditor;
        _jsonProfileEditor = jsonProfileEditor;
        _registryProfileApplier = registryProfileApplier;
    }

    public ConfigProfileApplyResult Apply(ConfigProfileApplyContext context)
    {
        var summaries = new List<ConfigProfileApplySummary>
        {
            ExecuteBestEffort(() => ApplyGameIni(context), ConfigProfileNames.GameIniProfile),
            ExecuteBestEffort(() => ApplyGameUnrealIni(context), ConfigProfileNames.GameUnrealIniProfile),
            ExecuteBestEffort(() => ApplyGameXml(context), ConfigProfileNames.GameXmlProfile),
            ExecuteBestEffort(() => ApplyGameJson(context), ConfigProfileNames.GameJsonProfile),
            ExecuteBestEffort(() => ApplyEngineIni(context), ConfigProfileNames.EngineIniProfile),
            ExecuteBestEffort(() => ApplyRegistry(context), ConfigProfileNames.RegistryProfile)
        }.Select(NormalizeSummary).ToList();

        var applied = summaries.SelectMany(static summary => summary.Applied).ToArray();
        var skipped = summaries.SelectMany(static summary => summary.Skipped).ToArray();
        var errors = summaries.SelectMany(static summary => summary.Errors).ToArray();

        return new ConfigProfileApplyResult
        {
            Changed = summaries.Any(static summary => summary.Changed),
            Summaries = summaries,
            Applied = applied,
            Skipped = skipped,
            Errors = errors
        };
    }

    private ConfigProfileApplySummary ApplyGameIni(ConfigProfileApplyContext context)
    {
        return HasProfileRows(context.ProfileRows)
            ? _iniProfileEditor.ApplyGameIni(context.TargetPath, context.ProfileRows)
            : _iniProfileEditor.ApplyGameIni(context.TargetPath, context.GameData);
    }

    private ConfigProfileApplySummary ApplyEngineIni(ConfigProfileApplyContext context)
    {
        return HasProfileRows(context.ProfileRows)
            ? _iniProfileEditor.ApplyEngineIni(context.TargetPath, context.ProfileRows)
            : _iniProfileEditor.ApplyEngineIni(context.TargetPath, context.GameData);
    }

    private ConfigProfileApplySummary ApplyGameUnrealIni(ConfigProfileApplyContext context)
    {
        return HasProfileRows(context.ProfileRows)
            ? _unrealIniProfileEditor.Apply(context.TargetPath, context.ProfileRows)
            : _unrealIniProfileEditor.Apply(context.TargetPath, context.GameData);
    }

    private ConfigProfileApplySummary ApplyGameXml(ConfigProfileApplyContext context)
    {
        return HasProfileRows(context.ProfileRows)
            ? _xmlProfileEditor.Apply(context.TargetPath, context.ProfileRows)
            : _xmlProfileEditor.Apply(context.TargetPath, context.GameData);
    }

    private ConfigProfileApplySummary ApplyGameJson(ConfigProfileApplyContext context)
    {
        return HasProfileRows(context.ProfileRows)
            ? _jsonProfileEditor.Apply(context.TargetPath, context.ProfileRows)
            : _jsonProfileEditor.Apply(context.TargetPath, context.GameData);
    }

    private ConfigProfileApplySummary ApplyRegistry(ConfigProfileApplyContext context)
    {
        return HasProfileRows(context.ProfileRows)
            ? _registryProfileApplier.Apply(context.ProfileRows)
            : _registryProfileApplier.Apply(context.GameData);
    }

    private static bool HasProfileRows(ConfigApplyProfileRows? rows)
    {
        return rows?.HasAnyRows == true;
    }

    private static ConfigProfileApplySummary ExecuteBestEffort(
        Func<ConfigProfileApplySummary> action,
        string profileName)
    {
        try
        {
            return action();
        }
        catch (Exception ex)
        {
            return new ConfigProfileApplySummary
            {
                ProfileName = profileName,
                Completed = false,
                Changed = false,
                Errors = new[]
                {
                    new ConfigProfileError
                    {
                        ProfileName = profileName,
                        ReasonCode = ConfigErrorReasons.ApplyFailed,
                        Detail = ex.Message
                    }
                }
            };
        }
    }

    private static ConfigProfileApplySummary NormalizeSummary(ConfigProfileApplySummary summary)
    {
        return summary with
        {
            AppliedCount = summary.Applied.Count,
            SkippedCount = summary.Skipped.Count,
            ErrorCount = summary.Errors.Count
        };
    }
}

public sealed class NoOpRegistryWriter : IRegistryWriter
{
    public object? GetValue(string hiveName, string keyPath, string valueName)
    {
        _ = hiveName;
        _ = keyPath;
        _ = valueName;
        return null;
    }

    public void SetValue(string hiveName, string keyPath, string valueName, string valueTypeName, object value)
    {
        _ = hiveName;
        _ = keyPath;
        _ = valueName;
        _ = valueTypeName;
        _ = value;
    }
}
