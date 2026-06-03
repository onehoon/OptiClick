using OptiClick.Core.Models;

namespace OptiClick.Core.Games;

public static class ResourceEntryMapper
{
    public static ResourceEntry Map(IReadOnlyDictionary<string, string> row)
    {
        return new ResourceEntry
        {
            ResourceId = Get(row, "resource_id", "id", "resourceId"),
            ResourceGroup = Get(row, "resource_group", "group", "resourceGroup"),
            Name = Get(row, "name", "resource_name", "resourceName"),
            FileName = Get(row, "filename", "file_name", "fileName"),
            Url = Get(row, "url", "download_url", "downloadUrl"),
            Alias = Get(row, "alias", "aliases"),
            BundleKey = Get(row, "bundle_key", "bundleKey", "key")
        };
    }

    private static string Get(IReadOnlyDictionary<string, string> row, params string[] names)
    {
        foreach (var name in names)
        {
            if (row.TryGetValue(name, out var value))
            {
                return value.Trim();
            }
        }
        return "";
    }
}
