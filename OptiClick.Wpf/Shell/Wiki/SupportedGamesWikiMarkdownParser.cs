using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace OptiClick.Wpf.Shell.Wiki;

public sealed class SupportedGamesWikiMarkdownParseResult
{
    public bool IsSuccess { get; init; }
    public string ErrorCode { get; init; } = "";
    public IReadOnlyList<SupportedGamesWikiEntry> Entries { get; init; } = [];

    public static SupportedGamesWikiMarkdownParseResult Success(IReadOnlyList<SupportedGamesWikiEntry> entries)
    {
        return new SupportedGamesWikiMarkdownParseResult
        {
            IsSuccess = true,
            Entries = entries ?? []
        };
    }

    public static SupportedGamesWikiMarkdownParseResult Failure(string errorCode)
    {
        return new SupportedGamesWikiMarkdownParseResult
        {
            IsSuccess = false,
            ErrorCode = string.IsNullOrWhiteSpace(errorCode) ? "invalid_payload" : errorCode.Trim()
        };
    }
}

public interface ISupportedGamesWikiMarkdownParser
{
    SupportedGamesWikiMarkdownParseResult Parse(string markdown);
}

public sealed class SupportedGamesWikiMarkdownParser : ISupportedGamesWikiMarkdownParser
{
    private const string SupportedGamesMetadataStart = "<!-- opticlick-supported-games-meta";
    private const string SupportedGamesMetadataEnd = "-->";

    private static readonly Regex HeaderRegex = new(
        @"\|\s*Korean Title\s*\|\s*English Title\s*\|\s*Intel\s*\|\s*AMD\s*\|\s*NVIDIA\s*\|",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static readonly Regex MarkdownLinkRegex = new(
        @"\[(?<text>[^\]]+)\]\((?<url>[^)]+)\)",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static readonly Regex HtmlBreakRegex = new(
        @"<br\s*/?>",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static readonly Regex WhitespaceRegex = new(
        @"\s+",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public SupportedGamesWikiMarkdownParseResult Parse(string markdown)
    {
        var payload = markdown ?? "";
        if (string.IsNullOrWhiteSpace(payload))
        {
            return SupportedGamesWikiMarkdownParseResult.Failure("empty_payload");
        }

        var metadataByKey = ParseSupportedGamesMetadataByKey(payload);
        var tables = ParseTables(payload);
        if (tables.Count == 0)
        {
            return SupportedGamesWikiMarkdownParseResult.Failure("table_header_not_found");
        }

        var newlySupportedRows = tables[0];
        var allSupportedRows = tables.Count >= 2 ? tables[1] : tables[0];

        var newlySupportedKeys = new HashSet<string>(
            newlySupportedRows
                .Select(static row => BuildDedupKey(row.KoreanTitle, row.EnglishTitle))
                .Where(static key => !string.IsNullOrWhiteSpace(key)),
            StringComparer.OrdinalIgnoreCase);

        var entries = new List<SupportedGamesWikiEntry>(allSupportedRows.Count);
        var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < allSupportedRows.Count; i++)
        {
            var row = allSupportedRows[i];
            if (string.IsNullOrWhiteSpace(row.KoreanTitle)
                && string.IsNullOrWhiteSpace(row.EnglishTitle))
            {
                continue;
            }

            var dedupKey = BuildDedupKey(row.KoreanTitle, row.EnglishTitle);
            if (!seenKeys.Add(dedupKey))
            {
                continue;
            }

            var gameNameKr = (row.KoreanTitle ?? "").Trim();
            var gameNameEn = (row.EnglishTitle ?? "").Trim();
            metadataByKey.TryGetValue(dedupKey, out var metadata);
            var metadataGameId = ResolvePrimaryValue(metadata?.GameId, metadata?.GameIds);
            var gameId = string.IsNullOrWhiteSpace(metadataGameId)
                ? BuildGameId(gameNameEn, gameNameKr, i)
                : metadataGameId;
            var coverSteamAppId = ResolvePrimaryValue(metadata?.CoverSteamAppId, metadata?.CoverSteamAppIds);
            var coverUrl = ResolvePrimaryValue(metadata?.CoverUrl, metadata?.CoverUrls);

            entries.Add(new SupportedGamesWikiEntry
            {
                GameId = gameId,
                GameIds = NormalizeMetadataValues(metadata?.GameIds, gameId),
                GameNameEn = gameNameEn,
                GameNameKr = gameNameKr,
                CoverSteamAppId = coverSteamAppId,
                CoverSteamAppIds = NormalizeMetadataValues(metadata?.CoverSteamAppIds, coverSteamAppId),
                CoverUrl = coverUrl,
                CoverUrls = NormalizeMetadataValues(metadata?.CoverUrls, coverUrl),
                IntelText = NormalizeVendorText(row.IntelText),
                AmdText = NormalizeVendorText(row.AmdText),
                NvidiaText = NormalizeVendorText(row.NvidiaText),
                IsNewlySupported = newlySupportedKeys.Contains(dedupKey)
            });
        }

        if (entries.Count == 0)
        {
            return SupportedGamesWikiMarkdownParseResult.Failure("table_rows_not_found");
        }

        return SupportedGamesWikiMarkdownParseResult.Success(entries);
    }

    private static IReadOnlyList<IReadOnlyList<ParsedWikiRow>> ParseTables(string markdown)
    {
        var tables = new List<IReadOnlyList<ParsedWikiRow>>();
        var searchIndex = 0;
        while (searchIndex < markdown.Length)
        {
            var match = HeaderRegex.Match(markdown, searchIndex);
            if (!match.Success)
            {
                break;
            }

            var cursor = match.Index + match.Length;
            var rows = ParseRows(markdown, ref cursor);
            if (rows.Count > 0)
            {
                tables.Add(rows);
            }

            searchIndex = Math.Max(cursor, match.Index + match.Length);
        }

        return tables;
    }

    private static List<ParsedWikiRow> ParseRows(string markdown, ref int cursor)
    {
        var rows = new List<ParsedWikiRow>();
        while (true)
        {
            var rowStart = cursor;
            if (!TryReadNextRow(markdown, ref cursor, out var cells))
            {
                break;
            }

            if (IsHeaderRow(cells))
            {
                cursor = rowStart;
                break;
            }

            if (IsSeparatorRow(cells))
            {
                continue;
            }

            rows.Add(new ParsedWikiRow
            {
                KoreanTitle = cells[0],
                EnglishTitle = cells[1],
                IntelText = cells[2],
                AmdText = cells[3],
                NvidiaText = cells[4]
            });
        }

        return rows;
    }

    private static bool TryReadNextRow(string markdown, ref int cursor, out string[] cells)
    {
        cells = Array.Empty<string>();
        var index = SkipWhitespace(markdown, cursor);
        if (index >= markdown.Length || markdown[index] != '|')
        {
            return false;
        }

        var values = new string[5];
        var position = index + 1;
        for (var i = 0; i < 5; i++)
        {
            var delimiter = FindNextCellDelimiter(markdown, position);
            if (delimiter < 0)
            {
                return false;
            }

            values[i] = NormalizeCell(markdown[position..delimiter]);
            position = delimiter + 1;
        }

        cursor = position;
        cells = values;
        return true;
    }

    private static int SkipWhitespace(string value, int index)
    {
        var cursor = Math.Max(0, index);
        while (cursor < value.Length && char.IsWhiteSpace(value[cursor]))
        {
            cursor++;
        }

        return cursor;
    }

    private static int FindNextCellDelimiter(string value, int startIndex)
    {
        var index = Math.Max(0, startIndex);
        var squareDepth = 0;
        var roundDepth = 0;
        var inCodeSpan = false;
        while (index < value.Length)
        {
            var current = value[index];
            if (current == '\\')
            {
                index += 2;
                continue;
            }

            if (current == '`')
            {
                inCodeSpan = !inCodeSpan;
                index++;
                continue;
            }

            if (!inCodeSpan)
            {
                switch (current)
                {
                    case '[':
                        squareDepth++;
                        break;
                    case ']':
                        if (squareDepth > 0)
                        {
                            squareDepth--;
                        }
                        break;
                    case '(':
                        if (squareDepth > 0 || roundDepth > 0)
                        {
                            roundDepth++;
                        }
                        break;
                    case ')':
                        if (roundDepth > 0)
                        {
                            roundDepth--;
                        }
                        break;
                    case '|':
                        if (squareDepth == 0 && roundDepth == 0)
                        {
                            return index;
                        }
                        break;
                }
            }

            index++;
        }

        return -1;
    }

    private static bool IsSeparatorRow(IReadOnlyList<string> cells)
    {
        if (cells.Count != 5)
        {
            return false;
        }

        for (var i = 0; i < cells.Count; i++)
        {
            var text = (cells[i] ?? "").Trim();
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            for (var c = 0; c < text.Length; c++)
            {
                var ch = text[c];
                if (ch is not ('-' or ':'))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static bool IsHeaderRow(IReadOnlyList<string> cells)
    {
        if (cells.Count != 5)
        {
            return false;
        }

        return EqualsCell(cells[0], "Korean Title")
               && EqualsCell(cells[1], "English Title")
               && EqualsCell(cells[2], "Intel")
               && EqualsCell(cells[3], "AMD")
               && EqualsCell(cells[4], "NVIDIA");
    }

    private static bool EqualsCell(string left, string right)
    {
        return string.Equals((left ?? "").Trim(), right, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeCell(string value)
    {
        var normalized = (value ?? "").Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return "";
        }

        var text = normalized;
        text = text.Replace("\\|", "|", StringComparison.Ordinal);
        text = HtmlBreakRegex.Replace(text, " / ");
        text = MarkdownLinkRegex.Replace(text, "${text}");
        text = text.Replace("**", "", StringComparison.Ordinal)
            .Replace("__", "", StringComparison.Ordinal)
            .Replace("`", "", StringComparison.Ordinal)
            .Replace("*", "", StringComparison.Ordinal);
        text = WhitespaceRegex.Replace(text, " ").Trim();
        return text;
    }

    private static IReadOnlyDictionary<string, SupportedGamesWikiAppMetadata> ParseSupportedGamesMetadataByKey(string markdown)
    {
        var metadataJson = ExtractSupportedGamesMetadataJson(markdown);
        if (string.IsNullOrWhiteSpace(metadataJson))
        {
            return new Dictionary<string, SupportedGamesWikiAppMetadata>(StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            var document = JsonSerializer.Deserialize<SupportedGamesWikiAppMetadataDocument>(metadataJson);
            if (document?.Games is null || document.Games.Count == 0)
            {
                return new Dictionary<string, SupportedGamesWikiAppMetadata>(StringComparer.OrdinalIgnoreCase);
            }

            var metadataByKey = new Dictionary<string, SupportedGamesWikiAppMetadata>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in document.Games)
            {
                var key = BuildDedupKey(
                    NormalizeCell(item.GameNameKr),
                    NormalizeCell(item.GameNameEn));
                if (string.IsNullOrWhiteSpace(key) || metadataByKey.ContainsKey(key))
                {
                    continue;
                }

                metadataByKey[key] = item;
            }

            return metadataByKey;
        }
        catch (JsonException)
        {
            return new Dictionary<string, SupportedGamesWikiAppMetadata>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private static string ExtractSupportedGamesMetadataJson(string markdown)
    {
        var payload = markdown ?? "";
        var startIndex = payload.IndexOf(SupportedGamesMetadataStart, StringComparison.OrdinalIgnoreCase);
        if (startIndex < 0)
        {
            return "";
        }

        var contentStart = startIndex + SupportedGamesMetadataStart.Length;
        var endIndex = payload.IndexOf(SupportedGamesMetadataEnd, contentStart, StringComparison.Ordinal);
        if (endIndex <= contentStart)
        {
            return "";
        }

        return payload[contentStart..endIndex].Trim();
    }

    private static string ResolvePrimaryValue(string? primaryValue, IReadOnlyList<string>? fallbackValues)
    {
        var normalizedPrimary = (primaryValue ?? "").Trim();
        if (!string.IsNullOrWhiteSpace(normalizedPrimary))
        {
            return normalizedPrimary;
        }

        if (fallbackValues is null || fallbackValues.Count == 0)
        {
            return "";
        }

        foreach (var value in fallbackValues)
        {
            var normalizedValue = (value ?? "").Trim();
            if (!string.IsNullOrWhiteSpace(normalizedValue))
            {
                return normalizedValue;
            }
        }

        return "";
    }

    private static IReadOnlyList<string> NormalizeMetadataValues(IReadOnlyList<string>? values, string primaryValue)
    {
        var result = new List<string>();
        var normalizedPrimary = (primaryValue ?? "").Trim();
        if (!string.IsNullOrWhiteSpace(normalizedPrimary))
        {
            result.Add(normalizedPrimary);
        }

        if (values is not null)
        {
            foreach (var value in values)
            {
                var normalizedValue = (value ?? "").Trim();
                if (string.IsNullOrWhiteSpace(normalizedValue)
                    || result.Contains(normalizedValue, StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }

                result.Add(normalizedValue);
            }
        }

        return result;
    }

    private static string BuildDedupKey(string koreanTitle, string englishTitle)
    {
        return $"{(koreanTitle ?? "").Trim().ToLowerInvariant()}||{(englishTitle ?? "").Trim().ToLowerInvariant()}";
    }

    private static string BuildGameId(string englishTitle, string koreanTitle, int fallbackIndex)
    {
        var preferred = !string.IsNullOrWhiteSpace(englishTitle) ? englishTitle : koreanTitle;
        var slug = Slugify(preferred);
        if (!string.IsNullOrWhiteSpace(slug))
        {
            return slug;
        }

        return $"wiki_game_{fallbackIndex + 1}";
    }

    private static string Slugify(string value)
    {
        var source = (value ?? "").Trim();
        if (string.IsNullOrWhiteSpace(source))
        {
            return "";
        }

        var sb = new StringBuilder(source.Length);
        var lastUnderscore = false;
        foreach (var ch in source)
        {
            if (char.IsLetterOrDigit(ch))
            {
                sb.Append(char.ToLowerInvariant(ch));
                lastUnderscore = false;
                continue;
            }

            if (!lastUnderscore && sb.Length > 0)
            {
                sb.Append('_');
                lastUnderscore = true;
            }
        }

        return sb.ToString().Trim('_');
    }

    private static string NormalizeVendorText(string value)
    {
        var normalized = (value ?? "").Trim();
        return string.IsNullOrWhiteSpace(normalized) ? "-" : normalized;
    }

    private sealed record ParsedWikiRow
    {
        public string KoreanTitle { get; init; } = "";
        public string EnglishTitle { get; init; } = "";
        public string IntelText { get; init; } = "";
        public string AmdText { get; init; } = "";
        public string NvidiaText { get; init; } = "";
    }

    private sealed record SupportedGamesWikiAppMetadataDocument
    {
        [JsonPropertyName("games")]
        public IReadOnlyList<SupportedGamesWikiAppMetadata> Games { get; init; } = [];
    }

    private sealed record SupportedGamesWikiAppMetadata
    {
        [JsonPropertyName("game_name_kr")]
        public string GameNameKr { get; init; } = "";

        [JsonPropertyName("game_name_en")]
        public string GameNameEn { get; init; } = "";

        [JsonPropertyName("game_id")]
        public string GameId { get; init; } = "";

        [JsonPropertyName("game_ids")]
        public IReadOnlyList<string> GameIds { get; init; } = [];

        [JsonPropertyName("cover_steam_app_id")]
        public string CoverSteamAppId { get; init; } = "";

        [JsonPropertyName("cover_steam_app_ids")]
        public IReadOnlyList<string> CoverSteamAppIds { get; init; } = [];

        [JsonPropertyName("cover_url")]
        public string CoverUrl { get; init; } = "";

        [JsonPropertyName("cover_urls")]
        public IReadOnlyList<string> CoverUrls { get; init; } = [];
    }
}
