namespace OptiClick.Wpf.Install.Precheck;

public sealed class InstallSelectionPrecheckFlow
{
    public InstallSelectionPrecheckFlowResult Build(
        int selectedIndex,
        int foundGameCount,
        InstallSelectionPrecheckOutcome? outcome,
        string selectionPopupMessage)
    {
        if (selectedIndex < 0 || selectedIndex >= foundGameCount || outcome is null)
        {
            return new InstallSelectionPrecheckFlowResult
            {
                State = new InstallSelectionUiState
                {
                    PopupConfirmed = false,
                    PrecheckRunning = false,
                    PrecheckOk = false
                }
            };
        }

        var state = new InstallSelectionUiState
        {
            PopupConfirmed = false,
            PrecheckRunning = false,
            PrecheckOk = outcome.Ok,
            PrecheckError = outcome.Error,
            PrecheckDllName = outcome.ResolvedDllName
        };

        var modNoticeMessage = (outcome.ModNoticeMessage ?? "").Trim();
        var popupMessage = (outcome.PopupMessage ?? "").Trim();
        var precheckPopupMessage = string.Join("\n\n", new[] { popupMessage, modNoticeMessage }.Where(message => !string.IsNullOrWhiteSpace(message))).Trim();
        if (!outcome.Ok)
        {
            if (!string.IsNullOrWhiteSpace(precheckPopupMessage))
            {
                return new InstallSelectionPrecheckFlowResult
                {
                    State = state,
                    PopupRequests = new[]
                    {
                        new PopupRequest
                        {
                            Type = PopupRequestType.Precheck,
                            Message = precheckPopupMessage
                        }
                    }
                };
            }

            return new InstallSelectionPrecheckFlowResult
            {
                State = state
            };
        }

        var normalizedSelectionPopupMessage = (selectionPopupMessage ?? "").Trim();
        var requests = new List<PopupRequest>();
        if (!string.IsNullOrWhiteSpace(modNoticeMessage))
        {
            requests.Add(new PopupRequest
            {
                Type = PopupRequestType.ModNotice,
                Message = modNoticeMessage
            });
        }

        if (!string.IsNullOrWhiteSpace(normalizedSelectionPopupMessage))
        {
            requests.Add(new PopupRequest
            {
                Type = PopupRequestType.Selection,
                Message = normalizedSelectionPopupMessage
            });
        }

        if (requests.Count == 0)
        {
            return new InstallSelectionPrecheckFlowResult
            {
                State = state with { PopupConfirmed = true },
                ConfirmedImmediately = true,
                ConfirmAfterPopupChain = false
            };
        }

        return new InstallSelectionPrecheckFlowResult
        {
            State = state,
            PopupRequests = requests,
            ConfirmAfterPopupChain = true
        };
    }
}

