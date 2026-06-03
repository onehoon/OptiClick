namespace OptiClick.Wpf.Install.Config;

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
            ExecuteBestEffort(() => _iniProfileEditor.ApplyGameIni(context.TargetPath, context.GameData), ConfigProfileNames.GameIniProfile),
            ExecuteBestEffort(() => _unrealIniProfileEditor.Apply(context.TargetPath, context.GameData), ConfigProfileNames.GameUnrealIniProfile),
            ExecuteBestEffort(() => _xmlProfileEditor.Apply(context.TargetPath, context.GameData), ConfigProfileNames.GameXmlProfile),
            ExecuteBestEffort(() => _jsonProfileEditor.Apply(context.TargetPath, context.GameData), ConfigProfileNames.GameJsonProfile),
            ExecuteBestEffort(() => _iniProfileEditor.ApplyEngineIni(context.TargetPath, context.GameData), ConfigProfileNames.EngineIniProfile),
            ExecuteBestEffort(() => _registryProfileApplier.Apply(context.GameData), ConfigProfileNames.RegistryProfile)
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

