using System.Text;

namespace OptiClick.Wpf.Shell.Dialogs.Markup;

public enum PopupMarkupInlineKind
{
    Text,
    LineBreak
}

public sealed record PopupMarkupInlineSegment
{
    public PopupMarkupInlineKind Kind { get; init; } = PopupMarkupInlineKind.Text;
    public string Text { get; init; } = "";
    public bool IsEmphasis { get; init; }

    public static PopupMarkupInlineSegment CreateText(string text, bool isEmphasis)
    {
        return new PopupMarkupInlineSegment
        {
            Kind = PopupMarkupInlineKind.Text,
            Text = text ?? "",
            IsEmphasis = isEmphasis
        };
    }

    public static PopupMarkupInlineSegment CreateLineBreak()
    {
        return new PopupMarkupInlineSegment
        {
            Kind = PopupMarkupInlineKind.LineBreak,
            Text = "",
            IsEmphasis = false
        };
    }
}

public sealed record PopupMarkupBulletItem
{
    public IReadOnlyList<PopupMarkupInlineSegment> Inline { get; init; } = [];
    public string PlainText { get; init; } = "";
}

public sealed record PopupMarkupParseResult
{
    public static PopupMarkupParseResult Empty { get; } = new();

    public IReadOnlyList<PopupMarkupInlineSegment> Inline { get; init; } = [];
    public IReadOnlyList<PopupMarkupBulletItem> BulletItems { get; init; } = [];
    public string PlainText { get; init; } = "";
}

public static class PopupMarkupParser
{
    private static readonly HashSet<string> KnownTokens =
    [
        "RED",
        "END",
        "P",
        "BR",
        "DOT",
        "INDENT"
    ];

    private const int IndentSpaces = 3;

    public static PopupMarkupParseResult Parse(string? rawText)
    {
        var normalizedText = NormalizeLineEndings(rawText);
        if (string.IsNullOrEmpty(normalizedText))
        {
            return PopupMarkupParseResult.Empty;
        }

        var inlineSegments = new List<PopupMarkupInlineSegment>();
        var bulletItems = new List<PopupMarkupBulletItem>();
        List<PopupMarkupInlineSegment>? currentBulletSegments = null;
        var isEmphasis = false;

        var cursor = 0;
        while (cursor < normalizedText.Length)
        {
            if (TryReadMarkupToken(normalizedText, cursor, out var token, out var tokenLength))
            {
                cursor += tokenLength;

                switch (token)
                {
                    case "RED":
                        isEmphasis = true;
                        break;

                    case "END":
                        isEmphasis = false;
                        break;

                    case "P":
                        if (currentBulletSegments is not null)
                        {
                            FinalizeBulletItem(currentBulletSegments, bulletItems);
                            currentBulletSegments = null;
                        }

                        AddLineBreak(inlineSegments);
                        AddLineBreak(inlineSegments);
                        break;

                    case "BR":
                        AddLineBreak(GetActiveSegments(inlineSegments, currentBulletSegments));
                        break;

                    case "DOT":
                        if (currentBulletSegments is null)
                        {
                            currentBulletSegments = new List<PopupMarkupInlineSegment>();
                            break;
                        }

                        if (HasVisibleContent(currentBulletSegments))
                        {
                            FinalizeBulletItem(currentBulletSegments, bulletItems);
                            currentBulletSegments = new List<PopupMarkupInlineSegment>();
                        }

                        break;

                    case "INDENT":
                        AddText(GetActiveSegments(inlineSegments, currentBulletSegments), new string(' ', IndentSpaces), isEmphasis);
                        break;
                }

                continue;
            }

            var current = normalizedText[cursor];
            if (current == '\n')
            {
                AddLineBreak(GetActiveSegments(inlineSegments, currentBulletSegments));
            }
            else
            {
                AddText(GetActiveSegments(inlineSegments, currentBulletSegments), current.ToString(), isEmphasis);
            }

            cursor++;
        }

        if (currentBulletSegments is not null)
        {
            FinalizeBulletItem(currentBulletSegments, bulletItems);
        }

        var normalizedInline = NormalizeSegments(inlineSegments, trimOuterWhitespace: true);
        return new PopupMarkupParseResult
        {
            Inline = normalizedInline,
            BulletItems = bulletItems,
            PlainText = BuildPlainText(normalizedInline).Trim()
        };
    }

    private static List<PopupMarkupInlineSegment> GetActiveSegments(
        List<PopupMarkupInlineSegment> inlineSegments,
        List<PopupMarkupInlineSegment>? currentBulletSegments)
    {
        return currentBulletSegments ?? inlineSegments;
    }

    private static void AddText(List<PopupMarkupInlineSegment> segments, string text, bool isEmphasis)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        if (segments.Count > 0)
        {
            var last = segments[^1];
            if (last.Kind == PopupMarkupInlineKind.Text && last.IsEmphasis == isEmphasis)
            {
                segments[^1] = last with { Text = last.Text + text };
                return;
            }
        }

        segments.Add(PopupMarkupInlineSegment.CreateText(text, isEmphasis));
    }

    private static void AddLineBreak(List<PopupMarkupInlineSegment> segments)
    {
        segments.Add(PopupMarkupInlineSegment.CreateLineBreak());
    }

    private static bool TryReadMarkupToken(
        string text,
        int start,
        out string token,
        out int tokenLength)
    {
        token = "";
        tokenLength = 0;

        if (start < 0 || start >= text.Length || text[start] != '[')
        {
            return false;
        }

        var endBracket = text.IndexOf(']', start + 1);
        var endBrace = text.IndexOf('}', start + 1);
        var endIndex = ResolveTokenEnd(endBracket, endBrace);
        if (endIndex <= start)
        {
            return false;
        }

        var rawToken = text[(start + 1)..endIndex].Trim();
        if (string.IsNullOrWhiteSpace(rawToken))
        {
            return false;
        }

        var normalized = rawToken.ToUpperInvariant();
        if (!KnownTokens.Contains(normalized))
        {
            return false;
        }

        token = normalized;
        tokenLength = endIndex - start + 1;
        return true;
    }

    private static int ResolveTokenEnd(int endBracket, int endBrace)
    {
        if (endBracket < 0)
        {
            return endBrace;
        }

        if (endBrace < 0)
        {
            return endBracket;
        }

        return Math.Min(endBracket, endBrace);
    }

    private static string NormalizeLineEndings(string? value)
    {
        return (value ?? "")
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n');
    }

    private static void FinalizeBulletItem(
        List<PopupMarkupInlineSegment> segments,
        List<PopupMarkupBulletItem> items)
    {
        var normalizedSegments = NormalizeSegments(segments, trimOuterWhitespace: true);
        if (!HasVisibleContent(normalizedSegments))
        {
            return;
        }

        items.Add(new PopupMarkupBulletItem
        {
            Inline = normalizedSegments,
            PlainText = BuildPlainText(normalizedSegments).Trim()
        });
    }

    private static bool HasVisibleContent(IReadOnlyList<PopupMarkupInlineSegment> segments)
    {
        foreach (var segment in segments)
        {
            if (segment.Kind != PopupMarkupInlineKind.Text)
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(segment.Text))
            {
                return true;
            }
        }

        return false;
    }

    private static IReadOnlyList<PopupMarkupInlineSegment> NormalizeSegments(
        IReadOnlyList<PopupMarkupInlineSegment> source,
        bool trimOuterWhitespace)
    {
        if (source.Count == 0)
        {
            return Array.Empty<PopupMarkupInlineSegment>();
        }

        var segments = source.Select(segment => segment).ToList();

        while (segments.Count > 0 && segments[0].Kind == PopupMarkupInlineKind.LineBreak)
        {
            segments.RemoveAt(0);
        }

        while (segments.Count > 0 && segments[^1].Kind == PopupMarkupInlineKind.LineBreak)
        {
            segments.RemoveAt(segments.Count - 1);
        }

        if (trimOuterWhitespace)
        {
            TrimStartWhitespace(segments);
            TrimEndWhitespace(segments);
        }

        segments = segments
            .Where(segment => segment.Kind != PopupMarkupInlineKind.Text || !string.IsNullOrEmpty(segment.Text))
            .ToList();

        return segments;
    }

    private static void TrimStartWhitespace(List<PopupMarkupInlineSegment> segments)
    {
        for (var index = 0; index < segments.Count; index++)
        {
            var segment = segments[index];
            if (segment.Kind != PopupMarkupInlineKind.Text)
            {
                continue;
            }

            segments[index] = segment with { Text = segment.Text.TrimStart() };
            break;
        }
    }

    private static void TrimEndWhitespace(List<PopupMarkupInlineSegment> segments)
    {
        for (var index = segments.Count - 1; index >= 0; index--)
        {
            var segment = segments[index];
            if (segment.Kind != PopupMarkupInlineKind.Text)
            {
                continue;
            }

            segments[index] = segment with { Text = segment.Text.TrimEnd() };
            break;
        }
    }

    private static string BuildPlainText(IReadOnlyList<PopupMarkupInlineSegment> segments)
    {
        if (segments.Count == 0)
        {
            return "";
        }

        var builder = new StringBuilder();
        foreach (var segment in segments)
        {
            if (segment.Kind == PopupMarkupInlineKind.LineBreak)
            {
                builder.Append('\n');
                continue;
            }

            builder.Append(segment.Text);
        }

        return builder.ToString();
    }
}
