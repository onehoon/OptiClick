using OptiClick.Core.Runtime;
using OptiClick.Wpf.Install.Archives;
using OptiClick.Wpf.Install.UiState;
using OptiClick.Wpf.Install.Planning;
using OptiClick.Wpf.Shell.RuntimeData;
using OptiClick.Wpf.Shell.Games;
using OptiClick.Wpf.ViewModels;

namespace OptiClick.Wpf.Shell.Selection;

public sealed class InstallSelectionRequestBuilder
{
    private readonly IInstallStatusResolver? _installStatusResolver;
    private readonly IRtssOverlayNoticeResolver _rtssOverlayNoticeResolver;

    public InstallSelectionRequestBuilder(
        IInstallStatusResolver? installStatusResolver = null,
        IRtssOverlayNoticeResolver? rtssOverlayNoticeResolver = null)
    {
        _installStatusResolver = installStatusResolver;
        _rtssOverlayNoticeResolver = rtssOverlayNoticeResolver ?? new RtssOverlayNoticeResolver();
    }

    public ShellInstallSelectionRequest Build(InstallSelectionRequestBuildInput input)
    {
        var hasRemoteCatalogError = !string.IsNullOrWhiteSpace((input.LatestRemoteCatalogErrorCode ?? "").Trim());
        var games = (input.Cards ?? Array.Empty<GameCardViewModel>())
            .Select(ShellGameCardMapper.Map)
            .ToArray();
        var selectedShellGame = ShellGameCardMapper.Map(input.SelectedCard);
        var targetPaths = games
            .Where(static game => !string.IsNullOrWhiteSpace(game.GameId))
            .ToDictionary(
                game => game.GameId,
                game => input.TargetPathByGameId.TryGetValue(game.GameId, out var path)
                    ? InstallTargetPathNormalizer.NormalizeTargetDirectory(path)
                    : "",
                StringComparer.OrdinalIgnoreCase);

        input.MatchByGameId.TryGetValue(selectedShellGame.GameId, out var selectedMatchResult);
        var selectedTargetPath = targetPaths.TryGetValue(selectedShellGame.GameId, out var targetPath)
            ? targetPath
            : "";
        var selectedInstallStatus = ResolveInstallStatusForSelection(
            selectedShellGame,
            selectedTargetPath,
            input.ModuleDownloadLinks,
            input.SelectedLanguage);
        var fsr4Required = ShellGameCardMapper.ResolveFsr4Required(selectedShellGame);
        var selectionPopupMessage = BuildSelectionPopupMessage(selectedShellGame, input.SelectedLanguage);
        var installPostPopupMessage = ResolveLocalizedInstallPostMessage(selectedShellGame, input.SelectedLanguage);

        return new ShellInstallSelectionRequest
        {
            SelectedIndex = input.SelectedIndex,
            Games = games,
            SelectedMatchResult = selectedMatchResult,
            PreviousState = input.PreviousState,
            TargetPathsByGameId = targetPaths,
            Language = input.SelectedLanguage == AppLanguage.Korean ? "ko" : "en",
            SelectionPopupMessage = selectionPopupMessage,
            PrecheckPopupMessage = "",
            InstallPostPopupMessage = installPostPopupMessage,
            SelectedInstallStatusCode = selectedInstallStatus.Code,
            SelectedInstallStatus = selectedInstallStatus,
            MultiGpuBlocked = input.MultiGpuBlocked,
            GpuSelectionPending = input.GpuSelectionPending,
            SheetReady = !hasRemoteCatalogError,
            SheetLoading = false,
            InstallInProgress = input.IsInstallExecutionInProgress,
            AppUpdateInProgress = input.IsAppUpdateInProgress,
            Fsr4Required = fsr4Required,
            PrecheckSnapshotFallback = input.PreviousState.PrecheckSnapshot,
            ArchiveReadiness = input.LatestArchiveReadiness
        };
    }

    private InstallStatusSnapshot ResolveInstallStatusForSelection(
        ShellGameCardModel selectedGame,
        string targetPath,
        IReadOnlyDictionary<string, object?> moduleDownloadLinks,
        AppLanguage selectedLanguage)
    {
        if (_installStatusResolver is null || string.IsNullOrWhiteSpace(targetPath))
        {
            return new InstallStatusSnapshot
            {
                Code = InstallStatusCodes.Installable
            };
        }

        var (currentVersion, currentDisplayVersion) = ResolveCurrentOptiScalerVersionPair(moduleDownloadLinks);
        return _installStatusResolver.Resolve(new InstallStatusResolveInput
        {
            TargetPath = targetPath,
            CurrentVersion = currentVersion,
            CurrentDisplayVersion = currentDisplayVersion,
            Language = selectedLanguage == AppLanguage.Korean ? "ko" : "en"
        });
    }

    private static (string CurrentVersion, string CurrentDisplayVersion) ResolveCurrentOptiScalerVersionPair(
        IReadOnlyDictionary<string, object?> moduleDownloadLinks)
    {
        if (!moduleDownloadLinks.TryGetValue(ArchiveAssetRuntimeDataKeys.OptiScaler, out var rawEntry)
            || rawEntry is not IReadOnlyDictionary<string, object?> entry)
        {
            return ("", "");
        }

        var currentVersion = ReadFirstNonEmpty(entry, "version", "current_version");
        var currentDisplayVersion = ReadFirstNonEmpty(entry, "display_version", "current_display_version", "version_label");
        return (currentVersion, currentDisplayVersion);
    }

    private static string ResolveLocalizedInstallPreMessage(ShellGameCardModel game, AppLanguage language)
    {
        if (language == AppLanguage.Korean)
        {
            return PickFirstNonEmpty(game.InstallPreKr, game.InstallPreEn);
        }

        return PickFirstNonEmpty(game.InstallPreEn, game.InstallPreKr);
    }

    private static string ResolveLocalizedInstallPostMessage(ShellGameCardModel game, AppLanguage language)
    {
        if (language == AppLanguage.Korean)
        {
            return PickFirstNonEmpty(game.InstallPostKr, game.InstallPostEn);
        }

        return PickFirstNonEmpty(game.InstallPostEn, game.InstallPostKr);
    }

    private string BuildSelectionPopupMessage(ShellGameCardModel game, AppLanguage language)
    {
        var installPre = ResolveLocalizedInstallPreMessage(game, language);
        var rtssOverlayNotice = _rtssOverlayNoticeResolver.ResolveNotice(game, language);
        if (string.IsNullOrWhiteSpace(installPre))
        {
            return rtssOverlayNotice;
        }

        if (string.IsNullOrWhiteSpace(rtssOverlayNotice))
        {
            return installPre;
        }

        return $"{installPre}[P]{rtssOverlayNotice}";
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
}
