using System.Globalization;
using OptiClick.Core.Games;
using OptiClick.Core.Runtime;
using OptiClick.Wpf.Shell.Scan;
using OptiClick.Wpf.Shell.RuntimeData;

namespace OptiClick.Wpf.Shell.Games;

public sealed class RuntimeDataShellGameMapper : IRuntimeDataShellGameMapper
{
    private readonly IGpuBundleInstallMetadataProvider _gpuBundleMetadataProvider;

    public RuntimeDataShellGameMapper()
        : this(NullGpuBundleInstallMetadataProvider.Instance)
    {
    }

    public RuntimeDataShellGameMapper(IGpuBundleInstallMetadataProvider gpuBundleMetadataProvider)
    {
        _gpuBundleMetadataProvider = gpuBundleMetadataProvider ?? NullGpuBundleInstallMetadataProvider.Instance;
    }

    public ShellGameCatalog Map(RemoteRuntimeData runtimeData, AppLanguage language)
    {
        if (runtimeData is null || runtimeData.GameMaster.Count == 0)
        {
            return ShellGameCatalog.Empty;
        }

        var mapped = new List<ShellGameCardModel>();
        var matchRules = new List<ShellGameMatchRule>();
        var messageTemplates = ParseMessageTemplates(runtimeData.MessageCenter);
        var messageBindings = ParseMessageBindings(runtimeData.MessageBinding);
        var gameGroups = runtimeData.GameMaster
            .Where(static game => game is not null)
            .GroupBy(
                static game => (game!.GameId ?? "").Trim(),
                StringComparer.OrdinalIgnoreCase);

        foreach (var group in gameGroups)
        {
            var gameId = (group.Key ?? "").Trim();
            if (string.IsNullOrWhiteSpace(gameId))
            {
                continue;
            }

            var hasMerged = _gpuBundleMetadataProvider.TryGetMetadata(gameId, out var mergedMetadata);
            if (!hasMerged)
            {
                continue;
            }

            if (!mergedMetadata.GpuBundleLoaded || !mergedMetadata.GpuBundleSupported)
            {
                continue;
            }

            var preferred = SelectPreferredProfileRow(group);
            var matchExe = CoalesceTrimmed(
                preferred.MatchExe,
                group.Select(static profile => profile!.MatchExe).ToArray());
            if (string.IsNullOrWhiteSpace(matchExe))
            {
                continue;
            }

            var gameNameEn = CoalesceTrimmed(
                preferred.GameNameEn,
                group.Select(static profile => profile!.GameNameEn).ToArray());
            var gameNameKr = CoalesceTrimmed(
                preferred.GameNameKr,
                group.Select(static profile => profile!.GameNameKr).ToArray());
            var displayName = SelectDisplayName(language, gameNameEn, gameNameKr, gameId, matchExe);
            var coverUrl = CoalesceTrimmed(
                preferred.CoverUrl,
                group.Select(static profile => profile!.CoverUrl).ToArray());
            var coverSteamAppId = CoalesceTrimmed(
                preferred.CoverSteamAppId,
                group.Select(static profile => profile!.CoverSteamAppId).ToArray());
            var resolvedGpuVendor = CoalesceTrimmed(
                mergedMetadata.GpuBundleVendor,
                preferred.GpuBundleVendor);
            var boundInstallPreKr = ResolveStagePopupText(
                messageTemplates,
                messageBindings,
                stage: "install_pre",
                gameId: gameId,
                gpuVendor: resolvedGpuVendor,
                lang: "ko");
            var boundInstallPreEn = ResolveStagePopupText(
                messageTemplates,
                messageBindings,
                stage: "install_pre",
                gameId: gameId,
                gpuVendor: resolvedGpuVendor,
                lang: "en");
            var boundInstallPostKr = ResolveStagePopupText(
                messageTemplates,
                messageBindings,
                stage: "install_post",
                gameId: gameId,
                gpuVendor: resolvedGpuVendor,
                lang: "ko");
            var boundInstallPostEn = ResolveStagePopupText(
                messageTemplates,
                messageBindings,
                stage: "install_post",
                gameId: gameId,
                gpuVendor: resolvedGpuVendor,
                lang: "en");
            var externalGuidePreUrl = ResolveExternalGuideUrl(
                messageTemplates,
                messageBindings,
                stage: "install_pre",
                gameId: gameId,
                gpuVendor: resolvedGpuVendor,
                language: language);
            var externalGuidePostUrl = ResolveExternalGuideUrl(
                messageTemplates,
                messageBindings,
                stage: "install_post",
                gameId: gameId,
                gpuVendor: resolvedGpuVendor,
                language: language);
            var runtimeExcludeRaw = (preferred.ExcludeListRaw ?? "").Trim();
            var runtimeExcludePatterns = preferred.ExcludeListPatterns ?? Array.Empty<string>();
            var metadataExcludeRaw = (mergedMetadata.ExcludeListRaw ?? "").Trim();
            var metadataExcludePatterns = mergedMetadata.ExcludeListPatterns ?? Array.Empty<string>();
            var resolvedExcludeRaw = !string.IsNullOrWhiteSpace(runtimeExcludeRaw)
                ? runtimeExcludeRaw
                : metadataExcludeRaw;
            var resolvedExcludePatterns = runtimeExcludePatterns.Count > 0
                ? runtimeExcludePatterns
                : metadataExcludePatterns;

            var gameCard = new ShellGameCardModel
            {
                GameId = gameId,
                MatchExe = matchExe,
                DisplayName = displayName,
                GameNameEn = gameNameEn,
                GameNameKr = gameNameKr,
                CoverSteamAppId = coverSteamAppId,
                CoverUrl = coverUrl,
                SupportedGpu = (preferred.SupportedGpu ?? "").Trim(),
                Enabled = preferred.Enabled,
                SupportIntel = preferred.SupportIntel,
                SupportAmd = preferred.SupportAmd,
                SupportNvidia = preferred.SupportNvidia,
                GpuBundleLoaded = mergedMetadata.GpuBundleLoaded,
                GpuBundleSupported = mergedMetadata.GpuBundleSupported,
                GpuProfileId = (mergedMetadata.GpuProfileId ?? "").Trim(),
                GpuBundleVendor = (mergedMetadata.GpuBundleVendor ?? "").Trim(),
                GpuBundleKey = (mergedMetadata.GpuBundleKey ?? "").Trim(),
                GpuGroup = (mergedMetadata.GpuGroup ?? "").Trim(),
                Fsr4 = mergedMetadata.Fsr4 ?? Fsr4ManifestPolicy.Disabled,
                OptiScalerDllName = (mergedMetadata.OptiScalerDllName ?? "").Trim(),
                ReframeworkUrl = (mergedMetadata.ReFrameworkUrl ?? "").Trim(),
                SpecialK = (mergedMetadata.SpecialK ?? "").Trim(),
                UltimateAsiLoader = mergedMetadata.UltimateAsiLoader,
                OptiPatcher = mergedMetadata.OptiPatcher,
                Unreal5 = mergedMetadata.Unreal5,
                RtssOverlay = mergedMetadata.RtssOverlay,
                ExtraBundle = (mergedMetadata.ExtraBundle ?? "").Trim(),
                InstallPreKr = CoalesceTrimmed(boundInstallPreKr, preferred.InstallPreKr),
                InstallPreEn = CoalesceTrimmed(boundInstallPreEn, preferred.InstallPreEn),
                InstallPostKr = CoalesceTrimmed(boundInstallPostKr, preferred.InstallPostKr),
                InstallPostEn = CoalesceTrimmed(boundInstallPostEn, preferred.InstallPostEn),
                GuideUrl = (preferred.GuideUrl ?? "").Trim(),
                ExternalGuidePreUrl = externalGuidePreUrl,
                ExternalGuidePostUrl = externalGuidePostUrl,
                ExternalGuideUrl = CoalesceTrimmed(externalGuidePreUrl, externalGuidePostUrl),
                ExternalGuideUrlTrigger = !string.IsNullOrWhiteSpace(externalGuidePreUrl)
                    ? "install_pre"
                    : !string.IsNullOrWhiteSpace(externalGuidePostUrl)
                        ? "install_post"
                        : "",
                ExcludeListRaw = resolvedExcludeRaw,
                ExcludeListPatterns = resolvedExcludePatterns,
                InstallMetadata = mergedMetadata,
                RuntimeDataValues = preferred.RawValues
            };
            mapped.Add(gameCard);
            matchRules.AddRange(CreateMatchRules(group, gameId, gameCard));
        }

        return new ShellGameCatalog
        {
            Games = mapped,
            MatchRules = matchRules
        };
    }

    private static IReadOnlyList<ShellGameMatchRule> CreateMatchRules(
        IEnumerable<RuntimeDataGameProfile?> group,
        string gameId,
        ShellGameCardModel gameCard)
    {
        var rules = new List<ShellGameMatchRule>();
        var seenRuleKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var safeGameId = (gameId ?? "").Trim();
        foreach (var profile in group)
        {
            if (profile is null)
            {
                continue;
            }

            var requiredFiles = MatchExePatternParser.ParseRequiredFiles(profile.MatchExe);
            if (requiredFiles.Count == 0)
            {
                continue;
            }

            var executableCandidates = MatchExePatternParser.ExtractExecutableCandidates(requiredFiles);
            if (executableCandidates.Count == 0)
            {
                continue;
            }

            var ruleKey = MatchExePatternParser.BuildMatchRuleKey(requiredFiles);
            if (string.IsNullOrWhiteSpace(ruleKey))
            {
                continue;
            }

            var scopedRuleKey = $"{safeGameId}::{ruleKey}";
            if (!seenRuleKeys.Add(scopedRuleKey))
            {
                continue;
            }

            rules.Add(new ShellGameMatchRule
            {
                GameId = safeGameId,
                MatchRuleKey = ruleKey,
                RequiredFiles = requiredFiles,
                ExecutableCandidates = executableCandidates,
                Game = gameCard
            });
        }

        return rules;
    }

    private static string SelectDisplayName(
        AppLanguage language,
        string gameNameEn,
        string gameNameKr,
        string gameId,
        string matchExe)
    {
        if (language == AppLanguage.Korean)
        {
            if (!string.IsNullOrWhiteSpace(gameNameKr))
            {
                return gameNameKr;
            }

            if (!string.IsNullOrWhiteSpace(gameNameEn))
            {
                return gameNameEn;
            }
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(gameNameEn))
            {
                return gameNameEn;
            }

            if (!string.IsNullOrWhiteSpace(gameNameKr))
            {
                return gameNameKr;
            }
        }

        if (!string.IsNullOrWhiteSpace(gameId))
        {
            return gameId;
        }

        return matchExe;
    }

    private static RuntimeDataGameProfile SelectPreferredProfileRow(IEnumerable<RuntimeDataGameProfile?> group)
    {
        RuntimeDataGameProfile? selected = null;
        var bestScore = int.MinValue;
        foreach (var candidate in group)
        {
            if (candidate is null)
            {
                continue;
            }

            var score = CalculateScore(candidate);
            if (selected is null || score > bestScore)
            {
                selected = candidate;
                bestScore = score;
            }
        }

        return selected ?? new RuntimeDataGameProfile();
    }

    private static int CalculateScore(RuntimeDataGameProfile profile)
    {
        var score = 0;
        if (!string.IsNullOrWhiteSpace((profile.CoverUrl ?? "").Trim()))
        {
            score += 16;
        }
        else if (!string.IsNullOrWhiteSpace((profile.CoverSteamAppId ?? "").Trim()))
        {
            score += 12;
        }

        if (!string.IsNullOrWhiteSpace((profile.MatchExe ?? "").Trim()))
        {
            score += 8;
        }

        if (profile.Enabled)
        {
            score += 4;
        }

        if (!string.IsNullOrWhiteSpace((profile.GameNameEn ?? "").Trim())
            || !string.IsNullOrWhiteSpace((profile.GameNameKr ?? "").Trim()))
        {
            score += 2;
        }

        return score;
    }

    private static IReadOnlyDictionary<string, MessageTemplate> ParseMessageTemplates(
        IReadOnlyList<RuntimeDataMessageRow>? rows)
    {
        if (rows is null || rows.Count == 0)
        {
            return new Dictionary<string, MessageTemplate>(StringComparer.OrdinalIgnoreCase);
        }

        var map = new Dictionary<string, MessageTemplate>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows)
        {
            var messageId = RuntimeDataRowReader.GetString(row, "message_id");
            if (string.IsNullOrWhiteSpace(messageId))
            {
                continue;
            }

            map[messageId] = new MessageTemplate
            {
                Category = NormalizeKey(RuntimeDataRowReader.GetString(row, "category"), ""),
                Korean = RuntimeDataRowReader.GetString(row, "ko"),
                English = RuntimeDataRowReader.GetString(row, "en"),
                Url = RuntimeDataRowReader.GetString(row, "url")
            };
        }

        return map;
    }

    private static IReadOnlyList<MessageBinding> ParseMessageBindings(
        IReadOnlyList<RuntimeDataMessageRow>? rows)
    {
        if (rows is null || rows.Count == 0)
        {
            return Array.Empty<MessageBinding>();
        }

        var list = new List<MessageBinding>();
        foreach (var row in rows)
        {
            var messageId = RuntimeDataRowReader.GetString(row, "message_id");
            var stage = NormalizeKey(RuntimeDataRowReader.GetString(row, "stage"), "");
            if (string.IsNullOrWhiteSpace(messageId) || string.IsNullOrWhiteSpace(stage))
            {
                continue;
            }

            var priorityRaw = RuntimeDataRowReader.GetString(row, "priority");
            if (!int.TryParse(priorityRaw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var priority))
            {
                priority = 100;
            }

            list.Add(new MessageBinding
            {
                GameId = NormalizeKey(RuntimeDataRowReader.GetString(row, "game_id"), "*"),
                GpuVendor = NormalizeKey(RuntimeDataRowReader.GetString(row, "gpu_vendor"), "all"),
                Stage = stage,
                MessageId = messageId,
                Priority = priority
            });
        }

        return list;
    }

    private static string ResolveStagePopupText(
        IReadOnlyDictionary<string, MessageTemplate> templates,
        IReadOnlyList<MessageBinding> bindings,
        string stage,
        string gameId,
        string gpuVendor,
        string lang)
    {
        if (templates.Count == 0 || bindings.Count == 0)
        {
            return "";
        }

        var normalizedStage = NormalizeKey(stage, "");
        var normalizedGameId = NormalizeKey(gameId, "");
        var normalizedGpuVendorForMatch = NormalizeKey(gpuVendor, "default");
        var normalizedGpuVendorForRank = NormalizeKey(gpuVendor, "");

        var matches = bindings
            .Where(binding => IsBindingMatch(
                binding,
                normalizedStage,
                normalizedGameId,
                normalizedGpuVendorForMatch))
            .OrderBy(static binding => binding.Priority)
            .ThenBy(binding => GameExactRank(binding, normalizedGameId))
            .ThenBy(binding => VendorExactRank(binding, normalizedGpuVendorForRank))
            .ToArray();

        if (matches.Length == 0)
        {
            return "";
        }

        var textParts = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var useKorean = NormalizeKey(lang, "en") == "ko";
        foreach (var binding in matches)
        {
            if (!templates.TryGetValue(binding.MessageId, out var template))
            {
                continue;
            }

            if (!string.Equals(template.Category, "popup", StringComparison.Ordinal))
            {
                continue;
            }

            var localizedText = (useKorean ? template.Korean : template.English).Trim();
            if (string.IsNullOrWhiteSpace(localizedText) || !seen.Add(localizedText))
            {
                continue;
            }

            textParts.Add(localizedText);
        }

        return textParts.Count == 0 ? "" : string.Join("[P]", textParts);
    }

    private static string ResolveExternalGuideUrl(
        IReadOnlyDictionary<string, MessageTemplate> templates,
        IReadOnlyList<MessageBinding> bindings,
        string stage,
        string gameId,
        string gpuVendor,
        AppLanguage language)
    {
        if (templates.Count == 0 || bindings.Count == 0)
        {
            return "";
        }

        var normalizedGameId = NormalizeKey(gameId, "");
        var normalizedGpuVendorForMatch = NormalizeKey(gpuVendor, "default");
        var normalizedGpuVendorForRank = NormalizeKey(gpuVendor, "");
        var normalizedStage = NormalizeKey(stage, "");
        var candidates = new List<ExternalGuideUrlCandidate>();

        foreach (var binding in bindings.Where(binding => IsBindingMatch(
                     binding,
                     normalizedStage,
                     normalizedGameId,
                     normalizedGpuVendorForMatch)))
        {
            if (!templates.TryGetValue(binding.MessageId, out var template))
            {
                continue;
            }

            if (!string.Equals(template.Category, "url", StringComparison.Ordinal))
            {
                continue;
            }

            var rawUrl = ResolveLocalizedUrl(template, language);
            if (!ExternalMessageUrlValidator.TryNormalizeHttpUrl(rawUrl, out var normalizedUrl))
            {
                continue;
            }

            candidates.Add(new ExternalGuideUrlCandidate(
                normalizedUrl,
                binding.MessageId,
                binding.Priority,
                GameExactRank(binding, normalizedGameId),
                VendorExactRank(binding, normalizedGpuVendorForRank)));
        }

        var selected = candidates
            .OrderBy(static candidate => candidate.Priority)
            .ThenBy(static candidate => candidate.GameRank)
            .ThenBy(static candidate => candidate.VendorRank)
            .ThenBy(static candidate => candidate.MessageId, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();

        return selected?.Url ?? "";
    }

    private static bool IsBindingMatch(
        MessageBinding binding,
        string normalizedStage,
        string normalizedGameId,
        string normalizedGpuVendorForMatch)
    {
        if (!string.Equals(binding.Stage, normalizedStage, StringComparison.Ordinal))
        {
            return false;
        }

        if (!string.Equals(binding.GameId, "*", StringComparison.Ordinal)
            && !string.Equals(binding.GameId, normalizedGameId, StringComparison.Ordinal))
        {
            return false;
        }

        return string.Equals(binding.GpuVendor, "all", StringComparison.Ordinal)
               || string.Equals(binding.GpuVendor, normalizedGpuVendorForMatch, StringComparison.Ordinal);
    }

    private static int GameExactRank(MessageBinding binding, string normalizedGameId)
    {
        return string.Equals(binding.GameId, normalizedGameId, StringComparison.Ordinal) ? 0 : 1;
    }

    private static int VendorExactRank(MessageBinding binding, string normalizedGpuVendorForRank)
    {
        return string.Equals(binding.GpuVendor, normalizedGpuVendorForRank, StringComparison.Ordinal) ? 0 : 1;
    }

    private static string NormalizeKey(string value, string fallback)
    {
        var normalized = (value ?? "").Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return fallback;
        }

        return normalized;
    }

    private static string CoalesceTrimmed(string? preferred, params string?[] fallbacks)
    {
        var preferredValue = (preferred ?? "").Trim();
        if (!string.IsNullOrWhiteSpace(preferredValue))
        {
            return preferredValue;
        }

        foreach (var fallback in fallbacks)
        {
            var normalized = (fallback ?? "").Trim();
            if (!string.IsNullOrWhiteSpace(normalized))
            {
                return normalized;
            }
        }

        return "";
    }

    private static string ResolveLocalizedUrl(MessageTemplate template, AppLanguage language)
    {
        return language == AppLanguage.Korean
            ? CoalesceTrimmed(template.Korean, template.English, template.Url)
            : CoalesceTrimmed(template.English, template.Korean, template.Url);
    }

    private sealed record MessageTemplate
    {
        public string Category { get; init; } = "";
        public string Korean { get; init; } = "";
        public string English { get; init; } = "";
        public string Url { get; init; } = "";
    }

    private sealed record MessageBinding
    {
        public string GameId { get; init; } = "";
        public string GpuVendor { get; init; } = "";
        public string Stage { get; init; } = "";
        public string MessageId { get; init; } = "";
        public int Priority { get; init; } = 100;
    }

    private sealed record ExternalGuideUrlCandidate(
        string Url,
        string MessageId,
        int Priority,
        int GameRank,
        int VendorRank);
}
