using System.Windows.Media;
using OptiClick.Core.Models;
using OptiClick.Core.Runtime;
using OptiClick.Wpf.Install.FileSystem;
using OptiClick.Wpf.Install.Precheck;
using OptiClick.Wpf.Install.UiState;
using OptiClick.Wpf.Localization;
using OptiClick.Wpf.ViewModels;

namespace OptiClick.Wpf.Shell.Games;

public sealed class ShellGameCardViewModelFactory : IShellGameCardViewModelFactory
{
    private static readonly Brush CoverBrush = CreateCoverBrush("#315C75");
    private static readonly Brush InstallableBadgeBrush = CreateSolidBrush("#2E7D5B");
    private static readonly Brush UpdateAvailableBadgeBrush = CreateSolidBrush("#D6AA43");
    private static readonly Brush LatestBadgeBrush = CreateSolidBrush("#243447");
    private static readonly Brush PreReleaseBadgeBrush = CreateSolidBrush("#4A5D7A");
    private static readonly Brush NeedsReviewBadgeBrush = CreateSolidBrush("#9B4D56");
    private readonly IShellGameCardStateResolver _stateResolver;
    private readonly IInstallStatusResolver _installStatusResolver;
    private readonly IAppStringsProvider _stringsProvider;

    public ShellGameCardViewModelFactory()
        : this(
            new ShellGameCardStateResolver(),
            new AppStringsProvider(),
            new InstallStatusResolver(new InstallFileSystem(), new WindowsFileVersionInfoReader()))
    {
    }

    public ShellGameCardViewModelFactory(
        IShellGameCardStateResolver stateResolver,
        IAppStringsProvider? stringsProvider = null,
        IInstallStatusResolver? installStatusResolver = null)
    {
        _stateResolver = stateResolver;
        _installStatusResolver = installStatusResolver
            ?? new InstallStatusResolver(new InstallFileSystem(), new WindowsFileVersionInfoReader());
        _stringsProvider = stringsProvider ?? new AppStringsProvider();
    }

    public IReadOnlyList<GameCardViewModel> CreateCards(
        IReadOnlyList<ShellGameCardModel> games,
        RuntimeContext? runtimeContext,
        IReadOnlyDictionary<string, string>? targetPathByGameId = null,
        IReadOnlyDictionary<string, object?>? moduleDownloadLinks = null)
    {
        if (games is null || games.Count == 0)
        {
            return [];
        }

        var list = new List<GameCardViewModel>(games.Count);
        var language = runtimeContext?.Language ?? AppLanguage.English;
        var strings = _stringsProvider.Get(language);
        var languageCode = language == AppLanguage.Korean ? "ko" : "en";
        var safeTargetPathByGameId = targetPathByGameId
            ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var safeModuleDownloadLinks = moduleDownloadLinks
            ?? new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        var (currentVersion, currentDisplayVersion) = ResolveCurrentOptiScalerVersionPair(safeModuleDownloadLinks);
        var installStatusCache = new Dictionary<string, InstallStatusSnapshot>(StringComparer.OrdinalIgnoreCase);

        foreach (var game in games)
        {
            if (game is null)
            {
                continue;
            }

            var gameId = (game.GameId ?? "").Trim();
            var matchExe = (game.MatchExe ?? "").Trim();
            if (string.IsNullOrWhiteSpace(gameId) && string.IsNullOrWhiteSpace(matchExe))
            {
                continue;
            }

            var gameEntry = new GameEntry
            {
                GameId = gameId,
                GameNameKr = (game.GameNameKr ?? "").Trim(),
                GameNameEn = (game.GameNameEn ?? "").Trim(),
                MatchFiles = string.IsNullOrWhiteSpace(matchExe) ? Array.Empty<string>() : [matchExe],
                Enabled = game.Enabled,
                SupportIntel = game.SupportIntel,
                SupportAmd = game.SupportAmd,
                SupportNvidia = game.SupportNvidia,
                SupportedGpu = (game.SupportedGpu ?? "").Trim(),
                OptiScalerDllName = ShellGameInstallMetadataResolver.GetOptiScalerDllName(game),
                ReframeworkUrl = ShellGameInstallMetadataResolver.GetReFrameworkUrl(game),
                SpecialK = ShellGameInstallMetadataResolver.GetSpecialK(game),
                UltimateAsiLoader = ShellGameInstallMetadataResolver.GetUltimateAsiLoader(game),
                OptiPatcher = ShellGameInstallMetadataResolver.GetOptiPatcher(game),
                Unreal5 = ShellGameInstallMetadataResolver.GetUnreal5(game),
                RtssOverlay = ShellGameInstallMetadataResolver.GetRtssOverlay(game),
                ExtraBundle = ShellGameInstallMetadataResolver.GetExtraBundle(game)
            };

            var stateDecision = _stateResolver.Resolve(game, runtimeContext);
            var targetPath = ResolveTargetPath(safeTargetPathByGameId, gameId);
            var installStatus = ResolveInstallStatusCached(
                targetPath,
                currentVersion,
                currentDisplayVersion,
                languageCode,
                installStatusCache);
            var statusBadge = ToStatusBadge(installStatus, strings);
            var badgeBrush = ToBadgeBrush(installStatus.Code);

            list.Add(new GameCardViewModel(
                game.DisplayName,
                string.IsNullOrWhiteSpace(matchExe) ? "Runtime catalog" : matchExe,
                statusBadge,
                stateDecision.ReasonCode,
                strings.RuntimeSummaryPreparing,
                "",
                strings.HomeSelectGameForNotes,
                CoverBrush,
                badgeBrush,
                gameEntry,
                game));
        }

        return list;
    }

    private InstallStatusSnapshot ResolveInstallStatusCached(
        string targetPath,
        string currentVersion,
        string currentDisplayVersion,
        string languageCode,
        Dictionary<string, InstallStatusSnapshot> cache)
    {
        var safeCurrentVersion = currentVersion ?? "";
        var safeCurrentDisplayVersion = currentDisplayVersion ?? "";
        var safeLanguageCode = languageCode ?? "";
        if (string.IsNullOrWhiteSpace(targetPath))
        {
            return BuildInstallableSnapshot(safeLanguageCode, safeCurrentVersion, safeCurrentDisplayVersion);
        }

        var cacheKey = string.Join(
            "|",
            targetPath.Trim(),
            safeCurrentVersion,
            safeCurrentDisplayVersion,
            safeLanguageCode);

        if (cache.TryGetValue(cacheKey, out var snapshot))
        {
            return snapshot;
        }

        var resolved = ResolveInstallStatus(targetPath, safeCurrentVersion, safeCurrentDisplayVersion, safeLanguageCode);
        cache[cacheKey] = resolved;
        return resolved;
    }

    private InstallStatusSnapshot ResolveInstallStatus(
        string targetPath,
        string currentVersion,
        string currentDisplayVersion,
        string languageCode)
    {
        try
        {
            return _installStatusResolver.Resolve(new InstallStatusResolveInput
            {
                TargetPath = targetPath,
                CurrentVersion = currentVersion,
                CurrentDisplayVersion = currentDisplayVersion,
                Language = languageCode
            });
        }
        catch
        {
            return BuildInstallableSnapshot(languageCode, currentVersion, currentDisplayVersion);
        }
    }

    private static InstallStatusSnapshot BuildInstallableSnapshot(
        string languageCode,
        string currentVersion,
        string currentDisplayVersion)
    {
        return new InstallStatusSnapshot
        {
            Code = InstallStatusCodes.Installable,
            Label = languageCode.StartsWith("ko", StringComparison.OrdinalIgnoreCase) ? "미설치" : "Not Installed",
            CurrentVersion = currentVersion,
            CurrentDisplayVersion = currentDisplayVersion
        };
    }

    private static string ToStatusBadge(InstallStatusSnapshot installStatus, AppStrings strings)
    {
        var label = (installStatus.Label ?? "").Trim();
        if (!string.IsNullOrWhiteSpace(label))
        {
            return label;
        }

        var code = (installStatus.Code ?? "").Trim();
        return code switch
        {
            InstallStatusCodes.UpdateAvailable => "Update",
            InstallStatusCodes.Latest => "Latest",
            InstallStatusCodes.PreRelease => "Pre",
            InstallStatusCodes.NeedsReview => "Check",
            InstallStatusCodes.Installable => "Not Installed",
            _ => strings.StatusUnknown
        };
    }

    private static Brush ToBadgeBrush(string code)
    {
        if (string.Equals(code, InstallStatusCodes.UpdateAvailable, StringComparison.OrdinalIgnoreCase))
        {
            return UpdateAvailableBadgeBrush;
        }

        if (string.Equals(code, InstallStatusCodes.Latest, StringComparison.OrdinalIgnoreCase))
        {
            return LatestBadgeBrush;
        }

        if (string.Equals(code, InstallStatusCodes.PreRelease, StringComparison.OrdinalIgnoreCase))
        {
            return PreReleaseBadgeBrush;
        }

        if (string.Equals(code, InstallStatusCodes.NeedsReview, StringComparison.OrdinalIgnoreCase))
        {
            return NeedsReviewBadgeBrush;
        }

        return InstallableBadgeBrush;
    }

    private static string ResolveTargetPath(IReadOnlyDictionary<string, string> targetPathByGameId, string gameId)
    {
        if (string.IsNullOrWhiteSpace(gameId))
        {
            return "";
        }

        if (!targetPathByGameId.TryGetValue(gameId, out var targetPath))
        {
            return "";
        }

        return (targetPath ?? "").Trim();
    }

    private static (string CurrentVersion, string CurrentDisplayVersion) ResolveCurrentOptiScalerVersionPair(
        IReadOnlyDictionary<string, object?> moduleDownloadLinks)
    {
        if (!moduleDownloadLinks.TryGetValue("optiscaler", out var rawEntry)
            || rawEntry is not IReadOnlyDictionary<string, object?> entry)
        {
            return ("", "");
        }

        var currentVersion = ReadFirstNonEmpty(entry, "version", "current_version");
        var currentDisplayVersion = ReadFirstNonEmpty(entry, "display_version", "current_display_version", "version_label");
        return (currentVersion, currentDisplayVersion);
    }

    private static string ReadFirstNonEmpty(IReadOnlyDictionary<string, object?> entry, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (!entry.TryGetValue(key, out var value) || value is null)
            {
                continue;
            }

            var normalized = value.ToString()?.Trim() ?? "";
            if (!string.IsNullOrWhiteSpace(normalized))
            {
                return normalized;
            }
        }

        return "";
    }

    private static Brush CreateCoverBrush(string color)
    {
        var baseColor = (Color)ColorConverter.ConvertFromString(color);
        var light = Color.FromRgb(
            (byte)Math.Min(baseColor.R + 32, 255),
            (byte)Math.Min(baseColor.G + 32, 255),
            (byte)Math.Min(baseColor.B + 32, 255));

        return new LinearGradientBrush(baseColor, light, 45);
    }

    private static Brush CreateSolidBrush(string color)
    {
        return new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
    }
}
