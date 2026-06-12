using OptiClick.Core.Install;
using OptiClick.Wpf.Shell.RuntimeData;

namespace OptiClick.Wpf.Install.Execution;

public static class ExcludePatternResolver
{
    private static readonly string[] ModuleExcludeKeys =
    [
        RuntimeDataGameProfileKeys.ExcludeList,
        "__exclude_list__"
    ];

    public static IReadOnlyList<string> Resolve(
        InstallExecutionDescriptor? descriptor,
        ModuleDownloadLinkCatalog? moduleDownloadLinks)
    {
        var descriptorPatterns = ResolveFromDescriptor(descriptor);
        if (descriptorPatterns.Count > 0)
        {
            return descriptorPatterns;
        }

        return ResolveFromModuleDownloadLinks(moduleDownloadLinks);
    }

    private static IReadOnlyList<string> ResolveFromDescriptor(InstallExecutionDescriptor? descriptor)
    {
        if (descriptor?.ExcludeListPatterns?.Count > 0)
        {
            return InstallExcludeListPatternParser.Normalize(descriptor.ExcludeListPatterns);
        }

        var raw = (descriptor?.ExcludeListRaw ?? "").Trim();
        if (string.IsNullOrWhiteSpace(raw))
        {
            return Array.Empty<string>();
        }

        return InstallExcludeListPatternParser.Parse(raw);
    }

    private static IReadOnlyList<string> ResolveFromModuleDownloadLinks(
        ModuleDownloadLinkCatalog? moduleDownloadLinks)
    {
        if (moduleDownloadLinks is null || !moduleDownloadLinks.HasEntries)
        {
            return Array.Empty<string>();
        }

        foreach (var key in ModuleExcludeKeys)
        {
            if (!moduleDownloadLinks.TryGetRawEntry(key, out var entry))
            {
                continue;
            }

            var patterns = ParseEntry(entry);
            if (patterns.Count > 0)
            {
                return patterns;
            }
        }

        foreach (var link in moduleDownloadLinks.Links)
        {
            var resourceId = link.ReadString("resource_id");
            var resourceGroup = link.ReadString("resource_group");
            if (!string.Equals(resourceId, RuntimeDataGameProfileKeys.ExcludeList, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(resourceGroup, RuntimeDataGameProfileKeys.ExcludeList, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var patterns = ParseDictionaryEntry(link.RawValues);
            if (patterns.Count > 0)
            {
                return patterns;
            }
        }

        return Array.Empty<string>();
    }

    private static IReadOnlyList<string> ParseEntry(object? entry)
    {
        return entry switch
        {
            null => Array.Empty<string>(),
            string raw => InstallExcludeListPatternParser.Parse(raw),
            IReadOnlyDictionary<string, object?> row => ParseDictionaryEntry(row),
            IDictionary<string, object?> mutableRow => ParseDictionaryEntry(ToReadOnlyDictionary(mutableRow)),
            IEnumerable<string> tokens => InstallExcludeListPatternParser.Normalize(tokens),
            IEnumerable<object?> tokens => InstallExcludeListPatternParser.Normalize(tokens.Select(ToTokenString)),
            _ => InstallExcludeListPatternParser.Parse(entry.ToString())
        };
    }

    private static IReadOnlyList<string> ParseDictionaryEntry(IReadOnlyDictionary<string, object?> row)
    {
        var excludeRaw = ReadString(row, RuntimeDataGameProfileKeys.ExcludeList);
        if (!string.IsNullOrWhiteSpace(excludeRaw))
        {
            return InstallExcludeListPatternParser.Parse(excludeRaw);
        }

        if (row.TryGetValue("patterns", out var patternsValue))
        {
            var parsed = ParseEntry(patternsValue);
            if (parsed.Count > 0)
            {
                return parsed;
            }
        }

        var filenameRaw = ReadString(row, "filename");
        if (!string.IsNullOrWhiteSpace(filenameRaw))
        {
            return InstallExcludeListPatternParser.Parse(filenameRaw);
        }

        var valueRaw = ReadString(row, "value");
        if (!string.IsNullOrWhiteSpace(valueRaw))
        {
            return InstallExcludeListPatternParser.Parse(valueRaw);
        }

        return Array.Empty<string>();
    }

    private static string ReadString(IReadOnlyDictionary<string, object?> row, string key)
    {
        if (!row.TryGetValue(key, out var value) || value is null)
        {
            return "";
        }

        return value.ToString()?.Trim() ?? "";
    }

    private static string ToTokenString(object? value)
    {
        return value?.ToString() ?? "";
    }

    private static IReadOnlyDictionary<string, object?> ToReadOnlyDictionary(IDictionary<string, object?> source)
    {
        var copy = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in source)
        {
            copy[pair.Key] = pair.Value;
        }

        return copy;
    }
}
