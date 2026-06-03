namespace OptiClick.Wpf.Install.Config;

internal sealed class IniProfileTargetCollector
{
    private readonly IProfilePathResolver _pathResolver;

    public IniProfileTargetCollector(IProfilePathResolver pathResolver)
    {
        _pathResolver = pathResolver ?? throw new ArgumentNullException(nameof(pathResolver));
    }

    public Dictionary<string, Dictionary<string, Dictionary<string, string>>> CollectGameIniTargets(
        string targetPath,
        IReadOnlyDictionary<string, object?> gameData,
        List<ConfigProfileSkippedRow> skipped)
    {
        var grouped = new Dictionary<string, Dictionary<string, Dictionary<string, string>>>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in ConfigDataReader.ReadRows(gameData, ConfigProfileNames.GameIniProfile))
        {
            var profilePath = ConfigDataReader.ReadString(row, "path");
            var section = ConfigDataReader.ReadString(row, "section");
            var key = ConfigDataReader.ReadString(row, "key");
            if (string.IsNullOrWhiteSpace(profilePath) || string.IsNullOrWhiteSpace(section) || string.IsNullOrWhiteSpace(key))
            {
                skipped.Add(new ConfigProfileSkippedRow
                {
                    ProfileName = ConfigProfileNames.GameIniProfile,
                    ReasonCode = ConfigSkipReasons.InvalidRow,
                    Detail = "path/section/key"
                });
                continue;
            }

            var resolvedPath = _pathResolver.Resolve(targetPath, profilePath, requireExisting: true);
            if (resolvedPath is null)
            {
                skipped.Add(new ConfigProfileSkippedRow
                {
                    ProfileName = ConfigProfileNames.GameIniProfile,
                    ReasonCode = ConfigSkipReasons.MissingTargetFile,
                    Detail = profilePath
                });
                continue;
            }

            if (!grouped.TryGetValue(resolvedPath, out var sections))
            {
                sections = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
                grouped[resolvedPath] = sections;
            }

            if (!sections.TryGetValue(section, out var keys))
            {
                keys = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                sections[section] = keys;
            }

            keys[key] = IniProfileEditor.NormalizeProfileScalar(ConfigDataReader.ReadValue(row, "value"), "");
        }

        return grouped;
    }

    public Dictionary<string, Dictionary<string, Dictionary<string, string>>> CollectEngineIniTargets(
        string targetPath,
        IReadOnlyDictionary<string, object?> gameData,
        List<ConfigProfileSkippedRow> skipped)
    {
        var grouped = new Dictionary<string, Dictionary<string, Dictionary<string, string>>>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in ConfigDataReader.ReadRows(gameData, ConfigProfileNames.EngineIniProfile))
        {
            var profilePath = ConfigDataReader.ReadString(row, "path");
            var section = ConfigDataReader.ReadString(row, "section");
            var key = ConfigDataReader.ReadString(row, "key");
            if (string.IsNullOrWhiteSpace(profilePath) || string.IsNullOrWhiteSpace(section) || string.IsNullOrWhiteSpace(key))
            {
                skipped.Add(new ConfigProfileSkippedRow
                {
                    ProfileName = ConfigProfileNames.EngineIniProfile,
                    ReasonCode = ConfigSkipReasons.InvalidRow,
                    Detail = "path/section/key"
                });
                continue;
            }

            var resolvedPath = _pathResolver.Resolve(targetPath, profilePath, requireExisting: false);
            if (resolvedPath is null)
            {
                skipped.Add(new ConfigProfileSkippedRow
                {
                    ProfileName = ConfigProfileNames.EngineIniProfile,
                    ReasonCode = ConfigSkipReasons.MissingTargetDirectory,
                    Detail = profilePath
                });
                continue;
            }

            if (!grouped.TryGetValue(resolvedPath, out var sections))
            {
                sections = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
                grouped[resolvedPath] = sections;
            }

            if (!sections.TryGetValue(section, out var keys))
            {
                keys = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                sections[section] = keys;
            }

            keys[key] = IniProfileEditor.NormalizeProfileScalar(
                ConfigDataReader.ReadValue(row, "value"),
                ConfigDataReader.ReadString(row, "value_type"));
        }

        return grouped;
    }
}
