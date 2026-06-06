using System.Threading;
using OptiClick.Core.Runtime;
using OptiClick.Wpf.Install.Planning;
using OptiClick.Wpf.Install.Precheck;
using OptiClick.Wpf.Install.UiState;
using OptiClick.Wpf.Localization;
using OptiClick.Wpf.Shell.Dialogs.Markup;
using OptiClick.Wpf.Shell.Games;
using OptiClick.Wpf.Shell.Games.Actions;
using OptiClick.Wpf.Shell.Scan;

namespace OptiClick.Wpf.Shell.Selection;

public interface IShellInstallSelectionBridge
{
    Task<ShellInstallSelectionResult> SelectAsync(
        ShellInstallSelectionRequest request,
        CancellationToken cancellationToken = default);

    ShellInstallSelectionResult ConfirmNextPopup(ShellInstallSelectionState state);
}

public sealed class ShellInstallSelectionBridge : IShellInstallSelectionBridge
{
    private const string MissingSelectionTargetErrorCode = "invalid_target_folder";

    private readonly InstallPrecheckHandlerRegistry _precheckHandlerRegistry;
    private readonly InstallSelectionPrecheckFlow _precheckFlow;
    private readonly ModConflictNoticeBuilder _modConflictNoticeBuilder;
    private readonly ShellGameActionAvailabilityResolver _actionAvailabilityResolver;
    private readonly IInstallUiStateInputBuilder _installUiStateInputBuilder;
    private readonly IInstallButtonStateResolver _installButtonStateResolver;
    private readonly IInstallButtonPresentationResolver _installButtonPresentationResolver;
    private readonly IInstallSummaryViewModelBuilder _installSummaryViewModelBuilder;
    private readonly IAppStringsProvider _stringsProvider;
    private long _selectionRequestVersion;

    public ShellInstallSelectionBridge(
        InstallPrecheckHandlerRegistry precheckHandlerRegistry,
        InstallSelectionPrecheckFlow precheckFlow,
        ModConflictNoticeBuilder modConflictNoticeBuilder,
        ShellGameActionAvailabilityResolver actionAvailabilityResolver,
        IInstallUiStateInputBuilder installUiStateInputBuilder,
        IInstallButtonStateResolver installButtonStateResolver,
        IInstallButtonPresentationResolver installButtonPresentationResolver,
        IInstallSummaryViewModelBuilder installSummaryViewModelBuilder,
        IAppStringsProvider? stringsProvider = null)
    {
        _precheckHandlerRegistry = precheckHandlerRegistry;
        _precheckFlow = precheckFlow;
        _modConflictNoticeBuilder = modConflictNoticeBuilder;
        _actionAvailabilityResolver = actionAvailabilityResolver;
        _installUiStateInputBuilder = installUiStateInputBuilder;
        _installButtonStateResolver = installButtonStateResolver;
        _installButtonPresentationResolver = installButtonPresentationResolver;
        _installSummaryViewModelBuilder = installSummaryViewModelBuilder;
        _stringsProvider = stringsProvider ?? new AppStringsProvider();
    }

    public async Task<ShellInstallSelectionResult> SelectAsync(
        ShellInstallSelectionRequest request,
        CancellationToken cancellationToken = default)
    {
        var requestId = Interlocked.Increment(ref _selectionRequestVersion);
        try
        {
            var selectedGame = ResolveSelectedGame(request.Games, request.SelectedIndex);
            var selectedMatch = ResolveSelectedMatch(selectedGame, request.SelectedMatchResult);
            var actionAvailability = _actionAvailabilityResolver.Resolve(selectedGame, selectedMatch);
            var selectedInstallStatus = request.SelectedInstallStatus ?? new InstallStatusSnapshot
            {
                Code = request.SelectedInstallStatusCode
            };
            var summary = BuildSummary(selectedGame, request.Language, selectedInstallStatus);
            var runningSnapshot = selectedGame is null
                ? InstallPrecheckSnapshot.NotStarted
                : InstallPrecheckSnapshot.NotStarted with { State = Install.Planning.InstallPrecheckState.Running };

            var runningState = BuildState(
                request,
                requestId,
                selectedGame,
                selectedMatch,
                actionAvailability,
                summary,
                selectedInstallStatus,
                popupConfirmed: false,
                precheckRunning: selectedGame is not null,
                precheckWasRunning: selectedGame is not null,
                precheckOk: false,
                precheckError: "",
                precheckResolvedDllName: "",
                popupQueue: new ShellPopupRequestQueue(Array.Empty<ShellPopupRequest>(), confirmedImmediately: false, precheckOk: false),
                pendingPopupRequests: Array.Empty<ShellPopupRequest>(),
                precheckSnapshot: runningSnapshot,
                installButtonPresentation: null);

            runningState = runningState with
            {
                RunningInstallButtonPresentation = BuildInstallButtonPresentation(runningState)
            };

            if (selectedGame is null)
            {
                var invalidState = runningState with
                {
                    PrecheckRunning = false,
                    PrecheckSnapshot = request.PrecheckSnapshotFallback,
                    InstallButtonPresentation = BuildInstallButtonPresentation(runningState with
                    {
                        PrecheckRunning = false,
                        PrecheckSnapshot = request.PrecheckSnapshotFallback
                    })
                };

                return new ShellInstallSelectionResult
                {
                    IsSuccess = true,
                    State = invalidState
                };
            }

            var selectedTargetPath = ResolveTargetPath(request.TargetPathsByGameId, selectedGame.GameId);
            var hasMatchedSelectionTarget = selectedMatch is not null
                && selectedMatch.Status != ShellGameMatchStatus.None
                && !string.IsNullOrWhiteSpace(selectedTargetPath);

            InstallPrecheckResult precheckResult;
            if (!hasMatchedSelectionTarget)
            {
                precheckResult = new InstallPrecheckResult
                {
                    Ok = false,
                    ErrorCode = MissingSelectionTargetErrorCode
                };
            }
            else
            {
                var precheckRequest = new InstallPrecheckRequest
                {
                    Game = selectedGame,
                    TargetPath = selectedTargetPath,
                    PreferredDllName = ShellGameInstallMetadataResolver.GetOptiScalerDllName(selectedGame)
                };

                var handler = _precheckHandlerRegistry.Resolve(selectedGame);
                try
                {
                    precheckResult = await Task.Run(
                        () => handler.Run(precheckRequest, useKorean: IsKoreanLanguage(request.Language)),
                        cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    precheckResult = new InstallPrecheckResult
                    {
                        Ok = false,
                        RawErrorMessage = ex.Message
                    };
                }
            }

            if (requestId != Interlocked.Read(ref _selectionRequestVersion))
            {
                return new ShellInstallSelectionResult
                {
                    IsSuccess = true,
                    IsStaleIgnored = true,
                    State = request.PreviousState ?? runningState
                };
            }

            var noticeDecision = _modConflictNoticeBuilder.Build(
                precheckResult.NoticeFindings ?? precheckResult.ConflictFindings,
                useKorean: IsKoreanLanguage(request.Language));
            var precheckOutcome = BuildSelectionOutcome(request, precheckResult, noticeDecision);
            var flowResult = _precheckFlow.Build(
                request.SelectedIndex,
                request.Games.Count,
                precheckOutcome,
                request.SelectionPopupMessage);
            var mappedPopups = MapPopupRequests(flowResult.PopupRequests);
            var popupQueue = new ShellPopupRequestQueue(
                mappedPopups,
                confirmedImmediately: flowResult.ConfirmedImmediately,
                precheckOk: flowResult.State.PrecheckOk);

            var precheckSnapshot = InstallPrecheckSnapshotMapper.ToSnapshot(precheckResult);
            var completedState = BuildState(
                request,
                requestId,
                selectedGame,
                selectedMatch,
                actionAvailability,
                summary,
                selectedInstallStatus,
                popupConfirmed: popupQueue.PopupConfirmed,
                precheckRunning: flowResult.State.PrecheckRunning,
                precheckWasRunning: true,
                precheckOk: flowResult.State.PrecheckOk,
                precheckError: flowResult.State.PrecheckError,
                precheckResolvedDllName: flowResult.State.PrecheckDllName,
                popupQueue: popupQueue,
                pendingPopupRequests: popupQueue.PendingRequests,
                precheckSnapshot: precheckSnapshot,
                installButtonPresentation: null);

            completedState = completedState with
            {
                RunningInstallButtonPresentation = runningState.RunningInstallButtonPresentation,
                InstallButtonPresentation = BuildInstallButtonPresentation(completedState)
            };

            return new ShellInstallSelectionResult
            {
                IsSuccess = true,
                State = completedState
            };
        }
        catch (OperationCanceledException)
        {
            return new ShellInstallSelectionResult
            {
                IsSuccess = false,
                ErrorMessage = "Selection was canceled.",
                State = request.PreviousState ?? new ShellInstallSelectionState()
            };
        }
    }

    public ShellInstallSelectionResult ConfirmNextPopup(ShellInstallSelectionState state)
    {
        if (state.PopupQueue is null)
        {
            return new ShellInstallSelectionResult
            {
                IsSuccess = false,
                ErrorMessage = "Popup queue is not initialized.",
                State = state
            };
        }

        state.PopupQueue.ConfirmNext();
        var updatedState = state with
        {
            PopupConfirmed = state.PopupQueue.PopupConfirmed,
            PendingPopupRequests = state.PopupQueue.PendingRequests,
            InstallButtonPresentation = BuildInstallButtonPresentation(state with
            {
                PopupConfirmed = state.PopupQueue.PopupConfirmed,
                PendingPopupRequests = state.PopupQueue.PendingRequests
            })
        };

        return new ShellInstallSelectionResult
        {
            IsSuccess = true,
            State = updatedState
        };
    }

    private ShellInstallSelectionState BuildState(
        ShellInstallSelectionRequest request,
        long requestId,
        ShellGameCardModel? selectedGame,
        ShellGameMatchResult? selectedMatch,
        ShellGameActionAvailability availability,
        InstallSummaryPresentation summary,
        InstallStatusSnapshot selectedInstallStatus,
        bool popupConfirmed,
        bool precheckRunning,
        bool precheckWasRunning,
        bool precheckOk,
        string precheckError,
        string precheckResolvedDllName,
        ShellPopupRequestQueue popupQueue,
        IReadOnlyList<ShellPopupRequest> pendingPopupRequests,
        InstallPrecheckSnapshot precheckSnapshot,
        InstallButtonPresentation? installButtonPresentation)
    {
        return new ShellInstallSelectionState
        {
            RequestId = requestId,
            SelectedIndex = selectedGame is null ? null : request.SelectedIndex,
            SelectedGameId = (selectedGame?.GameId ?? "").Trim(),
            SelectedGame = selectedGame,
            SelectedMatchResult = selectedMatch,
            PopupConfirmed = popupConfirmed,
            PrecheckRunning = precheckRunning,
            PrecheckWasRunning = precheckWasRunning,
            PrecheckOk = precheckOk,
            PrecheckError = (precheckError ?? "").Trim(),
            PrecheckResolvedDllName = (precheckResolvedDllName ?? "").Trim(),
            PrecheckSnapshot = precheckSnapshot,
            PopupQueue = popupQueue,
            PendingPopupRequests = pendingPopupRequests,
            ActionAvailability = availability,
            ActionAvailabilityReasonCode = availability.ReasonCode,
            InstallSummary = summary,
            ArchiveReadiness = request.ArchiveReadiness,
            SelectedInstallStatusCode = NormalizeStatusCode(selectedInstallStatus),
            Language = request.Language,
            InstallPostPopupMessage = (request.InstallPostPopupMessage ?? "").Trim(),
            SelectedInstallStatus = selectedInstallStatus ?? new InstallStatusSnapshot(),
            MultiGpuBlocked = request.MultiGpuBlocked,
            GpuSelectionPending = request.GpuSelectionPending,
            SheetReady = request.SheetReady,
            SheetLoading = request.SheetLoading,
            InstallInProgress = request.InstallInProgress,
            AppUpdateInProgress = request.AppUpdateInProgress,
            Fsr4Required = request.Fsr4Required,
            InstallButtonPresentation = installButtonPresentation ?? new InstallButtonPresentation()
        };
    }

    private InstallButtonPresentation BuildInstallButtonPresentation(ShellInstallSelectionState state)
    {
        var input = new InstallUiStateBuildInput
        {
            MultiGpuBlocked = state.MultiGpuBlocked,
            GpuSelectionPending = state.GpuSelectionPending,
            SheetReady = state.SheetReady,
            SheetLoading = state.SheetLoading,
            InstallInProgress = state.InstallInProgress,
            AppUpdateInProgress = state.AppUpdateInProgress,
            PopupConfirmed = state.PopupConfirmed,
            Fsr4Required = state.Fsr4Required,
            ArchiveReadiness = state.ArchiveReadiness,
            Precheck = state.PrecheckSnapshot,
            ActionAvailabilityReasonCode = state.ActionAvailabilityReasonCode,
            HasSelectedGame = state.SelectedGame is not null
        };

        var stateInputs = _installUiStateInputBuilder.BuildInstallButtonStateInputs(input);
        var buttonState = _installButtonStateResolver.Compute(stateInputs);
        var strings = _stringsProvider.Get(IsKoreanLanguage(state.Language) ? AppLanguage.Korean : AppLanguage.English);
        return _installButtonPresentationResolver.Resolve(
            buttonState,
            NormalizeStatusCode(state.SelectedInstallStatus),
            new DefaultInstallUiTextProvider
            {
                InstallButton = strings.InstallButtonInstall,
                InstallingButton = strings.InstallButtonInstalling,
                UpdateButton = strings.InstallButtonUpdate,
                UpdatingButton = strings.InstallButtonUpdating,
                ReinstallButton = strings.InstallButtonReinstall,
                ReinstallingButton = strings.InstallButtonReinstalling,
                LoadingButton = strings.InstallButtonLoading
            });
    }

    private InstallSummaryPresentation BuildSummary(ShellGameCardModel? game, string language, InstallStatusSnapshot installStatus)
    {
        if (game is null)
        {
            return new InstallSummaryPresentation();
        }

        return _installSummaryViewModelBuilder.Build(new InstallSummaryBuildInput
        {
            Game = game,
            Language = language,
            InstallStatus = installStatus,
            InstallSummaryNote = SelectLocalizedSummaryNote(game, language)
        });
    }

    private static string SelectLocalizedSummaryNote(ShellGameCardModel game, string language)
    {
        var isKorean = IsKoreanLanguage(language);
        if (isKorean)
        {
            return PickFirstNonEmpty(game.InstallSummaryNoteKr, game.InstallSummaryNoteEn);
        }

        return PickFirstNonEmpty(game.InstallSummaryNoteEn, game.InstallSummaryNoteKr);
    }

    private static string PickFirstNonEmpty(params string[] candidates)
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

    private static string NormalizeStatusCode(InstallStatusSnapshot? status)
    {
        var code = (status?.Code ?? "").Trim();
        return string.IsNullOrWhiteSpace(code) ? InstallStatusCodes.Installable : code;
    }

    private static InstallSelectionPrecheckOutcome BuildSelectionOutcome(
        ShellInstallSelectionRequest request,
        InstallPrecheckResult precheckResult,
        ModConflictNoticeDecision noticeDecision)
    {
        var popupMessage = "";
        if (!precheckResult.Ok)
        {
            popupMessage = (request.PrecheckPopupMessage ?? "").Trim();
            if (string.IsNullOrWhiteSpace(popupMessage)
                && !string.Equals(precheckResult.ErrorCode, MissingSelectionTargetErrorCode, StringComparison.OrdinalIgnoreCase))
            {
                popupMessage = (precheckResult.RawErrorMessage ?? "").Trim();
            }
        }

        var useStructuredModNotice = noticeDecision.Mode == ModConflictNoticeMode.GenericModConflict;
        var modNoticeMessage = useStructuredModNotice
            ? PickFirstNonEmpty(noticeDecision.HeaderText, noticeDecision.NoticeText)
            : noticeDecision.NoticeText;

        return new InstallSelectionPrecheckOutcome
        {
            Ok = precheckResult.Ok,
            Error = (precheckResult.RawErrorMessage ?? "").Trim(),
            ResolvedDllName = (precheckResult.ResolvedDllName ?? "").Trim(),
            PopupMessage = popupMessage,
            ModNoticeMessage = modNoticeMessage,
            ModNoticeBulletItems = useStructuredModNotice
                ? EscapeMarkupLiteralLines(noticeDecision.Lines)
                : Array.Empty<string>(),
            ModNoticeFooterText = useStructuredModNotice
                ? (noticeDecision.FooterText ?? "").Trim()
                : ""
        };
    }

    private static IReadOnlyList<ShellPopupRequest> MapPopupRequests(IEnumerable<PopupRequest> popupRequests)
    {
        var list = new List<ShellPopupRequest>();
        foreach (var popup in popupRequests)
        {
            list.Add(new ShellPopupRequest
            {
                Kind = popup.Type switch
                {
                    PopupRequestType.Precheck => ShellPopupRequestKind.PrecheckFailure,
                    PopupRequestType.ModNotice => ShellPopupRequestKind.ModNotice,
                    PopupRequestType.Selection => ShellPopupRequestKind.SelectionNotice,
                    _ => ShellPopupRequestKind.SelectionNotice
                },
                Message = popup.Message,
                BulletItems = popup.BulletItems,
                FooterText = popup.FooterText
            });
        }

        return list;
    }

    private static IReadOnlyList<string> EscapeMarkupLiteralLines(IEnumerable<string>? lines)
    {
        return (lines ?? Array.Empty<string>())
            .Select(static line => PopupMarkupParser.EscapeLiteralText(line).Trim())
            .Where(static line => !string.IsNullOrWhiteSpace(line))
            .ToArray();
    }

    private static ShellGameCardModel? ResolveSelectedGame(IReadOnlyList<ShellGameCardModel> games, int selectedIndex)
    {
        if (selectedIndex < 0 || selectedIndex >= games.Count)
        {
            return null;
        }

        return games[selectedIndex];
    }

    private static ShellGameMatchResult? ResolveSelectedMatch(ShellGameCardModel? game, ShellGameMatchResult? selectedMatchResult)
    {
        if (game is null || selectedMatchResult is null)
        {
            return null;
        }

        var selectedGameId = (game.GameId ?? "").Trim();
        var matchGameId = (selectedMatchResult.Game?.GameId ?? "").Trim();
        if (string.IsNullOrWhiteSpace(matchGameId))
        {
            return selectedMatchResult;
        }

        if (string.Equals(selectedGameId, matchGameId, StringComparison.OrdinalIgnoreCase))
        {
            return selectedMatchResult;
        }

        return null;
    }

    private static string ResolveTargetPath(IReadOnlyDictionary<string, string> targetPathsByGameId, string gameId)
    {
        var normalizedGameId = (gameId ?? "").Trim();
        if (string.IsNullOrWhiteSpace(normalizedGameId))
        {
            return "";
        }

        if (targetPathsByGameId.TryGetValue(normalizedGameId, out var targetPath))
        {
            return InstallTargetPathNormalizer.NormalizeTargetDirectory(targetPath);
        }

        return "";
    }

    private static bool IsKoreanLanguage(string language)
    {
        return (language ?? "").Trim().StartsWith("ko", StringComparison.OrdinalIgnoreCase);
    }
}
