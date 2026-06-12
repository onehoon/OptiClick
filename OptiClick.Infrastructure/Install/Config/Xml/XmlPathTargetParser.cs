using System.Collections;
using System.Linq;

namespace OptiClick.Infrastructure.Install.Config.Xml;

public static class XmlPathTargetParser
{
    public static XmlNormalizedTarget NormalizeTarget(object? target)
    {
        var pathText = NormalizePathText(target);
        if (string.IsNullOrWhiteSpace(pathText))
        {
            return new XmlNormalizedTarget();
        }

        var normalizedPath = pathText.Replace('\\', '/').Trim('/');
        while (normalizedPath.StartsWith("./", StringComparison.Ordinal))
        {
            normalizedPath = normalizedPath[2..].Trim('/');
        }

        if (string.IsNullOrWhiteSpace(normalizedPath))
        {
            return new XmlNormalizedTarget();
        }

        string? attributeName = null;
        var lastAt = normalizedPath.LastIndexOf('@');
        if (lastAt >= 0)
        {
            attributeName = normalizedPath[(lastAt + 1)..].Trim();
            normalizedPath = normalizedPath[..lastAt].Trim('/');
            if (string.IsNullOrWhiteSpace(attributeName))
            {
                attributeName = null;
            }
        }

        var parts = new List<XmlPathPart>();
        foreach (var rawPart in normalizedPath.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var part = ParsePathPart(rawPart);
            if (part is not null)
            {
                parts.Add(part);
            }
        }

        return new XmlNormalizedTarget
        {
            PathParts = parts,
            AttributeName = attributeName
        };
    }

    public static string FormatPathParts(IReadOnlyList<XmlPathPart> pathParts)
    {
        if (pathParts is null || pathParts.Count == 0)
        {
            return "";
        }

        var segments = new List<string>(pathParts.Count);
        foreach (var part in pathParts)
        {
            var suffix = string.Concat(part.AttributeFilters.Select(static item => $"@{item.Key}={item.Value}"));
            segments.Add($"{part.Tag}{suffix}");
        }

        return string.Join("/", segments);
    }

    private static string NormalizePathText(object? target)
    {
        if (target is null)
        {
            return "";
        }

        if (target is string text)
        {
            return text.Trim();
        }

        if (target is IEnumerable enumerable)
        {
            var parts = new List<string>();
            foreach (var item in enumerable)
            {
                var segment = (item?.ToString() ?? "").Trim();
                if (!string.IsNullOrWhiteSpace(segment))
                {
                    parts.Add(segment);
                }
            }

            return string.Join("/", parts);
        }

        return (target.ToString() ?? "").Trim();
    }

    private static XmlPathPart? ParsePathPart(string pathPart)
    {
        var raw = (pathPart ?? "").Trim();
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var segments = raw.Split('@', StringSplitOptions.TrimEntries);
        var tag = segments[0].Trim();
        if (string.IsNullOrWhiteSpace(tag))
        {
            return null;
        }

        var filters = new List<KeyValuePair<string, string>>();
        foreach (var selector in segments.Skip(1))
        {
            if (string.IsNullOrWhiteSpace(selector))
            {
                continue;
            }

            var equalIndex = selector.IndexOf('=');
            if (equalIndex < 0)
            {
                tag = $"{tag}@{selector}";
                continue;
            }

            var attributeName = selector[..equalIndex].Trim();
            if (string.IsNullOrWhiteSpace(attributeName))
            {
                continue;
            }

            var expectedValue = selector[(equalIndex + 1)..].Trim();
            filters.Add(new KeyValuePair<string, string>(attributeName, expectedValue));
        }

        return new XmlPathPart
        {
            Tag = tag,
            AttributeFilters = filters
        };
    }
}
