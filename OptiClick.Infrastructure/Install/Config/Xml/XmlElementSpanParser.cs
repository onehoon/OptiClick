using System.Linq;

namespace OptiClick.Infrastructure.Install.Config.Xml;

public static class XmlElementSpanParser
{
    private static readonly HashSet<char> NameStopChars = [' ', '\t', '\r', '\n', '/', '>', '='];

    public static IReadOnlyList<XmlElementSpan> ParseElements(string text)
    {
        var elements = new List<XmlElementSpan>();
        var stack = new Stack<int>();
        var cursor = 0;
        var safeText = text ?? "";

        while (cursor < safeText.Length)
        {
            var startIndex = safeText.IndexOf('<', cursor);
            if (startIndex < 0)
            {
                break;
            }

            if (safeText.AsSpan(startIndex).StartsWith("<!--".AsSpan(), StringComparison.Ordinal))
            {
                var end = safeText.IndexOf("-->", startIndex + 4, StringComparison.Ordinal);
                if (end < 0)
                {
                    throw new InvalidOperationException("Unterminated XML comment.");
                }

                cursor = end + 3;
                continue;
            }

            if (safeText.AsSpan(startIndex).StartsWith("<![CDATA[".AsSpan(), StringComparison.Ordinal))
            {
                var end = safeText.IndexOf("]]>", startIndex + 9, StringComparison.Ordinal);
                if (end < 0)
                {
                    throw new InvalidOperationException("Unterminated XML CDATA section.");
                }

                cursor = end + 3;
                continue;
            }

            if (safeText.AsSpan(startIndex).StartsWith("<?".AsSpan(), StringComparison.Ordinal))
            {
                var end = safeText.IndexOf("?>", startIndex + 2, StringComparison.Ordinal);
                if (end < 0)
                {
                    throw new InvalidOperationException("Unterminated XML processing instruction.");
                }

                cursor = end + 2;
                continue;
            }

            if (safeText.AsSpan(startIndex).StartsWith("<!".AsSpan(), StringComparison.Ordinal))
            {
                cursor = FindMarkupEnd(safeText, startIndex);
                continue;
            }

            var tagEnd = FindTagEnd(safeText, startIndex);
            if (safeText.AsSpan(startIndex).StartsWith("</".AsSpan(), StringComparison.Ordinal))
            {
                HandleClosingTag(safeText, startIndex, tagEnd, elements, stack);
                cursor = tagEnd;
                continue;
            }

            var parsedTag = ParseStartTag(safeText, startIndex, tagEnd);
            var parentIndex = stack.Count > 0 ? stack.Peek() : (int?)null;
            var path = parentIndex is null
                ? new[] { parsedTag.TagName }
                : elements[parentIndex.Value].Path.Concat([parsedTag.TagName]).ToArray();

            if (parentIndex is not null)
            {
                elements[parentIndex.Value].Children++;
            }

            var element = new XmlElementSpan
            {
                Tag = parsedTag.TagName,
                Path = path,
                ParentIndex = parentIndex,
                StartTagStart = startIndex,
                StartTagEnd = tagEnd,
                StartCloseStart = parsedTag.StartCloseStart,
                AttributeInsertAt = parsedTag.AttributeInsertAt,
                ContentStart = tagEnd,
                SelfClosing = parsedTag.SelfClosing,
                Attributes = parsedTag.Attributes
            };
            elements.Add(element);
            var elementIndex = elements.Count - 1;

            if (parsedTag.SelfClosing)
            {
                element.EndTagStart = tagEnd;
                element.EndTagEnd = tagEnd;
            }
            else
            {
                stack.Push(elementIndex);
            }

            cursor = tagEnd;
        }

        if (stack.Count > 0)
        {
            var unclosed = elements[stack.Peek()].Tag;
            throw new InvalidOperationException($"Unclosed XML tag: {unclosed}");
        }

        return elements;
    }

    public static XmlElementSpan? FindMatchingElement(
        IReadOnlyList<XmlElementSpan> elements,
        IReadOnlyList<XmlPathPart> pathParts)
    {
        if (elements is null || elements.Count == 0 || pathParts is null || pathParts.Count == 0)
        {
            return null;
        }

        var rootTag = elements[0].Tag;
        for (var i = 0; i < elements.Count; i++)
        {
            var lineage = BuildLineage(elements, i);
            if (LineageMatches(lineage, pathParts))
            {
                return elements[i];
            }

            if (lineage.Count > 0 && string.Equals(lineage[0].Tag, rootTag, StringComparison.Ordinal))
            {
                var rootless = lineage.Skip(1).ToArray();
                if (rootless.Length > 0 && LineageMatches(rootless, pathParts))
                {
                    return elements[i];
                }
            }
        }

        return null;
    }

    private static void HandleClosingTag(
        string text,
        int startIndex,
        int tagEnd,
        IReadOnlyList<XmlElementSpan> elements,
        Stack<int> stack)
    {
        var cursor = startIndex + 2;
        while (cursor < tagEnd && char.IsWhiteSpace(text[cursor]))
        {
            cursor++;
        }

        var nameStart = cursor;
        while (cursor < tagEnd && !NameStopChars.Contains(text[cursor]))
        {
            cursor++;
        }

        var closingTag = text[nameStart..cursor];
        if (stack.Count == 0)
        {
            throw new InvalidOperationException($"Unexpected XML closing tag: {closingTag}");
        }

        var element = elements[stack.Pop()];
        if (!string.Equals(element.Tag, closingTag, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Mismatched XML closing tag: expected {element.Tag}, got {closingTag}");
        }

        element.EndTagStart = startIndex;
        element.EndTagEnd = tagEnd;
    }

    private static (string TagName, IReadOnlyDictionary<string, XmlAttributeSpan> Attributes, int StartCloseStart, int AttributeInsertAt, bool SelfClosing)
        ParseStartTag(string text, int startIndex, int tagEnd)
    {
        var cursor = startIndex + 1;
        while (cursor < tagEnd && char.IsWhiteSpace(text[cursor]))
        {
            cursor++;
        }

        var nameStart = cursor;
        while (cursor < tagEnd && !NameStopChars.Contains(text[cursor]))
        {
            cursor++;
        }

        var tagName = text[nameStart..cursor];
        if (string.IsNullOrWhiteSpace(tagName))
        {
            throw new InvalidOperationException("Invalid XML start tag.");
        }

        var closeScan = tagEnd - 2;
        while (closeScan >= startIndex && char.IsWhiteSpace(text[closeScan]))
        {
            closeScan--;
        }

        var selfClosing = closeScan >= startIndex && text[closeScan] == '/';
        var startCloseStart = selfClosing ? closeScan : tagEnd - 1;

        var insertScan = selfClosing ? startCloseStart - 1 : tagEnd - 2;
        while (insertScan >= startIndex && char.IsWhiteSpace(text[insertScan]))
        {
            insertScan--;
        }

        var attributeInsertAt = insertScan + 1;
        var attributes = new Dictionary<string, XmlAttributeSpan>(StringComparer.Ordinal);
        var limit = selfClosing ? startCloseStart : tagEnd - 1;

        while (cursor < limit)
        {
            while (cursor < limit && char.IsWhiteSpace(text[cursor]))
            {
                cursor++;
            }

            if (cursor >= limit)
            {
                break;
            }

            var attrNameStart = cursor;
            while (cursor < limit && !NameStopChars.Contains(text[cursor]))
            {
                cursor++;
            }

            var attrName = text[attrNameStart..cursor];
            if (string.IsNullOrWhiteSpace(attrName))
            {
                break;
            }

            while (cursor < limit && char.IsWhiteSpace(text[cursor]))
            {
                cursor++;
            }

            if (cursor >= limit || text[cursor] != '=')
            {
                while (cursor < limit && !char.IsWhiteSpace(text[cursor]))
                {
                    cursor++;
                }

                continue;
            }

            cursor++;
            while (cursor < limit && char.IsWhiteSpace(text[cursor]))
            {
                cursor++;
            }

            if (cursor >= limit)
            {
                break;
            }

            var quote = text[cursor];
            if (quote is not '"' and not '\'')
            {
                var valueStart = cursor;
                while (cursor < limit && !char.IsWhiteSpace(text[cursor]))
                {
                    cursor++;
                }

                attributes[attrName] = new XmlAttributeSpan
                {
                    Name = attrName,
                    ValueStart = valueStart,
                    ValueEnd = cursor,
                    Quote = '"',
                    Value = XmlSpanUpdater.UnescapeXmlValue(text[valueStart..cursor])
                };
                continue;
            }

            var quotedValueStart = cursor + 1;
            var quotedValueEnd = quotedValueStart;
            while (quotedValueEnd < limit && text[quotedValueEnd] != quote)
            {
                quotedValueEnd++;
            }

            if (quotedValueEnd >= limit)
            {
                throw new InvalidOperationException($"Unterminated XML attribute value for {attrName}");
            }

            attributes[attrName] = new XmlAttributeSpan
            {
                Name = attrName,
                ValueStart = quotedValueStart,
                ValueEnd = quotedValueEnd,
                Quote = quote,
                Value = XmlSpanUpdater.UnescapeXmlValue(text[quotedValueStart..quotedValueEnd])
            };
            cursor = quotedValueEnd + 1;
        }

        return (tagName, attributes, startCloseStart, attributeInsertAt, selfClosing);
    }

    private static int FindTagEnd(string text, int startIndex)
    {
        char? quote = null;
        for (var i = startIndex + 1; i < text.Length; i++)
        {
            var c = text[i];
            if (quote is not null)
            {
                if (c == quote.Value)
                {
                    quote = null;
                }

                continue;
            }

            if (c is '"' or '\'')
            {
                quote = c;
                continue;
            }

            if (c == '>')
            {
                return i + 1;
            }
        }

        throw new InvalidOperationException("Unterminated XML tag.");
    }

    private static int FindMarkupEnd(string text, int startIndex)
    {
        char? quote = null;
        var bracketDepth = 0;
        for (var i = startIndex + 2; i < text.Length; i++)
        {
            var c = text[i];
            if (quote is not null)
            {
                if (c == quote.Value)
                {
                    quote = null;
                }

                continue;
            }

            if (c is '"' or '\'')
            {
                quote = c;
                continue;
            }

            if (c == '[')
            {
                bracketDepth++;
                continue;
            }

            if (c == ']' && bracketDepth > 0)
            {
                bracketDepth--;
                continue;
            }

            if (c == '>' && bracketDepth == 0)
            {
                return i + 1;
            }
        }

        throw new InvalidOperationException("Unterminated XML markup declaration.");
    }

    private static IReadOnlyList<XmlElementSpan> BuildLineage(IReadOnlyList<XmlElementSpan> elements, int index)
    {
        var lineage = new List<XmlElementSpan>();
        var current = (int?)index;
        while (current is not null)
        {
            var element = elements[current.Value];
            lineage.Add(element);
            current = element.ParentIndex;
        }

        lineage.Reverse();
        return lineage;
    }

    private static bool LineageMatches(IReadOnlyList<XmlElementSpan> lineage, IReadOnlyList<XmlPathPart> pathParts)
    {
        if (lineage.Count != pathParts.Count)
        {
            return false;
        }

        for (var i = 0; i < lineage.Count; i++)
        {
            if (!ElementMatchesPathPart(lineage[i], pathParts[i]))
            {
                return false;
            }
        }

        return true;
    }

    private static bool ElementMatchesPathPart(XmlElementSpan element, XmlPathPart pathPart)
    {
        if (!string.Equals(element.Tag, pathPart.Tag, StringComparison.Ordinal))
        {
            return false;
        }

        foreach (var filter in pathPart.AttributeFilters)
        {
            if (!element.Attributes.TryGetValue(filter.Key, out var attribute))
            {
                return false;
            }

            if (!string.Equals(attribute.Value, filter.Value, StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }
}
