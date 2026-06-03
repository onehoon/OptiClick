namespace OptiClick.Wpf.Shell.Selection;

public sealed class GameSelectionFlowController
{
    private readonly IShellInstallSelectionBridge? _installSelectionBridge;
    private readonly InstallSelectionRequestBuilder _requestBuilder;

    public GameSelectionFlowController(
        IShellInstallSelectionBridge? installSelectionBridge,
        InstallSelectionRequestBuilder? requestBuilder = null)
    {
        _installSelectionBridge = installSelectionBridge;
        _requestBuilder = requestBuilder ?? new InstallSelectionRequestBuilder();
    }

    public bool CanSelect => _installSelectionBridge is not null;

    public async Task<GameSelectionFlowResult> SelectAsync(
        GameSelectionFlowRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.SelectedCard);
        ArgumentNullException.ThrowIfNull(request.PreviousSelectionState);

        if (_installSelectionBridge is null)
        {
            return new GameSelectionFlowResult
            {
                DidRun = false,
                IsSuccess = true,
                SelectionState = request.PreviousSelectionState
            };
        }

        var selectionRequest = _requestBuilder.Build(new InstallSelectionRequestBuildInput
        {
            SelectedIndex = request.SelectedIndex,
            SelectedCard = request.SelectedCard,
            Cards = request.Games,
            MatchByGameId = request.MatchByGameId,
            TargetPathByGameId = request.TargetPathByGameId,
            PreviousState = request.PreviousSelectionState,
            ModuleDownloadLinks = request.ModuleDownloadLinks,
            LatestArchiveReadiness = request.LatestArchiveReadiness,
            SelectedLanguage = request.SelectedLanguage,
            IsInstallExecutionInProgress = request.IsInstallExecutionInProgress,
            IsAppUpdateInProgress = request.IsAppUpdateInProgress,
            MultiGpuBlocked = request.MultiGpuBlocked,
            GpuSelectionPending = request.GpuSelectionPending,
            LatestRemoteCatalogErrorCode = request.LatestRemoteCatalogErrorCode
        });

        try
        {
            var result = await _installSelectionBridge.SelectAsync(selectionRequest, cancellationToken);
            return new GameSelectionFlowResult
            {
                DidRun = true,
                IsSuccess = result.IsSuccess,
                IsStaleIgnored = result.IsStaleIgnored,
                SelectionState = result.State
            };
        }
        catch (OperationCanceledException ex)
        {
            return new GameSelectionFlowResult
            {
                DidRun = true,
                IsSuccess = false,
                SelectionState = request.PreviousSelectionState,
                Logs =
                [
                    Warning("selection", "selection canceled", ex)
                ]
            };
        }
        catch (Exception ex)
        {
            return new GameSelectionFlowResult
            {
                DidRun = true,
                IsSuccess = false,
                SelectionState = request.PreviousSelectionState,
                Logs =
                [
                    Error("selection", "selection flow failed with exception", ex)
                ]
            };
        }
    }

    public GameSelectionFlowResult ConfirmNextPopup(ShellInstallSelectionState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (_installSelectionBridge is null || state.PendingPopupRequests.Count == 0)
        {
            return new GameSelectionFlowResult
            {
                DidRun = false,
                IsSuccess = true,
                SelectionState = state
            };
        }

        try
        {
            var result = _installSelectionBridge.ConfirmNextPopup(state);
            return new GameSelectionFlowResult
            {
                DidRun = true,
                IsSuccess = result.IsSuccess,
                IsStaleIgnored = result.IsStaleIgnored,
                SelectionState = result.State
            };
        }
        catch (Exception ex)
        {
            return new GameSelectionFlowResult
            {
                DidRun = true,
                IsSuccess = false,
                SelectionState = state,
                Logs =
                [
                    Error("selection", "confirm next popup failed with exception", ex)
                ]
            };
        }
    }

    private static GameSelectionFlowLogEntry Warning(string category, string message, Exception? exception = null)
    {
        return new GameSelectionFlowLogEntry
        {
            Level = "warning",
            Category = category,
            Message = message,
            Exception = exception
        };
    }

    private static GameSelectionFlowLogEntry Error(string category, string message, Exception? exception = null)
    {
        return new GameSelectionFlowLogEntry
        {
            Level = "error",
            Category = category,
            Message = message,
            Exception = exception
        };
    }
}
