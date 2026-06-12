using System.Text.Json;
using OptiClick.Core.Games.Support;

namespace OptiClick.Core.Games.GpuBundle;

public sealed class RemoteGpuBundleManifestParser : IRemoteGpuBundleManifestParser
{
    public RemoteGpuBundleManifestParseResult Parse(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return RemoteGpuBundleManifestParseResult.Failure("empty_input");
        }

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json);
        }
        catch (JsonException)
        {
            return RemoteGpuBundleManifestParseResult.Failure("invalid_json");
        }

        using (document)
        {
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return RemoteGpuBundleManifestParseResult.Failure("payload_not_object");
            }

            var rules = ParseRules(root);
            var fallback = ParseFallback(root);
            var manifestVersion = ReadString(root, "manifest_version");

            return RemoteGpuBundleManifestParseResult.Success(new RemoteGpuBundleManifest
            {
                Rules = rules,
                Fallback = fallback,
                ManifestVersion = manifestVersion
            });
        }
    }

    private static IReadOnlyList<RemoteGpuBundleManifestRule> ParseRules(JsonElement root)
    {
        if (!root.TryGetProperty("rules", out var rulesElement)
            || rulesElement.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var rules = new List<RemoteGpuBundleManifestRule>();
        var index = 0;
        foreach (var element in rulesElement.EnumerateArray())
        {
            if (element.ValueKind != JsonValueKind.Object)
            {
                index++;
                continue;
            }

            rules.Add(new RemoteGpuBundleManifestRule
            {
                Enabled = ReadBool(element, "enabled", true),
                Vendor = NormalizeVendor(ReadString(element, "vendor")),
                MatchMode = ReadString(element, "match_mode").ToLowerInvariant(),
                MatchValue = ReadString(element, "match_value"),
                BundleKey = ReadString(element, "bundle_key"),
                GpuGroup = ReadString(element, "gpu_group").ToLowerInvariant(),
                Priority = ReadInt(element, "priority", 100),
                SourceIndex = index,
                Fsr4 = ReadFsr4Policy(element)
            });
            index++;
        }

        return rules;
    }

    private static RemoteGpuBundleManifestFallback? ParseFallback(JsonElement root)
    {
        if (!root.TryGetProperty("fallback", out var fallbackElement)
            || fallbackElement.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        return new RemoteGpuBundleManifestFallback
        {
            Enabled = ReadBool(fallbackElement, "enabled", true),
            BundleKey = ReadString(fallbackElement, "bundle_key"),
            GpuGroup = ReadString(fallbackElement, "gpu_group").ToLowerInvariant()
        };
    }

    private static bool ReadBool(JsonElement element, string propertyName, bool defaultValue)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return defaultValue;
        }

        return SupportFlagParser.Parse(
            ParseJsonValue(property),
            emptyDefault: defaultValue,
            unknownDefault: defaultValue,
            nativeXefgMeansFalse: false);
    }

    private static int ReadInt(JsonElement element, string propertyName, int defaultValue)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return defaultValue;
        }

        if (property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out var number))
        {
            return number;
        }

        if (property.ValueKind == JsonValueKind.String
            && int.TryParse((property.GetString() ?? "").Trim(), out var parsed))
        {
            return parsed;
        }

        return defaultValue;
    }

    private static string ReadString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return "";
        }

        return property.ValueKind switch
        {
            JsonValueKind.String => property.GetString()?.Trim() ?? "",
            JsonValueKind.Null or JsonValueKind.Undefined => "",
            _ => property.ToString().Trim()
        };
    }

    private static Fsr4ManifestPolicy ReadFsr4Policy(JsonElement element)
    {
        if (!element.TryGetProperty("fsr4", out var property))
        {
            return Fsr4ManifestPolicy.Disabled;
        }

        if (property.ValueKind != JsonValueKind.String)
        {
            return Fsr4ManifestPolicy.Disabled;
        }

        var variant = NormalizeFsr4Variant(property.GetString());
        if (string.IsNullOrWhiteSpace(variant))
        {
            return Fsr4ManifestPolicy.Disabled;
        }

        return new Fsr4ManifestPolicy
        {
            Enabled = true,
            Variant = variant
        };
    }

    private static string NormalizeFsr4Variant(string? value)
    {
        return (value ?? "").Trim().ToLowerInvariant();
    }

    private static object ParseJsonValue(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString() ?? "",
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Number when value.TryGetInt64(out var longValue) => longValue,
            JsonValueKind.Number when value.TryGetDouble(out var doubleValue) => doubleValue,
            JsonValueKind.Null => "",
            _ => value.ToString()
        };
    }

    private static string NormalizeVendor(string vendor)
    {
        var text = (vendor ?? "").Trim().ToLowerInvariant();
        if (text.Contains("nvidia", StringComparison.Ordinal))
        {
            return "nvidia";
        }

        if (text.Contains("intel", StringComparison.Ordinal))
        {
            return "intel";
        }

        if (text.Contains("amd", StringComparison.Ordinal) || text.Contains("radeon", StringComparison.Ordinal))
        {
            return "amd";
        }

        return "";
    }
}
