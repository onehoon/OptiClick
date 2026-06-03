namespace OptiClick.Wpf.Install.Config.Xml;

public static class XmlSpanUpdater
{
    public static (string UpdatedText, bool Changed) UpdateAttribute(
        string text,
        XmlElementSpan element,
        string attributeName,
        string valueText)
    {
        if (!element.Attributes.TryGetValue(attributeName, out var attribute))
        {
            var quote = ChooseAttributeQuote(element);
            var escapedValue = EscapeXmlAttribute(valueText, quote);
            var insertion = $" {attributeName}={quote}{escapedValue}{quote}";
            return (ReplaceRange(text, element.AttributeInsertAt, element.AttributeInsertAt, insertion), true);
        }

        var previousValue = UnescapeXmlValue(text[attribute.ValueStart..attribute.ValueEnd]);
        if (string.Equals(previousValue, valueText, StringComparison.Ordinal))
        {
            return (text, false);
        }

        var escaped = EscapeXmlAttribute(valueText, attribute.Quote);
        return (ReplaceRange(text, attribute.ValueStart, attribute.ValueEnd, escaped), true);
    }

    public static (string UpdatedText, bool Changed) UpdateText(
        string text,
        XmlElementSpan element,
        string valueText)
    {
        if (element.SelfClosing)
        {
            if (string.IsNullOrEmpty(valueText))
            {
                return (text, false);
            }

            var startTagBody = text[element.StartTagStart..element.StartCloseStart].TrimEnd();
            var escaped = EscapeXmlText(valueText);
            var replacement = $"{startTagBody}>{escaped}</{element.Tag}>";
            return (ReplaceRange(text, element.StartTagStart, element.StartTagEnd, replacement), true);
        }

        if (element.EndTagStart is null)
        {
            throw new InvalidOperationException($"Missing XML closing tag for {string.Join("/", element.Path)}");
        }

        var currentInner = text[element.ContentStart..element.EndTagStart.Value];
        if (element.Children > 0 || currentInner.Contains('<', StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"XML path {string.Join("/", element.Path)} does not map to a simple text node");
        }

        var previousValue = UnescapeXmlValue(currentInner).Trim();
        if (string.Equals(previousValue, valueText, StringComparison.Ordinal))
        {
            return (text, false);
        }

        var escapedValue = EscapeXmlText(valueText);
        return (ReplaceRange(text, element.ContentStart, element.EndTagStart.Value, escapedValue), true);
    }

    public static string EscapeXmlText(string value)
    {
        return (value ?? "")
            .Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal);
    }

    public static string EscapeXmlAttribute(string value, char quote)
    {
        var escaped = EscapeXmlText(value);
        return quote == '\''
            ? escaped.Replace("'", "&apos;", StringComparison.Ordinal)
            : escaped.Replace("\"", "&quot;", StringComparison.Ordinal);
    }

    public static string UnescapeXmlValue(string value)
    {
        return (value ?? "")
            .Replace("&lt;", "<", StringComparison.Ordinal)
            .Replace("&gt;", ">", StringComparison.Ordinal)
            .Replace("&quot;", "\"", StringComparison.Ordinal)
            .Replace("&apos;", "'", StringComparison.Ordinal)
            .Replace("&amp;", "&", StringComparison.Ordinal);
    }

    public static string ReplaceRange(string text, int start, int end, string replacement)
    {
        var source = text ?? "";
        var prefix = source[..start];
        var suffix = source[end..];
        return $"{prefix}{replacement}{suffix}";
    }

    private static char ChooseAttributeQuote(XmlElementSpan element)
    {
        foreach (var attribute in element.Attributes.Values)
        {
            if (attribute.Quote is '"' or '\'')
            {
                return attribute.Quote;
            }
        }

        return '"';
    }
}
