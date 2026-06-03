using System.Text.Json;
using OptiClick.Wpf.Shell.RuntimeData;
using OptiClick.Wpf.Shell.Games.Support;

namespace OptiClick.Wpf.Shell.Games.GpuBundle;

public sealed class RemoteGpuBundleParser : IRemoteGpuBundleParser
{
    public RemoteGpuBundleParseResult Parse(
        string json,
        string? selectedGpuGroup = null,
        string? requestVendor = null,
        string? bundleKey = null,
        string? manifestVersion = null)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return RemoteGpuBundleParseResult.Failure("empty_input");
        }

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json);
        }
        catch (JsonException)
        {
            return RemoteGpuBundleParseResult.Failure("invalid_json");
        }

        using (document)
        {
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return RemoteGpuBundleParseResult.Failure("payload_not_object");
            }

            if (root.TryGetProperty("ok", out var okElement)
                && okElement.ValueKind == JsonValueKind.False)
            {
                return RemoteGpuBundleParseResult.Failure("bundle_not_ok");
            }

            var resolvedVendor = FirstNonEmpty(
                requestVendor,
                ReadString(root, "vendor"));
            var resolvedBundleKey = FirstNonEmpty(
                bundleKey,
                ReadString(root, "bundle_key"));
            var resolvedManifestVersion = FirstNonEmpty(
                manifestVersion,
                ReadString(root, "manifest_version"));
            var resolvedGroup = (selectedGpuGroup ?? "").Trim();

            var gamesSection = ResolveGamesSection(root, resolvedGroup, out var gamesResolveError);
            if (gamesSection.ValueKind == JsonValueKind.Undefined)
            {
                return RemoteGpuBundleParseResult.Failure(gamesResolveError);
            }

            var sharedRows = ParseSharedOptiScalerIniRows(root);
            var parsedGames = ParseGames(
                gamesSection,
                resolvedVendor,
                resolvedBundleKey,
                string.IsNullOrWhiteSpace(resolvedGroup) ? ReadFirstGroupName(root) : resolvedGroup,
                resolvedManifestVersion);

            return RemoteGpuBundleParseResult.Success(new RemoteGpuBundle
            {
                GamesByGameId = parsedGames,
                SharedOptiScalerIniRows = sharedRows,
                RawValues = CreateRawValuesMap(root)
            });
        }
    }

    private static JsonElement ResolveGamesSection(JsonElement root, string selectedGpuGroup, out string errorCode)
    {
        errorCode = "";
        if (!string.IsNullOrWhiteSpace(selectedGpuGroup))
        {
            if (!root.TryGetProperty("groups", out var groups)
                || groups.ValueKind != JsonValueKind.Object)
            {
                errorCode = "missing_groups";
                return default;
            }

            foreach (var property in groups.EnumerateObject())
            {
                if (!string.Equals(property.Name, selectedGpuGroup, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (property.Value.ValueKind != JsonValueKind.Object
                    || !property.Value.TryGetProperty("games", out var groupGames))
                {
                    errorCode = "missing_games_section";
                    return default;
                }

                return groupGames;
            }

            errorCode = "missing_selected_group";
            return default;
        }

        if (root.TryGetProperty("games", out var games))
        {
            return games;
        }

        if (root.TryGetProperty("groups", out var fallbackGroups)
            && fallbackGroups.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in fallbackGroups.EnumerateObject())
            {
                if (property.Value.ValueKind == JsonValueKind.Object
                    && property.Value.TryGetProperty("games", out var groupGames))
                {
                    return groupGames;
                }
            }
        }

        errorCode = "missing_games_section";
        return default;
    }

    private static string ReadFirstGroupName(JsonElement root)
    {
        if (!root.TryGetProperty("groups", out var groups)
            || groups.ValueKind != JsonValueKind.Object)
        {
            return "";
        }

        foreach (var property in groups.EnumerateObject())
        {
            return property.Name.Trim();
        }

        return "";
    }

    private static IReadOnlyDictionary<string, RemoteGpuBundleGameEntry> ParseGames(
        JsonElement gamesSection,
        string vendor,
        string bundleKey,
        string gpuGroup,
        string manifestVersion)
    {
        var map = new Dictionary<string, RemoteGpuBundleGameEntry>(StringComparer.OrdinalIgnoreCase);
        if (gamesSection.ValueKind == JsonValueKind.Array)
        {
            foreach (var row in gamesSection.EnumerateArray())
            {
                TryAddGameRow(map, row, null, vendor, bundleKey, gpuGroup, manifestVersion);
            }

            return map;
        }

        if (gamesSection.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in gamesSection.EnumerateObject())
            {
                TryAddGameRow(map, property.Value, property.Name, vendor, bundleKey, gpuGroup, manifestVersion);
            }
        }

        return map;
    }

    private static void TryAddGameRow(
        IDictionary<string, RemoteGpuBundleGameEntry> map,
        JsonElement row,
        string? gameIdFallback,
        string vendor,
        string bundleKey,
        string gpuGroup,
        string manifestVersion)
    {
        if (row.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        var gameId = FirstNonEmpty(ReadString(row, "game_id"), gameIdFallback).Trim();
        if (string.IsNullOrWhiteSpace(gameId))
        {
            return;
        }

        var installProfile = ParseInstallProfile(row);
        var entry = new RemoteGpuBundleGameEntry
        {
            GameId = gameId,
            ProfileId = ReadString(row, "profile_id"),
            BundleGpuVendor = vendor,
            BundleKey = bundleKey,
            BundleGpuGroup = gpuGroup,
            BundleManifestVersion = manifestVersion,
            InstallProfile = installProfile,
            LocalOptiScalerIniRows = ParseLocalOptiScalerIniRows(row),
            RawValues = CreateRawValuesMap(row)
        };

        map[gameId] = entry;
    }

    private static RemoteGpuBundleInstallProfile ParseInstallProfile(JsonElement row)
    {
        if (!row.TryGetProperty("install_profile", out var profile)
            || profile.ValueKind != JsonValueKind.Object)
        {
            return new RemoteGpuBundleInstallProfile();
        }

        return new RemoteGpuBundleInstallProfile
        {
            Enabled = ReadBool(profile, "enabled", defaultValue: true),
            OptiScalerDllName = ReadOptionalInstallText(profile, "optiscaler_dll_name"),
            ReFrameworkUrl = ReadOptionalInstallText(profile, "reframework_url"),
            SpecialK = ReadOptionalInstallText(profile, "specialk"),
            ExtraBundle = ReadOptionalInstallText(profile, "extra_bundle"),
            ExcludeListRaw = ReadExcludeListRaw(profile),
            ExcludeListPatterns = ReadExcludeListPatterns(profile),
            UltimateAsiLoader = ReadBool(profile, "ultimate_asi_loader", defaultValue: false),
            OptiPatcher = ReadBool(profile, "optipatcher", defaultValue: false),
            Unreal5 = ReadBool(profile, "unreal5", defaultValue: false),
            RtssOverlay = ReadBool(profile, "rtss_overlay", defaultValue: false),
            RawValues = CreateRawValuesMap(profile)
        };
    }

    private static IReadOnlyList<RuntimeDataRawRow> ParseSharedOptiScalerIniRows(JsonElement root)
    {
        if (!root.TryGetProperty("profiles", out var profiles)
            || profiles.ValueKind != JsonValueKind.Object)
        {
            return [];
        }

        if (!profiles.TryGetProperty("optiscaler_ini", out var optiScalerIni)
            || optiScalerIni.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return ParseIniRows(optiScalerIni);
    }

    private static IReadOnlyList<RuntimeDataRawRow> ParseLocalOptiScalerIniRows(JsonElement row)
    {
        if (!row.TryGetProperty("optiscaler_ini", out var localRows)
            || localRows.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return ParseIniRows(localRows);
    }

    private static IReadOnlyList<RuntimeDataRawRow> ParseIniRows(JsonElement rowsElement)
    {
        var rows = new List<RuntimeDataRawRow>();
        foreach (var row in rowsElement.EnumerateArray())
        {
            if (row.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            rows.Add(new RuntimeDataRawRow
            {
                Values = CreateRawValuesMap(row)
            });
        }

        return rows;
    }

    private static bool ReadBool(JsonElement element, string propertyName, bool defaultValue)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return defaultValue;
        }

        return SupportFlagParser.Parse(ParseJsonValue(property), emptyDefault: defaultValue, unknownDefault: false, nativeXefgMeansFalse: false);
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

    private static string ReadOptionalInstallText(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return "";
        }

        return property.ValueKind switch
        {
            JsonValueKind.String => NormalizeOptionalInstallToken(property.GetString()),
            JsonValueKind.False => "",
            JsonValueKind.Null or JsonValueKind.Undefined => "",
            _ => NormalizeOptionalInstallToken(property.ToString())
        };
    }

    private static string NormalizeOptionalInstallToken(string? value)
    {
        var normalized = (value ?? "").Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return "";
        }

        if (string.Equals(normalized, "false", StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalized, "none", StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalized, "null", StringComparison.OrdinalIgnoreCase))
        {
            return "";
        }

        return normalized;
    }

    private static string ReadExcludeListRaw(JsonElement profile)
    {
        if (!profile.TryGetProperty(RuntimeDataGameProfileKeys.ExcludeList, out var property))
        {
            return "";
        }

        return property.ValueKind switch
        {
            JsonValueKind.String => property.GetString()?.Trim() ?? "",
            JsonValueKind.Array => string.Join(
                "|",
                property.EnumerateArray()
                    .Select(static item => item.ValueKind == JsonValueKind.String ? item.GetString() ?? "" : item.ToString())
                    .Select(static item => item.Trim())
                    .Where(static item => !string.IsNullOrWhiteSpace(item))),
            _ => property.ToString().Trim()
        };
    }

    private static IReadOnlyList<string> ReadExcludeListPatterns(JsonElement profile)
    {
        if (!profile.TryGetProperty(RuntimeDataGameProfileKeys.ExcludeList, out var property))
        {
            return Array.Empty<string>();
        }

        if (property.ValueKind == JsonValueKind.Array)
        {
            return ExcludeListPatternParser.Normalize(
                property.EnumerateArray()
                    .Select(static item => item.ValueKind == JsonValueKind.String ? item.GetString() ?? "" : item.ToString())
                    .Select(static item => item.Trim()));
        }

        if (property.ValueKind == JsonValueKind.String)
        {
            return ExcludeListPatternParser.Parse(property.GetString());
        }

        return Array.Empty<string>();
    }

    private static string FirstNonEmpty(params string?[] candidates)
    {
        foreach (var candidate in candidates)
        {
            var normalized = (candidate ?? "").Trim();
            if (!string.IsNullOrWhiteSpace(normalized))
            {
                return normalized;
            }
        }

        return "";
    }

    private static IReadOnlyDictionary<string, JsonElement> CreateRawValuesMap(JsonElement rowElement)
    {
        var map = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
        foreach (var property in rowElement.EnumerateObject())
        {
            map[property.Name] = property.Value.Clone();
        }

        return map;
    }
}
