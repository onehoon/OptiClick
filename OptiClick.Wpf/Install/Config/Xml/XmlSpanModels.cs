using System.Text;

namespace OptiClick.Wpf.Install.Config.Xml;

public sealed record XmlEncodingInfo
{
    public Encoding Encoding { get; init; } = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    public byte[] Bom { get; init; } = Array.Empty<byte>();
}

public sealed record XmlTextReadResult
{
    public string Text { get; init; } = "";
    public XmlEncodingInfo EncodingInfo { get; init; } = new();
}

public sealed record XmlAttributeSpan
{
    public string Name { get; init; } = "";
    public int ValueStart { get; init; }
    public int ValueEnd { get; init; }
    public char Quote { get; init; } = '"';
    public string Value { get; init; } = "";
}

public sealed record XmlPathPart
{
    public string Tag { get; init; } = "";
    public IReadOnlyList<KeyValuePair<string, string>> AttributeFilters { get; init; } =
        Array.Empty<KeyValuePair<string, string>>();
}

public sealed class XmlElementSpan
{
    public string Tag { get; init; } = "";
    public IReadOnlyList<string> Path { get; init; } = Array.Empty<string>();
    public int? ParentIndex { get; init; }

    public int StartTagStart { get; init; }
    public int StartTagEnd { get; init; }
    public int StartCloseStart { get; init; }
    public int AttributeInsertAt { get; init; }
    public int ContentStart { get; init; }

    public int? EndTagStart { get; set; }
    public int? EndTagEnd { get; set; }

    public int Children { get; set; }
    public bool SelfClosing { get; init; }

    public IReadOnlyDictionary<string, XmlAttributeSpan> Attributes { get; init; } =
        new Dictionary<string, XmlAttributeSpan>(StringComparer.Ordinal);
}

public sealed record XmlNormalizedTarget
{
    public IReadOnlyList<XmlPathPart> PathParts { get; init; } = Array.Empty<XmlPathPart>();
    public string? AttributeName { get; init; }
}
