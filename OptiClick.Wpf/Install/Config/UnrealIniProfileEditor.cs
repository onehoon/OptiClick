using System.IO;
using System.Text.RegularExpressions;

namespace OptiClick.Wpf.Install.Config;

public sealed class UnrealIniProfileEditor
{
    private static readonly Regex SectionPattern = new(@"^\s*\[([^\]]+)\]\s*(?:[;#].*)?$", RegexOptions.Compiled);
    private static readonly Regex KeyValuePattern = new(@"^(\s*)([^=;#\r\n]+?)(\s*)=(.*)$", RegexOptions.Compiled);
    private static readonly Regex TupleSelectorPattern =
        new(@"^(?<field>[A-Za-z_][A-Za-z0-9_]*)\[(?<quote>[""'])(?<entry>.*?)(\k<quote>)\]$", RegexOptions.Compiled | RegexOptions.Singleline);
    private static readonly Regex StructFieldPattern =
        new(@"^(?<prefix>\s*)(?<name>[A-Za-z_][A-Za-z0-9_]*)(?<separator>\s*=\s*)(?<value>.*?)(?<suffix>\s*)$", RegexOptions.Compiled | RegexOptions.Singleline);
    private static readonly Regex TupleEntryPattern =
        new(@"^(?<prefix>\s*\(\s*)(?<quote>[""'])(?<name>.*?)(\k<quote>)(?<separator>\s*,\s*)(?<value>.*?)(?<suffix>\s*\)\s*)$",
            RegexOptions.Compiled | RegexOptions.Singleline);

    private readonly IProfilePathResolver _pathResolver;

    public UnrealIniProfileEditor(IProfilePathResolver pathResolver)
    {
        _pathResolver = pathResolver;
    }

    public ConfigProfileApplySummary Apply(string targetPath, IReadOnlyDictionary<string, object?> gameData)
    {
        var profileName = ConfigProfileNames.GameUnrealIniProfile;
        var applied = new List<ConfigProfileAppliedRow>();
        var skipped = new List<ConfigProfileSkippedRow>();
        var errors = new List<ConfigProfileError>();
        var changedAny = false;

        var targets = CollectTargets(targetPath, gameData, skipped);
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
                        var changed = ApplyToFile(filePath, rows, fileApplied, fileSkipped);
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

    private Dictionary<string, List<UnrealIniRow>> CollectTargets(
        string targetPath,
        IReadOnlyDictionary<string, object?> gameData,
        List<ConfigProfileSkippedRow> skipped)
    {
        var grouped = new Dictionary<string, List<UnrealIniRow>>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in ConfigDataReader.ReadRows(gameData, ConfigProfileNames.GameUnrealIniProfile))
        {
            var profilePath = ConfigDataReader.ReadString(row, "path");
            var section = ConfigDataReader.ReadString(row, "section");
            var key = ConfigDataReader.ReadString(row, "key");
            var valuePath = ConfigDataReader.ReadString(row, "value_path");
            if (string.IsNullOrWhiteSpace(profilePath)
                || string.IsNullOrWhiteSpace(section)
                || string.IsNullOrWhiteSpace(key)
                || string.IsNullOrWhiteSpace(valuePath))
            {
                skipped.Add(new ConfigProfileSkippedRow
                {
                    ProfileName = ConfigProfileNames.GameUnrealIniProfile,
                    ReasonCode = ConfigSkipReasons.InvalidRow,
                    Detail = "path/section/key/value_path"
                });
                continue;
            }

            var resolvedPath = _pathResolver.Resolve(targetPath, profilePath, requireExisting: true);
            if (resolvedPath is null)
            {
                skipped.Add(new ConfigProfileSkippedRow
                {
                    ProfileName = ConfigProfileNames.GameUnrealIniProfile,
                    ReasonCode = ConfigSkipReasons.MissingTargetFile,
                    Detail = profilePath
                });
                continue;
            }

            if (!grouped.TryGetValue(resolvedPath, out var list))
            {
                list = new List<UnrealIniRow>();
                grouped[resolvedPath] = list;
            }

            list.Add(new UnrealIniRow
            {
                Section = section,
                Key = key,
                ValuePath = valuePath,
                Value = IniProfileEditor.NormalizeProfileScalar(
                    ConfigDataReader.ReadValue(row, "value"),
                    ConfigDataReader.ReadString(row, "value_type"))
            });
        }

        return grouped;
    }

    private static bool ApplyToFile(
        string filePath,
        IReadOnlyList<UnrealIniRow> rows,
        List<ConfigProfileAppliedRow> applied,
        List<ConfigProfileSkippedRow> skipped)
    {
        var read = IniTextCodec.ReadWithFallback(filePath);
        var lines = read.Text.SplitLinesKeepEndings().ToList();
        var changed = false;
        var currentSection = "";

        for (var index = 0; index < lines.Count; index++)
        {
            var (body, ending) = lines[index].SplitLineEnding();
            var stripped = body.Trim();
            if (string.IsNullOrWhiteSpace(stripped) || stripped.StartsWith(';') || stripped.StartsWith('#'))
            {
                continue;
            }

            var parsedBody = body.TrimStart('\uFEFF');
            var sectionMatch = SectionPattern.Match(parsedBody);
            if (sectionMatch.Success)
            {
                currentSection = Normalize(sectionMatch.Groups[1].Value);
                continue;
            }

            var keyMatch = KeyValuePattern.Match(parsedBody);
            if (!keyMatch.Success || string.IsNullOrWhiteSpace(currentSection))
            {
                continue;
            }

            var normalizedKey = Normalize(keyMatch.Groups[2].Value);
            var targetRows = rows.Where(row => Normalize(row.Section) == currentSection && Normalize(row.Key) == normalizedKey).ToArray();
            if (targetRows.Length == 0)
            {
                continue;
            }

            var oldRest = keyMatch.Groups[4].Value;
            var leadingWhitespaceLength = oldRest.Length - oldRest.TrimStart().Length;
            var leadingWhitespace = oldRest[..leadingWhitespaceLength];
            var oldValue = oldRest[leadingWhitespaceLength..];
            var commentIndex = FindCommentIndex(oldValue);
            var comment = commentIndex >= 0 ? oldValue[commentIndex..].TrimStart() : "";
            var rebuiltValue = commentIndex >= 0 ? oldValue[..commentIndex].TrimEnd() : oldValue;
            var lineChanged = false;

            foreach (var targetRow in targetRows)
            {
                var oldItemValue = ReadUnrealValuePath(rebuiltValue, targetRow.ValuePath);
                var updatedValue = ApplyUnrealValuePath(rebuiltValue, targetRow.ValuePath, targetRow.Value);
                if (updatedValue is null)
                {
                    skipped.Add(new ConfigProfileSkippedRow
                    {
                        ProfileName = ConfigProfileNames.GameUnrealIniProfile,
                        ReasonCode = ConfigSkipReasons.MissingPathTarget,
                        Detail = $"{targetRow.Section}:{targetRow.Key}:{targetRow.ValuePath}",
                        TargetPath = filePath,
                        TargetKey = $"{targetRow.Section}:{targetRow.Key}",
                        ValuePath = targetRow.ValuePath,
                        OldValue = oldItemValue ?? "<missing>",
                        NewValue = targetRow.Value
                    });
                    continue;
                }

                if (!string.Equals(updatedValue, rebuiltValue, StringComparison.Ordinal))
                {
                    rebuiltValue = updatedValue;
                    lineChanged = true;
                    applied.Add(new ConfigProfileAppliedRow
                    {
                        ProfileName = ConfigProfileNames.GameUnrealIniProfile,
                        TargetPath = filePath,
                        TargetKey = $"{targetRow.Section}:{targetRow.Key}",
                        ValuePath = targetRow.ValuePath,
                        OldValue = oldItemValue ?? "",
                        NewValue = targetRow.Value
                    });
                }
                else
                {
                    skipped.Add(new ConfigProfileSkippedRow
                    {
                        ProfileName = ConfigProfileNames.GameUnrealIniProfile,
                        ReasonCode = ConfigSkipReasons.Unchanged,
                        Detail = $"{targetRow.Section}:{targetRow.Key}:{targetRow.ValuePath}",
                        TargetPath = filePath,
                        TargetKey = $"{targetRow.Section}:{targetRow.Key}",
                        ValuePath = targetRow.ValuePath,
                        OldValue = oldItemValue ?? targetRow.Value,
                        NewValue = targetRow.Value
                    });
                }
            }

            if (!lineChanged)
            {
                continue;
            }

            var rebuiltRest = string.IsNullOrWhiteSpace(comment)
                ? $"{leadingWhitespace}{rebuiltValue}"
                : $"{leadingWhitespace}{rebuiltValue} {comment}";
            lines[index] = $"{keyMatch.Groups[1].Value}{keyMatch.Groups[2].Value}{keyMatch.Groups[3].Value}={rebuiltRest}{ending}";
            changed = true;
        }

        if (changed)
        {
            IniTextCodec.WriteWithEncoding(filePath, string.Concat(lines), read.Encoding);
        }

        return changed;
    }

    private static int FindCommentIndex(string text)
    {
        for (var index = 0; index < text.Length; index++)
        {
            if (text[index] == ';' || text[index] == '#')
            {
                return index;
            }
        }

        return -1;
    }

    private static string? ApplyUnrealValuePath(string valueText, string valuePath, string newValue)
    {
        var normalizedPath = (valuePath ?? "").Trim();
        if (string.IsNullOrWhiteSpace(normalizedPath))
        {
            return null;
        }

        var tupleSelectorMatch = TupleSelectorPattern.Match(normalizedPath);
        if (tupleSelectorMatch.Success)
        {
            return ReplaceTupleMapValue(
                valueText,
                tupleSelectorMatch.Groups["field"].Value,
                tupleSelectorMatch.Groups["entry"].Value,
                newValue);
        }

        if (!Regex.IsMatch(normalizedPath, "^[A-Za-z_][A-Za-z0-9_]*$"))
        {
            return null;
        }

        return ReplaceStructField(valueText, normalizedPath, newValue);
    }

    private static string? ReadUnrealValuePath(string valueText, string valuePath)
    {
        var normalizedPath = (valuePath ?? "").Trim();
        if (string.IsNullOrWhiteSpace(normalizedPath))
        {
            return null;
        }

        var tupleSelectorMatch = TupleSelectorPattern.Match(normalizedPath);
        if (tupleSelectorMatch.Success)
        {
            return ReadTupleMapValue(
                valueText,
                tupleSelectorMatch.Groups["field"].Value,
                tupleSelectorMatch.Groups["entry"].Value);
        }

        if (!Regex.IsMatch(normalizedPath, "^[A-Za-z_][A-Za-z0-9_]*$"))
        {
            return null;
        }

        return ReadStructField(valueText, normalizedPath);
    }

    private static string? ReadStructField(string valueText, string fieldName)
    {
        if (!TryUnwrapParenthesized(valueText, out _, out var body, out _))
        {
            return null;
        }

        var parts = SplitTopLevelCommaParts(body);
        foreach (var part in parts)
        {
            var match = StructFieldPattern.Match(part);
            if (match.Success && string.Equals(match.Groups["name"].Value, fieldName, StringComparison.Ordinal))
            {
                return match.Groups["value"].Value.Trim();
            }
        }

        return null;
    }

    private static string? ReadTupleMapValue(string valueText, string tupleField, string entryName)
    {
        if (!TryUnwrapParenthesized(valueText, out _, out var body, out _))
        {
            return null;
        }

        var topLevelParts = SplitTopLevelCommaParts(body);
        foreach (var part in topLevelParts)
        {
            var fieldMatch = StructFieldPattern.Match(part);
            if (!fieldMatch.Success || !string.Equals(fieldMatch.Groups["name"].Value, tupleField, StringComparison.Ordinal))
            {
                continue;
            }

            var tupleValue = fieldMatch.Groups["value"].Value;
            if (!TryUnwrapParenthesized(tupleValue, out _, out var tupleBody, out _))
            {
                return null;
            }

            var tupleParts = SplitTopLevelCommaParts(tupleBody);
            foreach (var tuplePart in tupleParts)
            {
                var tupleMatch = TupleEntryPattern.Match(tuplePart);
                if (tupleMatch.Success && string.Equals(tupleMatch.Groups["name"].Value, entryName, StringComparison.Ordinal))
                {
                    return tupleMatch.Groups["value"].Value.Trim();
                }
            }

            return null;
        }

        return null;
    }

    private static string? ReplaceStructField(string valueText, string fieldName, string newValue)
    {
        if (!TryUnwrapParenthesized(valueText, out var leading, out var body, out var trailing))
        {
            return null;
        }

        var parts = SplitTopLevelCommaParts(body);
        for (var index = 0; index < parts.Count; index++)
        {
            var match = StructFieldPattern.Match(parts[index]);
            if (!match.Success || !string.Equals(match.Groups["name"].Value, fieldName, StringComparison.Ordinal))
            {
                continue;
            }

            parts[index] = $"{match.Groups["prefix"].Value}{match.Groups["name"].Value}{match.Groups["separator"].Value}{newValue}{match.Groups["suffix"].Value}";
            return $"{leading}({string.Join(",", parts)}){trailing}";
        }

        return null;
    }

    private static string? ReplaceTupleMapValue(string valueText, string tupleField, string entryName, string newValue)
    {
        if (!TryUnwrapParenthesized(valueText, out var leading, out var body, out var trailing))
        {
            return null;
        }

        var topLevelParts = SplitTopLevelCommaParts(body);
        for (var index = 0; index < topLevelParts.Count; index++)
        {
            var fieldMatch = StructFieldPattern.Match(topLevelParts[index]);
            if (!fieldMatch.Success || !string.Equals(fieldMatch.Groups["name"].Value, tupleField, StringComparison.Ordinal))
            {
                continue;
            }

            var tupleValue = fieldMatch.Groups["value"].Value;
            if (!TryUnwrapParenthesized(tupleValue, out var tupleLeading, out var tupleBody, out var tupleTrailing))
            {
                return null;
            }

            var tupleParts = SplitTopLevelCommaParts(tupleBody);
            for (var tupleIndex = 0; tupleIndex < tupleParts.Count; tupleIndex++)
            {
                var tupleMatch = TupleEntryPattern.Match(tupleParts[tupleIndex]);
                if (!tupleMatch.Success || !string.Equals(tupleMatch.Groups["name"].Value, entryName, StringComparison.Ordinal))
                {
                    continue;
                }

                tupleParts[tupleIndex] = $"{tupleMatch.Groups["prefix"].Value}{tupleMatch.Groups["quote"].Value}{tupleMatch.Groups["name"].Value}{tupleMatch.Groups["quote"].Value}{tupleMatch.Groups["separator"].Value}{newValue}{tupleMatch.Groups["suffix"].Value}";
                var rebuiltTupleValue = $"{tupleLeading}({string.Join(",", tupleParts)}){tupleTrailing}";
                topLevelParts[index] = $"{fieldMatch.Groups["prefix"].Value}{fieldMatch.Groups["name"].Value}{fieldMatch.Groups["separator"].Value}{rebuiltTupleValue}{fieldMatch.Groups["suffix"].Value}";
                return $"{leading}({string.Join(",", topLevelParts)}){trailing}";
            }

            return null;
        }

        return null;
    }

    private static bool TryUnwrapParenthesized(string text, out string leadingWhitespace, out string body, out string trailingWhitespace)
    {
        leadingWhitespace = "";
        body = "";
        trailingWhitespace = "";

        var raw = text ?? "";
        var stripped = raw.Trim();
        if (stripped.Length < 2 || stripped[0] != '(' || stripped[^1] != ')')
        {
            return false;
        }

        var leadingLength = raw.Length - raw.TrimStart().Length;
        var trailingLength = raw.Length - raw.TrimEnd().Length;
        leadingWhitespace = leadingLength > 0 ? raw[..leadingLength] : "";
        trailingWhitespace = trailingLength > 0 ? raw[^trailingLength..] : "";
        body = stripped[1..^1];
        return true;
    }

    private static List<string> SplitTopLevelCommaParts(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return new List<string> { "" };
        }

        var parts = new List<string>();
        var start = 0;
        var depth = 0;
        var quote = '\0';
        var escaping = false;

        for (var index = 0; index < text.Length; index++)
        {
            var ch = text[index];
            if (quote != '\0')
            {
                if (escaping)
                {
                    escaping = false;
                    continue;
                }

                if (ch == '\\')
                {
                    escaping = true;
                    continue;
                }

                if (ch == quote)
                {
                    quote = '\0';
                }

                continue;
            }

            if (ch is '"' or '\'')
            {
                quote = ch;
                continue;
            }

            if (ch == '(')
            {
                depth++;
                continue;
            }

            if (ch == ')')
            {
                depth--;
                continue;
            }

            if (ch == ',' && depth == 0)
            {
                parts.Add(text[start..index]);
                start = index + 1;
            }
        }

        parts.Add(text[start..]);
        return parts;
    }

    private static string Normalize(string value)
    {
        return new string((value ?? "")
            .Where(static ch => !char.IsWhiteSpace(ch))
            .ToArray())
            .ToLowerInvariant();
    }

    private sealed record UnrealIniRow
    {
        public string Section { get; init; } = "";
        public string Key { get; init; } = "";
        public string ValuePath { get; init; } = "";
        public string Value { get; init; } = "";
    }
}
