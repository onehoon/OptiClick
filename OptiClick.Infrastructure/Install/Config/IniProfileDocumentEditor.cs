using System.IO;
using System.Text.RegularExpressions;

namespace OptiClick.Infrastructure.Install.Config;

public static class IniProfileDocumentEditor
{
    private static readonly Regex SectionPattern = new(@"^\s*\[([^\]]+)\]\s*(?:[;#].*)?$", RegexOptions.Compiled);
    private static readonly Regex KeyValuePattern = new(@"^(\s*)([^=;#\r\n]+?)(\s*)=(.*)$", RegexOptions.Compiled);
    private static readonly Regex KeyColonPattern = new(@"^(\s*)([^:\r\n]+?)(\s*):(.*)$", RegexOptions.Compiled);

    public static bool ApplyEntries(
        string filePath,
        IReadOnlyDictionary<string, Dictionary<string, string>> sectionMap,
        bool allowAddKey,
        bool allowAddSection,
        bool allowEditExisting,
        bool createMissingFile,
        List<ConfigProfileAppliedRow> trackApplied,
        List<ConfigProfileSkippedRow>? trackSkipped = null,
        string profileName = ConfigProfileNames.GameIniProfile)
    {
        if (!File.Exists(filePath))
        {
            if (!createMissingFile)
            {
                trackSkipped?.Add(new ConfigProfileSkippedRow
                {
                    ProfileName = profileName,
                    ReasonCode = ConfigSkipReasons.TargetFileNotFound,
                    Detail = filePath,
                    TargetPath = filePath
                });
                return false;
            }

            File.WriteAllText(filePath, string.Empty);
        }

        var read = IniTextCodec.ReadWithFallback(filePath);
        var preferredNewline = IniTextCodec.DetectPreferredNewLine(read.Text);
        var lines = read.Text.SplitLinesKeepEndings().ToList();
        var changed = false;

        foreach (var (sectionName, keyValues) in sectionMap)
        {
            var sectionRange = FindSectionRange(lines, sectionName);
            if (sectionRange is null)
            {
                if (!allowAddSection)
                {
                    foreach (var (key, value) in keyValues)
                    {
                        trackSkipped?.Add(BuildSkipped(
                            profileName,
                            ConfigSkipReasons.MissingPathTarget,
                            filePath,
                            sectionName,
                            key,
                            MissingValue(),
                            value));
                    }

                    continue;
                }

                if (lines.Count > 0 && !lines[^1].EndsWithAnyNewLine())
                {
                    lines[^1] = lines[^1] + preferredNewline;
                }

                lines.Add($"[{sectionName}]{preferredNewline}");
                sectionRange = (lines.Count - 1, lines.Count);
                changed = true;
            }

            foreach (var (key, value) in keyValues)
            {
                var keyLineIndex = FindKeyLine(lines, sectionRange.Value.StartInclusive, sectionRange.Value.EndExclusive, key);
                if (keyLineIndex >= 0)
                {
                    if (!allowEditExisting)
                    {
                        var oldValue = ExtractIniValueForLog(lines[keyLineIndex], allowColon: true);
                        trackSkipped?.Add(BuildSkipped(
                            profileName,
                            ConfigSkipReasons.UnsupportedOperation,
                            filePath,
                            sectionName,
                            key,
                            oldValue,
                            value));
                        continue;
                    }

                    var oldValueForLog = ExtractIniValueForLog(lines[keyLineIndex], allowColon: true);
                    var updated = ReplaceKeyValueLine(lines[keyLineIndex], value, allowColon: true, currentSection: sectionName);
                    if (!string.Equals(lines[keyLineIndex], updated, StringComparison.Ordinal))
                    {
                        lines[keyLineIndex] = updated;
                        changed = true;
                        trackApplied.Add(BuildApplied(
                            profileName,
                            filePath,
                            sectionName,
                            key,
                            oldValueForLog,
                            value));
                    }
                    else
                    {
                        trackSkipped?.Add(BuildSkipped(
                            profileName,
                            string.Equals(oldValueForLog, value, StringComparison.Ordinal)
                                ? ConfigSkipReasons.Unchanged
                                : ConfigSkipReasons.UnsupportedOperation,
                            filePath,
                            sectionName,
                            key,
                            oldValueForLog,
                            value));
                    }

                    continue;
                }

                if (!allowAddKey)
                {
                    trackSkipped?.Add(BuildSkipped(
                        profileName,
                        ConfigSkipReasons.MissingPathTarget,
                        filePath,
                        sectionName,
                        key,
                        MissingValue(),
                        value));
                    continue;
                }

                var insertIndex = sectionRange.Value.EndExclusive;
                lines.Insert(insertIndex, $"{key}={value}{preferredNewline}");
                changed = true;
                sectionRange = (sectionRange.Value.StartInclusive, sectionRange.Value.EndExclusive + 1);
                trackApplied.Add(BuildApplied(
                    profileName,
                    filePath,
                    sectionName,
                    key,
                    MissingValue(),
                    value));
            }
        }

        if (!changed)
        {
            return false;
        }

        IniTextCodec.WriteWithEncoding(filePath, string.Concat(lines), read.Encoding);
        return true;
    }

    public static bool RemoveEntries(
        string filePath,
        IReadOnlyDictionary<string, IReadOnlyList<string>> sectionKeys,
        bool removeEmptySections,
        List<ConfigProfileAppliedRow> trackRemoved,
        List<ConfigProfileSkippedRow>? trackSkipped = null,
        string profileName = ConfigProfileNames.EngineIniProfile)
    {
        if (!File.Exists(filePath))
        {
            foreach (var (sectionName, keys) in sectionKeys)
            {
                foreach (var key in keys.Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    trackSkipped?.Add(BuildSkipped(
                        profileName,
                        ConfigSkipReasons.TargetFileNotFound,
                        filePath,
                        sectionName,
                        key,
                        MissingValue(),
                        ""));
                }
            }

            return false;
        }

        var read = IniTextCodec.ReadWithFallback(filePath);
        var lines = read.Text.SplitLinesKeepEndings().ToList();
        var changed = false;

        foreach (var (sectionName, keys) in sectionKeys)
        {
            var distinctKeys = keys
                .Where(static key => !string.IsNullOrWhiteSpace(key))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            if (distinctKeys.Length == 0)
            {
                continue;
            }

            var sectionRange = FindSectionRange(lines, sectionName);
            if (sectionRange is null)
            {
                foreach (var key in distinctKeys)
                {
                    trackSkipped?.Add(BuildSkipped(
                        profileName,
                        ConfigSkipReasons.MissingPathTarget,
                        filePath,
                        sectionName,
                        key,
                        MissingValue(),
                        ""));
                }

                continue;
            }

            foreach (var key in distinctKeys)
            {
                var keyLineIndex = FindKeyLine(lines, sectionRange.Value.StartInclusive, sectionRange.Value.EndExclusive, key);
                if (keyLineIndex < 0)
                {
                    trackSkipped?.Add(BuildSkipped(
                        profileName,
                        ConfigSkipReasons.MissingPathTarget,
                        filePath,
                        sectionName,
                        key,
                        MissingValue(),
                        ""));
                    continue;
                }

                var oldValueForLog = ExtractIniValueForLog(lines[keyLineIndex], allowColon: true);
                lines.RemoveAt(keyLineIndex);
                sectionRange = (sectionRange.Value.StartInclusive, sectionRange.Value.EndExclusive - 1);
                changed = true;
                trackRemoved.Add(BuildApplied(
                    profileName,
                    filePath,
                    sectionName,
                    key,
                    oldValueForLog,
                    "<removed>"));
            }

            if (removeEmptySections)
            {
                var updatedSectionRange = FindSectionRange(lines, sectionName);
                if (updatedSectionRange is not null
                    && !HasSectionBodyContent(lines, updatedSectionRange.Value.StartInclusive, updatedSectionRange.Value.EndExclusive))
                {
                    var headerIndex = updatedSectionRange.Value.StartInclusive - 1;
                    if (headerIndex >= 0)
                    {
                        lines.RemoveRange(headerIndex, updatedSectionRange.Value.EndExclusive - headerIndex);
                        changed = true;
                    }
                }
            }
        }

        if (!changed)
        {
            return false;
        }

        IniTextCodec.WriteWithEncoding(filePath, string.Concat(lines), read.Encoding);
        return true;
    }

    private static ConfigProfileAppliedRow BuildApplied(
        string profileName,
        string filePath,
        string sectionName,
        string key,
        string oldValue,
        string newValue)
    {
        return new ConfigProfileAppliedRow
        {
            ProfileName = profileName,
            TargetPath = filePath,
            TargetKey = $"{sectionName}:{key}",
            OldValue = oldValue,
            NewValue = newValue
        };
    }

    private static ConfigProfileSkippedRow BuildSkipped(
        string profileName,
        string reasonCode,
        string filePath,
        string sectionName,
        string key,
        string oldValue,
        string newValue)
    {
        return new ConfigProfileSkippedRow
        {
            ProfileName = profileName,
            ReasonCode = reasonCode,
            Detail = $"{sectionName}:{key}",
            TargetPath = filePath,
            TargetKey = $"{sectionName}:{key}",
            OldValue = oldValue,
            NewValue = newValue
        };
    }

    private static string MissingValue()
    {
        return "<missing>";
    }

    private static string ExtractIniValueForLog(string line, bool allowColon)
    {
        var (body, _) = line.SplitLineEnding();
        var match = KeyValuePattern.Match(body);
        if (!match.Success && allowColon)
        {
            match = KeyColonPattern.Match(body);
        }

        if (!match.Success)
        {
            return "";
        }

        var valueText = match.Groups[4].Value.Trim();
        var commentIndex = FindIniCommentIndex(valueText);
        if (commentIndex >= 0)
        {
            valueText = valueText[..commentIndex].TrimEnd();
        }

        if (valueText.EndsWith(",", StringComparison.Ordinal))
        {
            valueText = valueText[..^1].TrimEnd();
        }

        return valueText.Trim();
    }

    private static string ReplaceKeyValueLine(string line, string newValue, bool allowColon, string currentSection)
    {
        var (body, ending) = line.SplitLineEnding();
        var match = KeyValuePattern.Match(body);
        var delimiter = "=";
        if (!match.Success && allowColon)
        {
            match = KeyColonPattern.Match(body);
            delimiter = ":";
        }

        if (!match.Success)
        {
            return line;
        }

        var prefix = match.Groups[1].Value;
        var keyText = match.Groups[2].Value;
        var keySpaceBeforeDelimiter = match.Groups[3].Value;
        var oldRest = match.Groups[4].Value;
        var normalizedKey = NormalizeKey(RemoveWrappingQuotes(keyText));
        if (normalizedKey == "depthinverted" && !string.Equals(NormalizeKey(currentSection), "xefg", StringComparison.Ordinal))
        {
            return line;
        }

        var rebuiltRest = delimiter == "="
            ? RebuildIniEqualsRest(oldRest, newValue)
            : RebuildIniColonRest(oldRest, newValue);
        return $"{prefix}{keyText}{keySpaceBeforeDelimiter}{delimiter}{rebuiltRest}{ending}";
    }

    private static (int StartInclusive, int EndExclusive)? FindSectionRange(IReadOnlyList<string> lines, string sectionName)
    {
        var normalized = NormalizeKey(sectionName);
        var start = -1;
        for (var index = 0; index < lines.Count; index++)
        {
            var body = lines[index].SplitLineEnding().Body.TrimStart('\uFEFF');
            var match = SectionPattern.Match(body);
            if (!match.Success)
            {
                continue;
            }

            var currentSection = NormalizeKey(match.Groups[1].Value);
            if (start >= 0)
            {
                return (start, index);
            }

            if (currentSection == normalized)
            {
                start = index + 1;
            }
        }

        if (start >= 0)
        {
            return (start, lines.Count);
        }

        return null;
    }

    private static int FindKeyLine(IReadOnlyList<string> lines, int startInclusive, int endExclusive, string key)
    {
        var normalizedTargetKey = NormalizeKey(RemoveWrappingQuotes(key));
        for (var index = startInclusive; index < endExclusive; index++)
        {
            var body = lines[index].SplitLineEnding().Body.TrimStart('\uFEFF');
            var match = KeyValuePattern.Match(body);
            if (!match.Success)
            {
                match = KeyColonPattern.Match(body);
            }

            if (!match.Success)
            {
                continue;
            }

            var normalizedCandidate = NormalizeKey(RemoveWrappingQuotes(match.Groups[2].Value));
            if (normalizedCandidate == normalizedTargetKey)
            {
                return index;
            }
        }

        return -1;
    }

    private static bool HasSectionBodyContent(IReadOnlyList<string> lines, int startInclusive, int endExclusive)
    {
        for (var index = startInclusive; index < endExclusive; index++)
        {
            var body = lines[index].SplitLineEnding().Body.TrimStart('\uFEFF').Trim();
            if (body.Length > 0)
            {
                return true;
            }
        }

        return false;
    }

    private static string RebuildIniEqualsRest(string oldRest, string newValue)
    {
        var leadingWhitespaceLength = oldRest.Length - oldRest.TrimStart().Length;
        var leadingWhitespace = oldRest[..leadingWhitespaceLength];
        var body = oldRest[leadingWhitespaceLength..];
        var commentIndex = FindIniCommentIndex(body);
        if (commentIndex < 0)
        {
            return $"{leadingWhitespace}{newValue}";
        }

        var comment = body[commentIndex..].TrimStart();
        return string.IsNullOrWhiteSpace(comment)
            ? $"{leadingWhitespace}{newValue}"
            : $"{leadingWhitespace}{newValue} {comment}";
    }

    private static string RebuildIniColonRest(string oldRest, string newValue)
    {
        var leadingWhitespaceLength = oldRest.Length - oldRest.TrimStart().Length;
        var leadingWhitespace = oldRest[..leadingWhitespaceLength];
        var hasTrailingComma = oldRest.TrimEnd().EndsWith(",", StringComparison.Ordinal);
        return hasTrailingComma
            ? $"{leadingWhitespace}{newValue},"
            : $"{leadingWhitespace}{newValue}";
    }

    private static int FindIniCommentIndex(string body)
    {
        for (var index = 0; index < body.Length; index++)
        {
            var ch = body[index];
            if (ch == ';' || ch == '#')
            {
                return index;
            }
        }

        return -1;
    }

    private static string RemoveWrappingQuotes(string value)
    {
        var text = (value ?? "").Trim();
        if (text.Length < 2)
        {
            return text;
        }

        if ((text[0] == '"' && text[^1] == '"') || (text[0] == '\'' && text[^1] == '\''))
        {
            return text[1..^1].Trim();
        }

        return text;
    }

    private static string NormalizeKey(string value)
    {
        return new string((value ?? "")
            .Where(static ch => !char.IsWhiteSpace(ch))
            .ToArray())
            .ToLowerInvariant();
    }
}

internal static class IniLineExtensions
{
    public static (string Body, string Ending) SplitLineEnding(this string line)
    {
        if (line.EndsWith("\r\n", StringComparison.Ordinal))
        {
            return (line[..^2], "\r\n");
        }

        if (line.EndsWith('\n'))
        {
            return (line[..^1], "\n");
        }

        if (line.EndsWith('\r'))
        {
            return (line[..^1], "\r");
        }

        return (line, string.Empty);
    }

    public static IEnumerable<string> SplitLinesKeepEndings(this string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            yield break;
        }

        var start = 0;
        for (var index = 0; index < text.Length; index++)
        {
            if (text[index] != '\r' && text[index] != '\n')
            {
                continue;
            }

            if (text[index] == '\r' && index + 1 < text.Length && text[index + 1] == '\n')
            {
                index++;
            }

            yield return text[start..(index + 1)];
            start = index + 1;
        }

        if (start < text.Length)
        {
            yield return text[start..];
        }
    }

    public static bool EndsWithAnyNewLine(this string text)
    {
        return text.EndsWith("\r\n", StringComparison.Ordinal)
               || text.EndsWith('\n')
               || text.EndsWith('\r');
    }
}
