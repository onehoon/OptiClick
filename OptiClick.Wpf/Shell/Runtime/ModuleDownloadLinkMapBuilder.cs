using System.Text.Json;
using System.IO;
using OptiClick.Wpf.Shell.RuntimeData;

namespace OptiClick.Wpf.Shell.Runtime;

public sealed class ModuleDownloadLinkMapBuilder
{
    public IReadOnlyDictionary<string, object?> Build(IReadOnlyList<RuntimeDataResourceRow>? rows)
    {
        var map = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        if (rows is null || rows.Count == 0)
        {
            return map;
        }

        foreach (var row in rows)
        {
            var entry = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            string key = "";
            string alias = "";

            foreach (var (column, value) in row.Values)
            {
                var converted = ConvertJsonElement(value);
                entry[column] = converted;

                if (string.IsNullOrWhiteSpace(key)
                    && IsKeyColumn(column))
                {
                    key = (converted?.ToString() ?? "").Trim();
                }

                if (string.IsNullOrWhiteSpace(alias)
                    && column.Equals("alias", StringComparison.OrdinalIgnoreCase))
                {
                    alias = (converted?.ToString() ?? "").Trim();
                }
            }

            if (!entry.ContainsKey("url"))
            {
                var url = ReadFallbackString(entry, "download_url");
                if (string.IsNullOrWhiteSpace(url))
                {
                    url = ReadFallbackString(entry, "source_url");
                }

                if (!string.IsNullOrWhiteSpace(url))
                {
                    entry["url"] = url;
                }
            }

            if (!entry.ContainsKey("filename"))
            {
                var filename = ReadFallbackString(entry, "file_name");
                if (!string.IsNullOrWhiteSpace(filename))
                {
                    entry["filename"] = filename;
                }
            }

            AddKeyIfNotEmpty(map, key, entry);
            AddKeyIfNotEmpty(map, ReadFallbackString(entry, "resource_id"), entry);
            AddKeyIfNotEmpty(map, ReadFallbackString(entry, "resource_group"), entry);
            AddKeyIfNotEmpty(map, alias, entry);
            AddExtraBundleAliasKeys(map, entry);
        }

        return map;
    }

    private static bool IsKeyColumn(string column)
    {
        return column.Equals("key", StringComparison.OrdinalIgnoreCase)
               || column.Equals("resource_id", StringComparison.OrdinalIgnoreCase)
               || column.Equals("resource_group", StringComparison.OrdinalIgnoreCase)
               || column.Equals("resource_key", StringComparison.OrdinalIgnoreCase)
               || column.Equals("id", StringComparison.OrdinalIgnoreCase)
               || column.Equals("name", StringComparison.OrdinalIgnoreCase);
    }

    private static void AddKeyIfNotEmpty(
        IDictionary<string, object?> map,
        string key,
        object? value)
    {
        if (!string.IsNullOrWhiteSpace(key))
        {
            map[key] = value;
        }
    }

    private static void AddExtraBundleAliasKeys(
        IDictionary<string, object?> map,
        IReadOnlyDictionary<string, object?> entry)
    {
        var resourceGroup = ReadFallbackString(entry, "resource_group");
        var resourceId = ReadFallbackString(entry, "resource_id");
        if (!string.Equals(resourceGroup, "extra_bundle", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(resourceId, "extra_bundle", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        AddKeyIfNotEmpty(map, ReadFallbackString(entry, "name"), entry);

        var filename = ReadFallbackString(entry, "filename");
        if (!string.IsNullOrWhiteSpace(filename))
        {
            AddKeyIfNotEmpty(map, Path.GetFileNameWithoutExtension(filename), entry);
        }
    }

    private static object? ConvertJsonElement(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null or JsonValueKind.Undefined => null,
            JsonValueKind.Number when value.TryGetInt64(out var longValue) => longValue,
            JsonValueKind.Number when value.TryGetDouble(out var doubleValue) => doubleValue,
            _ => value.ToString()
        };
    }

    private static string ReadFallbackString(IReadOnlyDictionary<string, object?> entry, string key)
    {
        if (!entry.TryGetValue(key, out var value) || value is null)
        {
            return "";
        }

        return value.ToString()?.Trim() ?? "";
    }
}
