using System.Security.Cryptography;
using System.Text;
using OptiClick.Wpf.Logging;
using OptiClick.Wpf.Models;
using OptiClick.Wpf.Shell.Dialogs;
using OptiClick.Wpf.Shell.Flow;
using OptiClick.Wpf.Shell.RuntimeData;
using OptiClick.Wpf.ViewModels;

namespace OptiClick.Wpf.Shell.Selection;

public sealed class SelectionPopupCoordinator
{
    private readonly GameSelectionFlowController _gameSelectionFlowController;
    private readonly DialogPresenter _dialogPresenter;
    private readonly FlowLogDispatcher _flowLogDispatcher;
    private readonly IAppLogger _appLogger;
    private readonly IExternalUrlLauncher? _externalUrlLauncher;
    private readonly HashSet<string> _reviewedSelectionPopupSignatures = new(StringComparer.OrdinalIgnoreCase);

    public SelectionPopupCoordinator(
        GameSelectionFlowController gameSelectionFlowController,
        DialogPresenter dialogPresenter,
        FlowLogDispatcher flowLogDispatcher,
        IAppLogger appLogger,
        IExternalUrlLauncher? externalUrlLauncher = null)
    {
        _gameSelectionFlowController = gameSelectionFlowController ?? throw new ArgumentNullException(nameof(gameSelectionFlowController));
        _dialogPresenter = dialogPresenter ?? throw new ArgumentNullException(nameof(dialogPresenter));
        _flowLogDispatcher = flowLogDispatcher ?? throw new ArgumentNullException(nameof(flowLogDispatcher));
        _appLogger = appLogger ?? throw new ArgumentNullException(nameof(appLogger));
        _externalUrlLauncher = externalUrlLauncher;
    }

    public ShellInstallSelectionState ConfirmNextPendingPopup(ShellInstallSelectionState selectionState)
    {
        ArgumentNullException.ThrowIfNull(selectionState);

        var result = _gameSelectionFlowController.ConfirmNextPopup(selectionState);
        _flowLogDispatcher.Dispatch(result.Logs, MainViewModelLogCategories.Selection);
        if (!result.DidRun || !result.IsSuccess || result.IsStaleIgnored)
        {
            return selectionState;
        }

        return result.SelectionState;
    }

    public async Task<SelectionPopupChainResult> ShowPendingAsync(
        SelectionPopupChainRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var selectionState = request.SelectionState;
        var didChange = false;

        if (request.SelectionVersion == request.ReadCurrentSelectionVersion())
        {
            OpenInstallPreUrlIfNeeded(selectionState);
        }

        while (request.SelectionVersion == request.ReadCurrentSelectionVersion())
        {
            var requests = selectionState.PendingPopupRequests;
            if (requests.Count == 0)
            {
                return new SelectionPopupChainResult(selectionState, didChange);
            }

            var popup = requests[0];
            var signature = BuildSelectionPopupSignature(popup, selectionState, request.SelectedGame);
            LogInfo($"popup show source=selection_precheck kind={popup.Kind} remaining_before={requests.Count} signature={signature}");
            var result = await _dialogPresenter.ShowSafelyAsync(
                BuildSelectionPopupDialogRequest(popup, request.Text),
                cancellationToken);
            LogInfo($"popup result source=selection_precheck kind={popup.Kind} result={result} signature={signature}");
            if (!ShouldConfirmSelectionPopup(result))
            {
                return new SelectionPopupChainResult(selectionState, didChange);
            }

            if (request.SelectionVersion != request.ReadCurrentSelectionVersion())
            {
                return new SelectionPopupChainResult(selectionState, didChange);
            }

            _reviewedSelectionPopupSignatures.Add(signature);
            var nextState = ConfirmNextPendingPopup(selectionState);
            didChange = didChange || !ReferenceEquals(selectionState, nextState);
            selectionState = nextState;
            request.ApplySelectionState(selectionState);
        }

        return new SelectionPopupChainResult(selectionState, didChange);
    }

    public SelectionPopupChainResult ConfirmAll(
        ShellInstallSelectionState selectionState,
        GameCardViewModel? selectedGame)
    {
        ArgumentNullException.ThrowIfNull(selectionState);

        var didChange = false;
        while (selectionState.PendingPopupRequests.Count > 0)
        {
            var popup = selectionState.PendingPopupRequests[0];
            var signature = BuildSelectionPopupSignature(popup, selectionState, selectedGame);
            var seenBefore = _reviewedSelectionPopupSignatures.Contains(signature);
            LogInfo(
                $"popup auto-confirm source=selection_refresh kind={popup.Kind} remaining_before={selectionState.PendingPopupRequests.Count} signature={signature} seen_before={seenBefore.ToString().ToLowerInvariant()}");
            var nextState = ConfirmNextPendingPopup(selectionState);
            didChange = didChange || !ReferenceEquals(selectionState, nextState);
            selectionState = nextState;
        }

        return new SelectionPopupChainResult(selectionState, didChange);
    }

    private static AppDialogRequest BuildSelectionPopupDialogRequest(
        ShellPopupRequest request,
        SelectionPopupText text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var dialogTitle = request.Kind switch
        {
            ShellPopupRequestKind.PrecheckFailure => text.SettingsWarning,
            ShellPopupRequestKind.ModNotice => text.InstallNoticeDialogTitle,
            ShellPopupRequestKind.SelectionNotice => text.InstallNoticeDialogTitle,
            _ => text.InstallDialogTitle
        };

        return new AppDialogRequest
        {
            Kind = AppDialogKind.Info,
            Severity = request.Kind switch
            {
                ShellPopupRequestKind.PrecheckFailure => DialogSeverity.Warning,
                ShellPopupRequestKind.ModNotice => DialogSeverity.Warning,
                _ => DialogSeverity.Info
            },
            Title = dialogTitle,
            Summary = (request.Message ?? "").Trim(),
            BulletItems = request.BulletItems ?? Array.Empty<string>(),
            FooterText = (request.FooterText ?? "").Trim(),
            PrimaryButtonText = text.DialogButtonOk,
            SecondaryButtonText = ""
        };
    }

    private static string BuildSelectionPopupSignature(
        ShellPopupRequest request,
        ShellInstallSelectionState selectionState,
        GameCardViewModel? selectedGame)
    {
        var selectedGameId = NormalizeStatusCode(
            selectionState.SelectedGameId ?? selectedGame?.ResolvedGameId,
            "none");
        var normalizedMessage = NormalizeSelectionPopupMessage(request.Message);
        var normalizedBullets = NormalizeSelectionPopupMessage(string.Join("\n", request.BulletItems ?? Array.Empty<string>()));
        var normalizedFooter = NormalizeSelectionPopupMessage(request.FooterText);
        var material = $"{selectedGameId}|{request.Kind}|{normalizedMessage}|{normalizedBullets}|{normalizedFooter}";
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(material))).ToLowerInvariant();
        return hash.Length > 16 ? hash[..16] : hash;
    }

    private static string NormalizeSelectionPopupMessage(string? message)
    {
        var normalized = (message ?? "").Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
        return string.Join("\n", normalized
                .Split('\n')
                .Select(static line => line.Trim()))
            .Trim();
    }

    private static bool ShouldConfirmSelectionPopup(AppDialogResult result)
    {
        return result != AppDialogResult.None
               && result != AppDialogResult.Cancel;
    }

    private void LogInfo(string message)
    {
        _appLogger.Info(MainViewModelLogCategories.Selection, message);
    }

    private void LogWarning(string message)
    {
        _appLogger.Warning(MainViewModelLogCategories.Selection, message);
    }

    private void OpenInstallPreUrlIfNeeded(ShellInstallSelectionState selectionState)
    {
        var selectedGame = selectionState.SelectedGame;
        if (selectedGame is null)
        {
            return;
        }

        var rawUrl = CoalesceTrimmed(
            selectedGame.ExternalGuidePreUrl,
            string.Equals(selectedGame.ExternalGuideUrlTrigger, "install_pre", StringComparison.OrdinalIgnoreCase)
                ? selectedGame.ExternalGuideUrl
                : "");
        if (string.IsNullOrWhiteSpace(rawUrl))
        {
            return;
        }

        if (!ExternalMessageUrlValidator.TryNormalizeHttpUrl(rawUrl, out var normalizedUrl))
        {
            LogWarning("external guide url open skipped trigger=install_pre reason=invalid_url");
            return;
        }

        if (_externalUrlLauncher is null)
        {
            LogWarning("external guide url open skipped trigger=install_pre reason=no_launcher");
            return;
        }

        try
        {
            if (!_externalUrlLauncher.OpenUrl(normalizedUrl))
            {
                LogWarning("external guide url open result=failed trigger=install_pre");
            }
            else
            {
                LogInfo("external guide url open result=success trigger=install_pre");
            }
        }
        catch (Exception ex)
        {
            LogWarning($"external guide url open result=failed trigger=install_pre type={ex.GetType().Name}");
        }
    }

    private static string NormalizeStatusCode(string? value, string fallback)
    {
        var normalized = (value ?? "").Trim();
        return string.IsNullOrWhiteSpace(normalized) ? fallback : normalized;
    }

    private static string CoalesceTrimmed(params string[] values)
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
}

public sealed record SelectionPopupChainRequest
{
    public required ShellInstallSelectionState SelectionState { get; init; }
    public required long SelectionVersion { get; init; }
    public required Func<long> ReadCurrentSelectionVersion { get; init; }
    public required Action<ShellInstallSelectionState> ApplySelectionState { get; init; }
    public required SelectionPopupText Text { get; init; }
    public GameCardViewModel? SelectedGame { get; init; }
}

public sealed record SelectionPopupChainResult(
    ShellInstallSelectionState SelectionState,
    bool DidChange);
