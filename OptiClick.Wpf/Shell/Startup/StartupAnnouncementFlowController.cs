using System.Globalization;
using System.Text.RegularExpressions;
using OptiClick.Core.Runtime;
using OptiClick.Wpf.Models;
using OptiClick.Wpf.Shell.RuntimeData;
using OptiClick.Wpf.Shell.Flow;

namespace OptiClick.Wpf.Shell.Startup;

public sealed record StartupAnnouncementFlowRequest
{
    public RemoteRuntimeData RuntimeData { get; init; } = RemoteRuntimeData.Empty;
    public AppLanguage Language { get; init; } = AppLanguage.English;
    public string SelectedGpuVendor { get; init; } = "";
}

public sealed record StartupAnnouncementFlowResult
{
    public bool ShouldShowDialog { get; init; }
    public AppDialogRequest? DialogRequest { get; init; }
    public string StartupUrl { get; init; } = "";
    public IReadOnlyList<StartupFlowLogEntry> Logs { get; init; } = [];
}

public sealed record StartupFlowLogEntry : IFlowLogEntry
{
    public string Level { get; init; } = "info";
    public string Category { get; init; } = "";
    public string Message { get; init; } = "";
    public Exception? Exception { get; init; }
}

public sealed class StartupAnnouncementFlowController
{
    private static readonly Regex ParagraphTokenRegex = new(@"\[(P|BR)\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex AnyMarkupTokenRegex = new(@"\[(?<token>[A-Z_]+)\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly HashSet<string> SupportedMarkupTokens = new(StringComparer.OrdinalIgnoreCase)
    {
        "RED",
        "END",
        "P",
        "BR",
        "DOT",
        "INDENT"
    };

    private readonly StartupNoticePresenter _presenter;

    public StartupAnnouncementFlowController(StartupNoticePresenter? presenter = null)
    {
        _presenter = presenter ?? new StartupNoticePresenter();
    }

    public StartupAnnouncementFlowResult Build(StartupAnnouncementFlowRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var logs = new List<StartupFlowLogEntry>();
        var runtimeData = request.RuntimeData ?? RemoteRuntimeData.Empty;
        var normalizedVendor = NormalizeKey(request.SelectedGpuVendor, "default");

        var newGameBullets = BuildNewGameSupportBullets(runtimeData.NewGameSupport, request.Language);
        var warningMarkup = BuildStartupWarningMarkup(
            runtimeData.MessageBinding,
            runtimeData.MessageCenter,
            request.Language,
            normalizedVendor);
        var startupUrl = ResolveStartupUrl(
            runtimeData.MessageBinding,
            runtimeData.MessageCenter,
            request.Language,
            normalizedVendor,
            logs);
        var warningLineCount = SplitMarkupText(warningMarkup).Count;

        var sections = new List<AppDialogSection>();
        if (newGameBullets.Count > 0)
        {
            sections.Add(new AppDialogSection
            {
                Kind = AppDialogSectionKind.NewGameSupport,
                Title = request.Language == AppLanguage.Korean ? "신규 지원 게임" : "New Game Support",
                Items = newGameBullets
            });
            logs.Add(Info("startup", $"new_game_support bullet_count={newGameBullets.Count}"));
        }

        if (!string.IsNullOrWhiteSpace(warningMarkup))
        {
            sections.Add(new AppDialogSection
            {
                Kind = AppDialogSectionKind.StartupWarning,
                Title = "",
                Items = [warningMarkup]
            });
            logs.Add(Info("startup", $"startup_warning bullet_count={warningLineCount} vendor={normalizedVendor}"));
        }

        if (sections.Count == 0)
        {
            logs.Add(Info(
                "startup",
                string.IsNullOrWhiteSpace(startupUrl)
                    ? "startup announcement skipped reason=no_notice_content"
                    : "startup announcement skipped reason=no_notice_content startup_url=ready"));
            return new StartupAnnouncementFlowResult
            {
                ShouldShowDialog = false,
                StartupUrl = startupUrl,
                Logs = logs
            };
        }

        var dialog = _presenter.BuildStartupAnnouncementDialog(
            GetTitle(request.Language),
            sections,
            request.Language);

        logs.Add(Info("startup", $"startup announcement ready section_count={sections.Count} item_count={newGameBullets.Count + warningLineCount}"));
        return new StartupAnnouncementFlowResult
        {
            ShouldShowDialog = true,
            DialogRequest = dialog,
            StartupUrl = startupUrl,
            Logs = logs
        };
    }

    private static IReadOnlyList<string> BuildNewGameSupportBullets(
        IReadOnlyList<RuntimeDataNewGameSupportRow>? rows,
        AppLanguage language)
    {
        if (rows is null || rows.Count == 0)
        {
            return Array.Empty<string>();
        }

        var result = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows)
        {
            var title = language == AppLanguage.Korean
                ? RuntimeDataRowReader.GetString(row, "game_name_kr")
                : RuntimeDataRowReader.GetString(row, "game_name_en");
            if (string.IsNullOrWhiteSpace(title))
            {
                title = language == AppLanguage.Korean
                    ? RuntimeDataRowReader.GetString(row, "game_name_en")
                    : RuntimeDataRowReader.GetString(row, "game_name_kr");
            }

            var normalizedTitle = (title ?? "").Trim();
            if (string.IsNullOrWhiteSpace(normalizedTitle))
            {
                continue;
            }

            var detectedOn = RuntimeDataRowReader.GetString(row, "detected_on");
            if (!DateOnly.TryParse(detectedOn, CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            {
                continue;
            }

            if (!seen.Add(normalizedTitle))
            {
                continue;
            }

            result.Add(normalizedTitle);
        }

        return result;
    }

    private static string BuildStartupWarningMarkup(
        IReadOnlyList<RuntimeDataMessageRow>? bindingRows,
        IReadOnlyList<RuntimeDataMessageRow>? templateRows,
        AppLanguage language,
        string gpuVendor)
    {
        if (bindingRows is null || templateRows is null || bindingRows.Count == 0 || templateRows.Count == 0)
        {
            return "";
        }

        var templates = ParseTemplates(templateRows);
        var bindings = ParseBindings(bindingRows)
            .Where(static binding => string.Equals(binding.Stage, "startup", StringComparison.OrdinalIgnoreCase))
            .Where(binding => IsBindingMatch(binding, gpuVendor))
            .OrderBy(static binding => binding.Priority)
            .ThenBy(binding => BindingVendorRank(binding, gpuVendor))
            .ToArray();

        if (bindings.Length == 0)
        {
            return "";
        }

        var result = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var binding in bindings)
        {
            if (!templates.TryGetValue(binding.MessageId, out var template))
            {
                continue;
            }

            if (!string.Equals(template.Category, "popup", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var localizedText = language == AppLanguage.Korean ? template.Korean : template.English;
            var normalizedText = (localizedText ?? "").Trim();
            if (string.IsNullOrWhiteSpace(normalizedText))
            {
                continue;
            }

            if (seen.Add(normalizedText))
            {
                result.Add(normalizedText);
            }
        }

        return result.Count == 0 ? "" : string.Join("[P]", result);
    }

    private static string ResolveStartupUrl(
        IReadOnlyList<RuntimeDataMessageRow>? bindingRows,
        IReadOnlyList<RuntimeDataMessageRow>? templateRows,
        AppLanguage language,
        string gpuVendor,
        List<StartupFlowLogEntry> logs)
    {
        if (bindingRows is null || templateRows is null || bindingRows.Count == 0 || templateRows.Count == 0)
        {
            return "";
        }

        var templates = ParseTemplates(templateRows);
        var matches = ParseBindings(bindingRows)
            .Where(static binding => string.Equals(binding.Stage, "startup", StringComparison.OrdinalIgnoreCase))
            .Where(binding => IsBindingMatch(binding, gpuVendor))
            .OrderBy(static binding => binding.Priority)
            .ThenBy(binding => BindingVendorRank(binding, gpuVendor))
            .ThenBy(static binding => binding.MessageId, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var candidates = new List<string>();
        foreach (var binding in matches)
        {
            if (!templates.TryGetValue(binding.MessageId, out var template))
            {
                continue;
            }

            if (!string.Equals(template.Category, "url", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var rawUrl = ResolveLocalizedUrl(template, language);
            if (!ExternalMessageUrlValidator.TryNormalizeHttpUrl(rawUrl, out var normalizedUrl))
            {
                logs.Add(Warning("startup", $"startup_url skipped reason=invalid_url message_id={NormalizeLogValue(binding.MessageId, "unknown")}"));
                continue;
            }

            candidates.Add(normalizedUrl);
        }

        if (candidates.Count == 0)
        {
            return "";
        }

        if (candidates.Count > 1)
        {
            logs.Add(Warning("startup", $"startup_url duplicate_count={candidates.Count} action=using_first"));
        }

        logs.Add(Info("startup", "startup_url ready"));
        return candidates[0];
    }

    private static Dictionary<string, MessageTemplate> ParseTemplates(IReadOnlyList<RuntimeDataMessageRow> rows)
    {
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
                MessageId = messageId,
                Category = RuntimeDataRowReader.GetString(row, "category"),
                Korean = RuntimeDataRowReader.GetString(row, "ko"),
                English = RuntimeDataRowReader.GetString(row, "en"),
                Url = RuntimeDataRowReader.GetString(row, "url")
            };
        }

        return map;
    }

    private static IReadOnlyList<MessageBinding> ParseBindings(IReadOnlyList<RuntimeDataMessageRow> rows)
    {
        var list = new List<MessageBinding>();
        foreach (var row in rows)
        {
            var messageId = RuntimeDataRowReader.GetString(row, "message_id");
            var stage = RuntimeDataRowReader.GetString(row, "stage");
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
                GameId = RuntimeDataRowReader.GetString(row, "game_id"),
                GpuVendor = RuntimeDataRowReader.GetString(row, "gpu_vendor"),
                Stage = stage,
                MessageId = messageId,
                Priority = priority
            });
        }

        return list;
    }

    private static bool IsBindingMatch(MessageBinding binding, string gpuVendor)
    {
        var normalizedGame = NormalizeKey(binding.GameId, "*");
        if (!string.Equals(normalizedGame, "*", StringComparison.Ordinal))
        {
            return false;
        }

        var normalizedBindingVendor = NormalizeKey(binding.GpuVendor, "all");
        return normalizedBindingVendor == "all"
               || string.Equals(normalizedBindingVendor, gpuVendor, StringComparison.Ordinal);
    }

    private static int BindingVendorRank(MessageBinding binding, string gpuVendor)
    {
        var normalizedBindingVendor = NormalizeKey(binding.GpuVendor, "all");
        return string.Equals(normalizedBindingVendor, gpuVendor, StringComparison.Ordinal) ? 0 : 1;
    }

    private static IReadOnlyList<string> SplitMarkupText(string source)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return Array.Empty<string>();
        }

        var normalized = source
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n');
        normalized = ParagraphTokenRegex.Replace(normalized, "\n");
        normalized = StripUnsupportedMarkupTokens(normalized);

        return normalized
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(static line => NormalizeLine(line))
            .Where(static line => !string.IsNullOrWhiteSpace(line))
            .ToArray();
    }

    private static string StripUnsupportedMarkupTokens(string source)
    {
        if (string.IsNullOrEmpty(source))
        {
            return "";
        }

        return AnyMarkupTokenRegex.Replace(
            source,
            match =>
            {
                var token = match.Groups["token"].Value;
                return SupportedMarkupTokens.Contains(token) ? match.Value : "";
            });
    }

    private static string NormalizeLine(string source)
    {
        var value = (source ?? "").Trim();
        if (string.IsNullOrWhiteSpace(value))
        {
            return "";
        }

        return Regex.Replace(value, @"\s+", " ").Trim();
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

    private static string ResolveLocalizedUrl(MessageTemplate template, AppLanguage language)
    {
        return language == AppLanguage.Korean
            ? PickFirstNonEmpty(template.Korean, template.English, template.Url)
            : PickFirstNonEmpty(template.English, template.Korean, template.Url);
    }

    private static string PickFirstNonEmpty(params string[] values)
    {
        foreach (var value in values)
        {
            var normalized = (value ?? "").Trim();
            if (!string.IsNullOrWhiteSpace(normalized))
            {
                return normalized;
            }
        }

        return "";
    }

    private static string NormalizeLogValue(string? value, string fallback)
    {
        var normalized = (value ?? "").Trim();
        return string.IsNullOrWhiteSpace(normalized) ? fallback : normalized;
    }

    private static string GetTitle(AppLanguage language)
    {
        return language == AppLanguage.Korean ? "OptiClick 안내" : "OptiClick Notice";
    }

    private static StartupFlowLogEntry Info(string category, string message)
    {
        return new StartupFlowLogEntry
        {
            Level = "info",
            Category = category,
            Message = message
        };
    }

    private static StartupFlowLogEntry Warning(string category, string message)
    {
        return new StartupFlowLogEntry
        {
            Level = "warning",
            Category = category,
            Message = message
        };
    }

    private sealed record MessageTemplate
    {
        public string MessageId { get; init; } = "";
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
}
