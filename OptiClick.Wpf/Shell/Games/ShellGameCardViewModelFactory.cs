using System.Windows.Media;
using OptiClick.Core.Models;
using OptiClick.Core.OptiScaler;
using OptiClick.Core.Runtime;
using OptiClick.Wpf.Install.FileSystem;
using OptiClick.Core.Install;
using OptiClick.Wpf.Install.Precheck;
using OptiClick.Wpf.Install.UiState;
using OptiClick.Wpf.Localization;
using OptiClick.Wpf.Shell.Selection;
using OptiClick.Wpf.Shell.RuntimeData;
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
        return CreateCards(games, runtimeContext, targetPathByGameId, moduleDownloadLinks, null);
    }

    public IReadOnlyList<GameCardViewModel> CreateCards(
        IReadOnlyList<ShellGameCardModel> games,
        RuntimeContext? runtimeContext,
        IReadOnlyDictionary<string, string>? targetPathByGameId,
        ModuleDownloadLinkContext? moduleDownloadLinks,
        ArchiveReadinessSnapshot? archiveReadiness)
    {
        return CreateCards(games, runtimeContext, targetPathByGameId, moduleDownloadLinks, archiveReadiness, null);
    }

    public IReadOnlyList<GameCardViewModel> CreateCards(
        IReadOnlyList<ShellGameCardModel> games,
        RuntimeContext? runtimeContext,
        IReadOnlyDictionary<string, string>? targetPathByGameId,
        ModuleDownloadLinkContext? moduleDownloadLinks,
        ArchiveReadinessSnapshot? archiveReadiness,
        OptiScalerVariantCatalog? optiScalerVariantCatalog)
    {
        return CreateCards(
            games,
            runtimeContext,
            targetPathByGameId,
            moduleDownloadLinks,
            archiveReadiness,
            optiScalerVariantCatalog,
            "");
    }

    public IReadOnlyList<GameCardViewModel> CreateCards(
        IReadOnlyList<ShellGameCardModel> games,
        RuntimeContext? runtimeContext,
        IReadOnlyDictionary<string, string>? targetPathByGameId,
        ModuleDownloadLinkContext? moduleDownloadLinks,
        ArchiveReadinessSnapshot? archiveReadiness,
        OptiScalerVariantCatalog? optiScalerVariantCatalog,
        string preferredOptiScalerVariant)
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
        var safePreferredOptiScalerVariant = (preferredOptiScalerVariant ?? "").Trim();
        var currentVersionTargets = OptiScalerCurrentVersionInfoResolver.ResolveTargets(
            safeModuleDownloadLinks,
            archiveReadiness,
            optiScalerVariantCatalog);
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
                currentVersionTargets,
                safePreferredOptiScalerVariant,
                languageCode,
                installStatusCache);
            var statusBadge = ToStatusBadge(installStatus, strings);
            var badgePalette = ToBadgePalette(ResolveBadgeCode(installStatus));

            list.Add(new GameCardViewModel(
                ResolveLocalizedTitle(game, language, gameId, matchExe),
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

    private static string ResolveLocalizedTitle(
        ShellGameCardModel game,
        AppLanguage language,
        string gameId,
        string matchExe)
    {
        var englishName = (game.GameNameEn ?? "").Trim();
        var koreanName = (game.GameNameKr ?? "").Trim();
        var displayName = (game.DisplayName ?? "").Trim();

        if (language == AppLanguage.Korean)
        {
            return PickFirstNonEmpty(koreanName, englishName, displayName, gameId, matchExe);
        }

        return PickFirstNonEmpty(englishName, koreanName, displayName, gameId, matchExe);
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

    private InstallStatusSnapshot ResolveInstallStatusCached(
        string targetPath,
        OptiScalerCurrentVersionTargets currentVersionTargets,
        string preferredOptiScalerVariant,
        string languageCode,
        Dictionary<string, InstallStatusSnapshot> cache)
    {
        var safeCurrentVersionTargets = currentVersionTargets ?? OptiScalerCurrentVersionTargets.Empty;
        var safeCurrentVersionInfo = safeCurrentVersionTargets.Selected ?? OptiScalerCurrentVersionInfo.Empty;
        var safeCurrentVersion = safeCurrentVersionInfo.Version ?? "";
        var safeCurrentDisplayVersion = safeCurrentVersionInfo.DisplayVersion ?? "";
        var safeLanguageCode = languageCode ?? "";
        if (string.IsNullOrWhiteSpace(targetPath))
        {
            return BuildInstallableSnapshot(safeLanguageCode, safeCurrentVersion, safeCurrentDisplayVersion);
        }

        var cacheKey = string.Join(
            "|",
            targetPath.Trim(),
            safeCurrentVersionInfo.Variant,
            safeCurrentVersion,
            safeCurrentDisplayVersion,
            safeCurrentVersionInfo.FileVersion,
            safeCurrentVersionInfo.ProductVersion,
            safeCurrentVersionTargets.Stable.Variant,
            safeCurrentVersionTargets.Stable.Version,
            safeCurrentVersionTargets.Stable.DisplayVersion,
            safeCurrentVersionTargets.Stable.FileVersion,
            safeCurrentVersionTargets.Stable.ProductVersion,
            safeCurrentVersionTargets.Preview.Variant,
            safeCurrentVersionTargets.Preview.Version,
            safeCurrentVersionTargets.Preview.DisplayVersion,
            safeCurrentVersionTargets.Preview.FileVersion,
            safeCurrentVersionTargets.Preview.ProductVersion,
            preferredOptiScalerVariant,
            safeLanguageCode);

        if (cache.TryGetValue(cacheKey, out var snapshot))
        {
            return snapshot;
        }

        var resolved = ResolveInstallStatus(
            targetPath,
            safeCurrentVersionTargets,
            preferredOptiScalerVariant,
            safeLanguageCode);
        cache[cacheKey] = resolved;
        return resolved;
    }

    private InstallStatusSnapshot ResolveInstallStatus(
        string targetPath,
        OptiScalerCurrentVersionTargets currentVersionTargets,
        string preferredOptiScalerVariant,
        string languageCode)
    {
        var safeCurrentVersionTargets = currentVersionTargets ?? OptiScalerCurrentVersionTargets.Empty;
        var safeCurrentVersionInfo = safeCurrentVersionTargets.Selected ?? OptiScalerCurrentVersionInfo.Empty;
        try
        {
            return _installStatusResolver.Resolve(new InstallStatusResolveInput
            {
                TargetPath = targetPath,
                CurrentVariant = safeCurrentVersionInfo.Variant,
                CurrentVersion = safeCurrentVersionInfo.Version,
                CurrentDisplayVersion = safeCurrentVersionInfo.DisplayVersion,
                CurrentFileVersion = safeCurrentVersionInfo.FileVersion,
                CurrentProductVersion = safeCurrentVersionInfo.ProductVersion,
                PreferredVariant = preferredOptiScalerVariant,
                StableTarget = ToVersionIdentity(safeCurrentVersionTargets.Stable),
                PreviewTarget = ToVersionIdentity(safeCurrentVersionTargets.Preview),
                Language = languageCode
            });
        }
        catch
        {
            return BuildInstallableSnapshot(languageCode, safeCurrentVersionInfo.Version, safeCurrentVersionInfo.DisplayVersion);
        }
    }

    private static OptiScalerVersionIdentity ToVersionIdentity(OptiScalerCurrentVersionInfo info)
    {
        var safeInfo = info ?? OptiScalerCurrentVersionInfo.Empty;
        return new OptiScalerVersionIdentity
        {
            Variant = safeInfo.Variant,
            FileVersion = safeInfo.FileVersion,
            ProductVersion = safeInfo.ProductVersion,
            DisplayVersion = safeInfo.DisplayVersion
        };
    }

    private static InstallStatusSnapshot BuildInstallableSnapshot(
        string languageCode,
        string currentVersion,
        string currentDisplayVersion)
    {
        return new InstallStatusSnapshot
        {
            Code = InstallStatusCodes.Installable,
            BadgeCode = InstallStatusBadgeCodes.Installable,
            BadgeLabel = languageCode.StartsWith("ko", StringComparison.OrdinalIgnoreCase)
                ? "\uBBF8\uC124\uCE58"
                : "Not Installed",
            Label = languageCode.StartsWith("ko", StringComparison.OrdinalIgnoreCase) ? "미설치" : "Not Installed",
            CurrentVersion = currentVersion,
            CurrentDisplayVersion = currentDisplayVersion
        };
    }

    private static string ToStatusBadge(InstallStatusSnapshot installStatus, AppStrings strings)
    {
        var badgeLabel = (installStatus.BadgeLabel ?? "").Trim();
        if (!string.IsNullOrWhiteSpace(badgeLabel))
        {
            return badgeLabel;
        }

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

    private static string ResolveBadgeCode(InstallStatusSnapshot installStatus)
    {
        var badgeCode = (installStatus.BadgeCode ?? "").Trim();
        return string.IsNullOrWhiteSpace(badgeCode)
            ? (installStatus.Code ?? "").Trim()
            : badgeCode;
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

        if (string.Equals(code, InstallStatusBadgeCodes.StableInstalled, StringComparison.OrdinalIgnoreCase))
        {
            return LatestBadgePalette;
        }

        if (string.Equals(code, InstallStatusCodes.PreRelease, StringComparison.OrdinalIgnoreCase))
        {
            return PreReleaseBadgePalette;
        }

        if (string.Equals(code, InstallStatusBadgeCodes.PreviewInstalled, StringComparison.OrdinalIgnoreCase)
            || string.Equals(code, InstallStatusBadgeCodes.InstalledVersion, StringComparison.OrdinalIgnoreCase))
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
