using OptiClick.Core.Install;
using System.Text.Json;
using OptiClick.Core.Games.Support;

namespace OptiClick.Core.RuntimeData;

public sealed class RemoteRuntimeDataParser : IRemoteRuntimeDataParser
{
    public RemoteRuntimeDataParseResult Parse(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return RemoteRuntimeDataParseResult.Failure("empty_input");
        }

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json);
        }
        catch (JsonException)
        {
            return RemoteRuntimeDataParseResult.Failure("invalid_json");
        }

        using (document)
        {
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return RemoteRuntimeDataParseResult.Failure("payload_not_object");
            }

            if (!root.TryGetProperty("schema_version", out _))
            {
                return RemoteRuntimeDataParseResult.Failure("schema_version_missing");
            }

            var schemaVersion = ReadInt(root, "schema_version");
            if (schemaVersion != 1)
            {
                return RemoteRuntimeDataParseResult.Failure("unsupported_schema_version");
            }

            if (!root.TryGetProperty("data", out var dataElement) || dataElement.ValueKind != JsonValueKind.Object)
            {
                return RemoteRuntimeDataParseResult.Failure("data_missing_or_not_object");
            }

            foreach (var sectionName in RuntimeDataSectionNames.RequiredSections)
            {
                if (!dataElement.TryGetProperty(sectionName, out var sectionElement))
                {
                    return RemoteRuntimeDataParseResult.Failure(
                        "missing_required_section",
                        $"missing section: {sectionName}");
                }

                if (sectionElement.ValueKind != JsonValueKind.Array)
                {
                    return RemoteRuntimeDataParseResult.Failure(
                        "section_not_array",
                        $"section is not an array: {sectionName}");
                }
            }

            var gameMaster = ParseGames(dataElement.GetProperty(RuntimeDataSectionNames.GameMaster));
            var runtimeData = new RemoteRuntimeData
            {
                SchemaVersion = schemaVersion,
                GeneratedAt = ReadString(root, "generated_at"),
                GameMaster = gameMaster,
                GameIniProfile = ParseProfileRows(dataElement.GetProperty(RuntimeDataSectionNames.GameIniProfile)),
                GameUnrealIniProfile = ParseProfileRows(dataElement.GetProperty(RuntimeDataSectionNames.GameUnrealIniProfile)),
                GameXmlProfile = ParseProfileRows(dataElement.GetProperty(RuntimeDataSectionNames.GameXmlProfile)),
                GameJsonProfile = ParseProfileRows(dataElement.GetProperty(RuntimeDataSectionNames.GameJsonProfile)),
                EngineIniProfile = ParseProfileRows(dataElement.GetProperty(RuntimeDataSectionNames.EngineIniProfile)),
                RegistryProfile = ParseProfileRows(dataElement.GetProperty(RuntimeDataSectionNames.RegistryProfile)),
                ResourceMaster = ParseResourceRows(dataElement.GetProperty(RuntimeDataSectionNames.ResourceMaster)),
                MessageBinding = ParseMessageRows(dataElement.GetProperty(RuntimeDataSectionNames.MessageBinding)),
                MessageCenter = ParseMessageRows(dataElement.GetProperty(RuntimeDataSectionNames.MessageCenter)),
                NewGameSupport = ParseOptionalNewGameSupportRows(dataElement)
            };

            return RemoteRuntimeDataParseResult.Success(runtimeData);
        }
    }

    private static IReadOnlyList<RuntimeDataGameProfile> ParseGames(JsonElement gamesElement)
    {
        var games = new List<RuntimeDataGameProfile>();
        foreach (var element in gamesElement.EnumerateArray())
        {
            if (element.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var gameId = ReadString(element, "game_id");
            var gameNameEn = ReadString(element, "game_name_en");
            if (string.IsNullOrWhiteSpace(gameId) || string.IsNullOrWhiteSpace(gameNameEn))
            {
                continue;
            }

            var rawValues = CreateRawValuesMap(element);
            games.Add(new RuntimeDataGameProfile
            {
                GameId = gameId,
                MatchExe = ReadString(element, "match_exe"),
                GameNameKr = ReadString(element, "game_name_kr"),
                GameNameEn = gameNameEn,
                CoverSteamAppId = ReadString(element, "cover_steam_app_id"),
                CoverUrl = ReadString(element, "cover_url"),
                Enabled = ReadFlag(element, "enabled", defaultValue: true, nativeXefgMeansFalse: false),
                SupportedGpu = ReadString(element, "supported_gpu"),
                SupportIntel = ReadNullableFlag(element, "support_intel"),
                SupportAmd = ReadNullableFlag(element, "support_amd"),
                SupportNvidia = ReadNullableFlag(element, "support_nvidia"),
                GpuBundleLoaded = ReadNullableFlag(element, RuntimeDataGameProfile.GpuBundleLoadedKey),
                GpuBundleSupported = ReadNullableFlag(element, RuntimeDataGameProfile.GpuBundleSupportedKey),
                GpuProfileId = ReadString(element, RuntimeDataGameProfile.GpuProfileIdKey),
                GpuBundleVendor = ReadString(element, RuntimeDataGameProfile.GpuBundleVendorKey),
                OptiScalerDllName = "",
                ReframeworkUrl = ReadString(element, "reframework_url"),
                SpecialK = ReadString(element, "specialk"),
                UltimateAsiLoader = ReadFlag(element, "ultimate_asi_loader", defaultValue: false, nativeXefgMeansFalse: true),
                OptiPatcher = ReadFlag(element, "optipatcher", defaultValue: false, nativeXefgMeansFalse: true),
                Unreal5 = ReadFlag(element, "unreal5", defaultValue: false, nativeXefgMeansFalse: true),
                RtssOverlay = ReadFlag(element, "rtss_overlay", defaultValue: false, nativeXefgMeansFalse: true),
                ExtraBundle = ReadString(element, "extra_bundle"),
                InstallPreKr = ReadString(element, RuntimeDataGameProfileKeys.InstallPreKr),
                InstallPreEn = ReadString(element, RuntimeDataGameProfileKeys.InstallPreEn),
                InstallPostKr = ReadString(element, RuntimeDataGameProfileKeys.InstallPostKr),
                InstallPostEn = ReadString(element, RuntimeDataGameProfileKeys.InstallPostEn),
                GuideUrl = ReadString(element, RuntimeDataGameProfileKeys.GuideUrl),
                ExcludeListRaw = ReadExcludeListRaw(element),
                ExcludeListPatterns = ReadExcludeListPatterns(element),
                RawValues = rawValues
            });
        }

        return games;
    }

    private static IReadOnlyList<RuntimeDataProfileRow> ParseProfileRows(JsonElement sectionElement)
    {
        var rows = new List<RuntimeDataProfileRow>();
        foreach (var row in sectionElement.EnumerateArray())
        {
            if (row.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            rows.Add(new RuntimeDataProfileRow
            {
                Values = CreateRawValuesMap(row)
            });
        }

        return rows;
    }

    private static IReadOnlyList<RuntimeDataResourceRow> ParseResourceRows(JsonElement sectionElement)
    {
        var rows = new List<RuntimeDataResourceRow>();
        foreach (var row in sectionElement.EnumerateArray())
        {
            if (row.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            rows.Add(new RuntimeDataResourceRow
            {
                Values = CreateRawValuesMap(row)
            });
        }

        return rows;
    }

    private static IReadOnlyList<RuntimeDataMessageRow> ParseMessageRows(JsonElement sectionElement)
    {
        var rows = new List<RuntimeDataMessageRow>();
        foreach (var row in sectionElement.EnumerateArray())
        {
            if (row.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            rows.Add(new RuntimeDataMessageRow
            {
                Values = CreateRawValuesMap(row)
            });
        }

        return rows;
    }

    private static IReadOnlyList<RuntimeDataNewGameSupportRow> ParseOptionalNewGameSupportRows(JsonElement dataElement)
    {
        if (!dataElement.TryGetProperty(RuntimeDataSectionNames.NewGameSupport, out var sectionElement)
            || sectionElement.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<RuntimeDataNewGameSupportRow>();
        }

        var rows = new List<RuntimeDataNewGameSupportRow>();
        foreach (var row in sectionElement.EnumerateArray())
        {
            if (row.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            rows.Add(new RuntimeDataNewGameSupportRow
            {
                Values = CreateRawValuesMap(row)
            });
        }

        return rows;
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

    private static string ReadString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.String)
        {
            return "";
        }

        return property.GetString()?.Trim() ?? "";
    }

    private static int ReadInt(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return 0;
        }

        if (property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out var number))
        {
            return number;
        }

        if (property.ValueKind == JsonValueKind.String && int.TryParse(property.GetString(), out var parsed))
        {
            return parsed;
        }

        return 0;
    }

    private static bool ReadFlag(JsonElement element, string propertyName, bool defaultValue, bool nativeXefgMeansFalse)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return defaultValue;
        }

        return SupportFlagParser.Parse(
            ParseJsonValue(property),
            emptyDefault: defaultValue,
            unknownDefault: true,
            nativeXefgMeansFalse: nativeXefgMeansFalse);
    }

    private static bool? ReadNullableBool(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        if (property.ValueKind == JsonValueKind.True)
        {
            return true;
        }

        if (property.ValueKind == JsonValueKind.False)
        {
            return false;
        }

        if (property.ValueKind == JsonValueKind.String && bool.TryParse(property.GetString(), out var parsed))
        {
            return parsed;
        }

        return null;
    }

    private static bool? ReadNullableFlag(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        return SupportFlagParser.ParseNullable(ParseJsonValue(property), nativeXefgMeansFalse: false);
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
            _ => value.ToString()
        };
    }

    private static string ReadExcludeListRaw(JsonElement element)
    {
        if (!element.TryGetProperty(RuntimeDataGameProfileKeys.ExcludeList, out var property))
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

    private static IReadOnlyList<string> ReadExcludeListPatterns(JsonElement element)
    {
        if (!element.TryGetProperty(RuntimeDataGameProfileKeys.ExcludeList, out var property))
        {
            return Array.Empty<string>();
        }

        if (property.ValueKind == JsonValueKind.Array)
        {
            return InstallExcludeListPatternParser.Normalize(
                property.EnumerateArray()
                    .Select(static item => item.ValueKind == JsonValueKind.String ? item.GetString() ?? "" : item.ToString())
                    .Select(static item => item.Trim()));
        }

        if (property.ValueKind == JsonValueKind.String)
        {
            return InstallExcludeListPatternParser.Parse(property.GetString());
        }

        return Array.Empty<string>();
    }
}
