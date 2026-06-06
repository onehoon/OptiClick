using System.IO;

namespace OptiClick.Wpf.Install.Config;

public sealed class IniProfileEditor
{
    private readonly IniProfileTargetCollector _targetCollector;

    public IniProfileEditor(IProfilePathResolver pathResolver)
    {
        ArgumentNullException.ThrowIfNull(pathResolver);
        _targetCollector = new IniProfileTargetCollector(pathResolver);
    }

    public ConfigProfileApplySummary ApplyGameIni(string targetPath, IReadOnlyDictionary<string, object?> gameData)
    {
        var profileName = ConfigProfileNames.GameIniProfile;
        var applied = new List<ConfigProfileAppliedRow>();
        var skipped = new List<ConfigProfileSkippedRow>();
        var errors = new List<ConfigProfileError>();
        var changedAny = false;

        var groupedTargets = _targetCollector.CollectGameIniTargets(targetPath, gameData, skipped);
        foreach (var (filePath, sectionMap) in groupedTargets)
        {
            var fileApplied = new List<ConfigProfileAppliedRow>();
            var fileSkipped = new List<ConfigProfileSkippedRow>();
            try
            {
                OptionalFileEditRunner.ApplyExistingFileSettings(
                    filePath,
                    () =>
                    {
                        var changed = IniProfileDocumentEditor.ApplyEntries(
                            filePath,
                            sectionMap,
                            allowAddKey: true,
                            allowAddSection: false,
                            allowEditExisting: true,
                            createMissingFile: false,
                            trackApplied: fileApplied,
                            trackSkipped: fileSkipped,
                            profileName: profileName);
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
            Completed = true,
            Applied = applied,
            Skipped = skipped,
            Errors = errors
        };
    }

    public ConfigProfileApplySummary ApplyEngineIni(string targetPath, IReadOnlyDictionary<string, object?> gameData)
    {
        var profileName = ConfigProfileNames.EngineIniProfile;
        var applied = new List<ConfigProfileAppliedRow>();
        var skipped = new List<ConfigProfileSkippedRow>();
        var errors = new List<ConfigProfileError>();
        var changedAny = false;

        var groupedTargets = _targetCollector.CollectEngineIniTargets(targetPath, gameData, skipped);
        foreach (var (filePath, sectionMap) in groupedTargets)
        {
            var fileApplied = new List<ConfigProfileAppliedRow>();
            var fileSkipped = new List<ConfigProfileSkippedRow>();
            try
            {
                if (!File.Exists(filePath))
                {
                    var parentDir = Path.GetDirectoryName(filePath);
                    if (string.IsNullOrWhiteSpace(parentDir) || !Directory.Exists(parentDir))
                    {
                        skipped.Add(new ConfigProfileSkippedRow
                        {
                            ProfileName = profileName,
                            ReasonCode = ConfigSkipReasons.MissingTargetDirectory,
                            Detail = filePath
                        });
                        continue;
                    }

                    File.WriteAllText(filePath, string.Empty);
                }

                OptionalFileEditRunner.ApplyExistingFileSettings(
                    filePath,
                    () =>
                    {
                        var changed = IniProfileDocumentEditor.ApplyEntries(
                            filePath,
                            sectionMap,
                            allowAddKey: true,
                            allowAddSection: true,
                            allowEditExisting: true,
                            createMissingFile: true,
                            trackApplied: fileApplied,
                            trackSkipped: fileSkipped,
                            profileName: profileName);
                        changedAny = changedAny || changed;
                    },
                    restoreOriginalReadonly: false);
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
            finally
            {
                if (File.Exists(filePath))
                {
                    var attributes = File.GetAttributes(filePath);
                    File.SetAttributes(filePath, attributes | FileAttributes.ReadOnly);
                }
            }
        }

        return new ConfigProfileApplySummary
        {
            ProfileName = profileName,
            Changed = changedAny,
            Completed = true,
            Applied = applied,
            Skipped = skipped,
            Errors = errors
        };
    }

    public ConfigProfileApplySummary ApplyOptiScalerIniBase(
        string targetPath,
        string relativePath,
        IReadOnlyDictionary<string, string> iniSettings,
        string profileName = "optiscaler_ini_base")
    {
        var normalizedProfileName = string.IsNullOrWhiteSpace(profileName)
            ? "optiscaler_ini_base"
            : profileName.Trim();
        var applied = new List<ConfigProfileAppliedRow>();
        var skipped = new List<ConfigProfileSkippedRow>();
        var errors = new List<ConfigProfileError>();

        if (iniSettings is null || iniSettings.Count == 0)
        {
            skipped.Add(new ConfigProfileSkippedRow
            {
                ProfileName = normalizedProfileName,
                ReasonCode = ConfigSkipReasons.Unchanged,
                Detail = "no_ini_settings"
            });
            return new ConfigProfileApplySummary
            {
                ProfileName = normalizedProfileName,
                Changed = false,
                Completed = true,
                Applied = applied,
                Skipped = skipped,
                Errors = errors
            };
        }

        var normalizedRelativePath = string.IsNullOrWhiteSpace(relativePath) ? "OptiScaler.ini" : relativePath.Trim();
        var absolutePath = Path.Combine(targetPath, normalizedRelativePath);
        if (!File.Exists(absolutePath))
        {
            skipped.Add(new ConfigProfileSkippedRow
            {
                ProfileName = normalizedProfileName,
                ReasonCode = ConfigSkipReasons.TargetFileNotFound,
                Detail = normalizedRelativePath
            });

            return new ConfigProfileApplySummary
            {
                ProfileName = normalizedProfileName,
                Changed = false,
                Completed = true,
                Applied = applied,
                Skipped = skipped,
                Errors = errors
            };
        }

        var sectionMap = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var (compositeKey, value) in iniSettings)
        {
            var normalizedKey = (compositeKey ?? "").Trim();
            if (string.IsNullOrWhiteSpace(normalizedKey))
            {
                skipped.Add(new ConfigProfileSkippedRow
                {
                    ProfileName = normalizedProfileName,
                    ReasonCode = ConfigSkipReasons.MissingRequiredField,
                    Detail = "section:key"
                });
                continue;
            }

            var separatorIndex = normalizedKey.IndexOf(':');
            if (separatorIndex <= 0 || separatorIndex >= normalizedKey.Length - 1)
            {
                skipped.Add(new ConfigProfileSkippedRow
                {
                    ProfileName = normalizedProfileName,
                    ReasonCode = ConfigSkipReasons.MissingRequiredField,
                    Detail = normalizedKey
                });
                continue;
            }

            var section = normalizedKey[..separatorIndex].Trim();
            var key = normalizedKey[(separatorIndex + 1)..].Trim();
            if (string.IsNullOrWhiteSpace(section) || string.IsNullOrWhiteSpace(key))
            {
                skipped.Add(new ConfigProfileSkippedRow
                {
                    ProfileName = normalizedProfileName,
                    ReasonCode = ConfigSkipReasons.MissingRequiredField,
                    Detail = normalizedKey
                });
                continue;
            }

            if (!sectionMap.TryGetValue(section, out var keys))
            {
                keys = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                sectionMap[section] = keys;
            }

            keys[key] = value ?? "";
        }

        if (sectionMap.Count == 0)
        {
            return new ConfigProfileApplySummary
            {
                ProfileName = normalizedProfileName,
                Changed = false,
                Completed = true,
                Applied = applied,
                Skipped = skipped,
                Errors = errors
            };
        }

        var changedAny = false;
        var fileApplied = new List<ConfigProfileAppliedRow>();
        var fileSkipped = new List<ConfigProfileSkippedRow>();
        try
        {
            OptionalFileEditRunner.ApplyExistingFileSettings(
                absolutePath,
                () =>
                {
                    var changed = IniProfileDocumentEditor.ApplyEntries(
                        absolutePath,
                        sectionMap,
                        allowAddKey: true,
                        allowAddSection: true,
                        allowEditExisting: true,
                        createMissingFile: false,
                        trackApplied: fileApplied,
                        trackSkipped: fileSkipped,
                        profileName: normalizedProfileName);
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
                ProfileName = normalizedProfileName,
                ReasonCode = ConfigErrorReasons.ApplyException,
                Detail = ex.Message,
                TargetPath = absolutePath
            });
        }

        return new ConfigProfileApplySummary
        {
            ProfileName = normalizedProfileName,
            Changed = changedAny,
            Completed = true,
            Applied = applied,
            Skipped = skipped,
            Errors = errors
        };
    }

    public static string NormalizeProfileScalar(object? value, string valueType)
    {
        var normalizedType = (valueType ?? "").Trim().ToLowerInvariant();
        try
        {
            if (normalizedType is "bool" or "boolean" || value is bool)
            {
                return Convert.ToBoolean(value) ? "true" : "false";
            }

            if (normalizedType is "int" or "integer")
            {
                return Convert.ToInt32(value).ToString();
            }

            if (normalizedType is "float" or "double")
            {
                return Convert.ToDouble(value).ToString(System.Globalization.CultureInfo.InvariantCulture);
            }
        }
        catch
        {
            // Fall back to raw string conversion.
        }

        return value?.ToString() ?? "";
    }
}
