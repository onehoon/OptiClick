using System.Windows.Media;
using OptiClick.Core.Models;
using OptiClick.Core.Runtime;
using OptiClick.Wpf.Install.FileSystem;
using OptiClick.Core.Install;
using OptiClick.Wpf.Install.Precheck;
using OptiClick.Wpf.Install.UiState;
using OptiClick.Wpf.Localization;
using OptiClick.Wpf.Shell.Selection;
using OptiClick.Wpf.ViewModels;

namespace OptiClick.Wpf.Shell.Games;

public sealed class ShellGameCardViewModelFactory : IShellGameCardViewModelFactory
{
    private static readonly Brush CoverBrush = CreateCoverBrush("#315C75");
    private static readonly BadgePalette InstallableBadgePalette = CreateBadgePalette("#D6101923", "#CC718699", "#FFD2DBE5");
    private static readonly BadgePalette UpdateAvailableBadgePalette = CreateBadgePalette("#D6211705", "#CCCA8A24", "#FFE7B55A");
    private static readonly BadgePalette LatestBadgePalette = CreateBadgePalette("#D60D2417", "#CC2AAF5A", "#FF35D26B");
    private static readonly BadgePalette PreReleaseBadgePalette = CreateBadgePalette("#D614142C", "#CC756FE3", "#FFC3BEFF");
    private static readonly BadgePalette NeedsReviewBadgePalette = CreateBadgePalette("#D6261015", "#CCCA5268", "#FFFF9AAA");
    private readonly IShellGameCardStateResolver _stateResolver;
    private readonly IInstallStatusResolver _installStatusResolver;
    private readonly IAppStringsProvider _stringsProvider;

    public ShellGameCardViewModelFactory(
        IShellGameCardStateResolver stateResolver,
        IAppStringsProvider stringsProvider,
        IInstallStatusResolver? installStatusResolver = null)
    {
        ArgumentNullException.ThrowIfNull(stateResolver);
        ArgumentNullException.ThrowIfNull(stringsProvider);
        _stateResolver = stateResolver;
        _installStatusResolver = installStatusResolver
            ?? new InstallStatusResolver(new InstallFileSystem(), new WindowsFileVersionInfoReader());
        _stringsProvider = stringsProvider;
    }

    public IReadOnlyList<GameCardViewModel> CreateCards(
        IReadOnlyList<ShellGameCardModel> games,
        RuntimeContext? runtimeContext,
        IReadOnlyDictionary<string, string>? targetPathByGameId = null,
        ModuleDownloadLinkContext? moduleDownloadLinks = null)
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
            ?? ModuleDownloadLinkContext.Empty;
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
            var descriptorInput = ShellInstallDescriptorInputFactory.FromShellGame(game);
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
                OptiScalerDllName = descriptorInput.OptiScalerDllName,
                ReframeworkUrl = descriptorInput.ReFrameworkUrl,
                SpecialK = descriptorInput.SpecialK,
                UltimateAsiLoader = descriptorInput.RequiresUltimateAsiLoader,
                OptiPatcher = descriptorInput.RequiresOptiPatcher,
                Unreal5 = descriptorInput.RequiresUnreal5,
                RtssOverlay = descriptorInput.RequiresRtssProfile,
                ExtraBundle = descriptorInput.ExtraBundle
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
            var badgePalette = ToBadgePalette(installStatus.Code);

            list.Add(new GameCardViewModel(
                game.DisplayName,
                string.IsNullOrWhiteSpace(matchExe) ? "Runtime catalog" : matchExe,
                statusBadge,
                stateDecision.ReasonCode,
                strings.RuntimeSummaryPreparing,
                "",
                strings.HomeSelectGameForNotes,
                CoverBrush,
                badgePalette.BackgroundBrush,
                gameEntry,
                game,
                badgePalette.BorderBrush,
                badgePalette.ForegroundBrush));
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

    private static BadgePalette ToBadgePalette(string code)
    {
        if (string.Equals(code, InstallStatusCodes.UpdateAvailable, StringComparison.OrdinalIgnoreCase))
        {
            return UpdateAvailableBadgePalette;
        }

        if (string.Equals(code, InstallStatusCodes.Latest, StringComparison.OrdinalIgnoreCase))
        {
            return LatestBadgePalette;
        }

        if (string.Equals(code, InstallStatusCodes.PreRelease, StringComparison.OrdinalIgnoreCase))
        {
            return PreReleaseBadgePalette;
        }

        if (string.Equals(code, InstallStatusCodes.NeedsReview, StringComparison.OrdinalIgnoreCase))
        {
            return NeedsReviewBadgePalette;
        }

        return InstallableBadgePalette;
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
        ModuleDownloadLinkContext moduleDownloadLinks)
    {
        if (!moduleDownloadLinks.TryResolveLink("optiscaler", out var entry))
        {
            return ("", "");
        }

        var currentVersion = entry.ReadFirstString("version", "current_version");
        var currentDisplayVersion = entry.ReadFirstString("display_version", "current_display_version", "version_label");
        return (currentVersion, currentDisplayVersion);
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

    private static BadgePalette CreateBadgePalette(string background, string border, string foreground)
    {
        return new BadgePalette(
            CreateSolidBrush(background),
            CreateSolidBrush(border),
            CreateSolidBrush(foreground));
    }

    private sealed record BadgePalette(
        Brush BackgroundBrush,
        Brush BorderBrush,
        Brush ForegroundBrush);
}
